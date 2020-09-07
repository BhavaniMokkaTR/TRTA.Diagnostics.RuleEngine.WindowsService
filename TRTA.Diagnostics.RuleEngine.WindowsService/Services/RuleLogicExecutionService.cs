using Common.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using TRTA.Diagnostics.RuleEngine.WindowsService.Models;
//using Common.Logging;
using Saxon.Api;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using log4net;
using log4net.Config;
using System.Threading.Tasks;
using TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public class RuleLogicExecutionService<TDocumentType> : IRuleLogicExecutionService<TDocumentType> where TDocumentType : IDiagnosable
    {
        private IRuleQueryExecuter<TDocumentType> _ruleQueryExecuter;
        private RuleExecutionLogService _logService;
        private SchemaTypeService _schemaTypeService;
        //private readonly ILogger _logger;
        //private readonly TrmrLogContext _context;
        private static ILog _logger = LogManager.GetLogger("RuleEngineWindowsService");

        public RuleLogicExecutionService(IRuleQueryExecuter<TDocumentType> ruleQueryExecuter, RuleExecutionLogService LogService,/* ILoggerFactory loggerFactory,*/ SchemaTypeService SchemaTypeService)
        {
            this._ruleQueryExecuter = ruleQueryExecuter;
            this._logService = LogService;
            this._schemaTypeService = SchemaTypeService;
            //_logger = loggerFactory.GetLogger("RuleEngine_WindowsService");
            //_context = new TrmrLogContext("RuleEngine_WindowsService");
        }

        public IEnumerable<FailedExecutionInfo> ExecuteRules(RuleEvaluationRequest nextRequest, Processor processor)
        {
            EvaluationRequest evaluationRequest = new EvaluationRequest();
            evaluationRequest.Id = nextRequest.Id;
            //_context.StartEventLog("ExecuteRules start");
            _logger.Info("ExecuteRules start");
            IList<FailedExecutionInfo> failedRules = new List<FailedExecutionInfo>();
            Guid? LocatorId = null;
            EfileSchemaType efileSchemaType = null;
            string logId = "";
            string diagnosticLog = string.Empty;
            CurrentSelection currentSelection = new CurrentSelection(nextRequest.Jurisdiction, nextRequest.Year, nextRequest.ReturnType);

            try
            {
                LocatorId = _logService.GetOrSaveLocator(nextRequest.Year, nextRequest.Jurisdiction, nextRequest.ReturnType, nextRequest.Locator).Id;
                efileSchemaType = _schemaTypeService.GetSchemaType(nextRequest.SchemaType);
                logId = _logService.Save(nextRequest.Year, nextRequest.Jurisdiction, nextRequest.ReturnType, LocatorId, efileSchemaType, evaluationRequest, RequestStatusConstants.IN_PROCESS);

                var xmlDocumentToEvaluate = CreateXmlDocument(nextRequest.DocumentToEvaluate);
                var defaultNamespace = ExtractDefaultNamespaceUri(xmlDocumentToEvaluate);
                var ruleExecutionElements = _ruleQueryExecuter.ExecuteRuleQuery(nextRequest.Year, nextRequest.ReturnType, nextRequest.Jurisdiction, nextRequest.SchemaType, defaultNamespace, processor).ToList();

                Stopwatch sw = Stopwatch.StartNew();
                DocumentBuilder documentBuilder = processor.NewDocumentBuilder();
                documentBuilder.IsLineNumbering = true;
                documentBuilder.WhitespacePolicy = WhitespacePolicy.PreserveAll;
                XdmNode _XdmNode = documentBuilder.Build(xmlDocumentToEvaluate);

                sw.Stop();
                _logger.Info(String.Format("End SaxonApi DocumentBuilder {0} - Time  {1:0.00}s", evaluationRequest.Id, sw.ElapsedMilliseconds / 1000));
                sw.Restart();
                _logger.Info(String.Format("Start loop rule execution elements RequestId: {0}", evaluationRequest.Id));

                Parallel.ForEach(ruleExecutionElements, (element) =>
                //foreach (var element in ruleExecutionElements)
                {
                    try
                    {
                        EFileDocument documentToEvaluate = new EFileDocument(xmlDocumentToEvaluate, true);
                        if (documentToEvaluate.IsDiagnosable)
                            documentToEvaluate.BeginDiagnostic();

                        var success = element.Evaluate(documentToEvaluate, element.useAtLeast, element.XQuery, currentSelection,
                            string.IsNullOrEmpty(element.AlternateText) ? element.RuleText : element.AlternateText,
                            processor, _XdmNode, element.Fields, element.CompiledXQueryExecutable);

                        if (element.useAtLeast > 0)
                        {
                            if (documentToEvaluate.NumberOfNoDiagnostics <= element.useAtLeast - 1)
                            {
                                FailedExecutionInfo failRule = GetFailRule(documentToEvaluate, element);
                                lock (failedRules)
                                {
                                    failedRules.Add(failRule);
                                }
                            }
                        }
                        else
                        {
                            if (documentToEvaluate.NumberOfDiagnostics > 0)
                            {
                                FailedExecutionInfo failRule = GetFailRule(documentToEvaluate, element);
                                lock (failedRules)
                                {
                                    failedRules.Add(failRule);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        string message = string.Empty;
                        if (e is ArgumentException)
                        {
                            message = "The fields from the rule cannot be used together.";
                            _logger.Error(
                                String.Format(
                                    "The fields from the rule cannot be used together RequestId: {0}, RuleId: {1}, RuleNumber: {2}",
                                    evaluationRequest.Id, element.RuleId, element.RuleNumber), e);
                        }
                        else
                        {
                            message = "Error while evaluating rule: " + e.Message;
                            _logger.Error(
                                String.Format(
                                    "Error while evaluating rule RequestId: {0}, RuleId: {1}, RuleNumber: {2}",
                                    evaluationRequest.Id, element.RuleId, element.RuleNumber), e);
                        }
                        lock (failedRules)
                        {
                            failedRules.Add(CreateFailRule(element.RuleId, element.RuleNumber, message, element.Categories, element.TaxProfileArea, element.LinkedQuery, 0));
                        }
                        _logger.Info(
                            String.Format(
                                "Catch loop rule execution elements end add rule RequestId: {0}, RuleId: {1}, RuleNumber: {2}",
                                evaluationRequest.Id, element.RuleId, element.RuleNumber));
                    }
                    //}
                });

                sw.Stop();
                _logger.Info(String.Format("End loop rule execution elements RequestId: {0}, Time:  {1:0.00}s ", evaluationRequest.Id, sw.ElapsedMilliseconds / 1000));

                diagnosticLog = string.Empty;
                if (failedRules.Count > 0)
                {
                    diagnosticLog = JsonConvert.SerializeObject(failedRules);
                }

                _logger.Info(String.Format("End Rule Execution Loop {0}", evaluationRequest.Id));

                _logService.Save(nextRequest.Year, nextRequest.Jurisdiction, nextRequest.ReturnType, LocatorId, efileSchemaType, evaluationRequest, RequestStatusConstants.SUCCESS, diagnosticLog, logId);

                _logger.Info(String.Format("Rule execution elements end save rule RequestId: {0}", evaluationRequest.Id));
            }
            catch (Exception ex)
            {
                _logger.Error("Error in ExecuteRules method", ex);
                _logService.Save(nextRequest.Year, nextRequest.Jurisdiction, nextRequest.ReturnType, LocatorId, efileSchemaType, evaluationRequest, RequestStatusConstants.FAILED, diagnosticLog, logId);
                throw new Exception("Error in ExecuteRules method", ex);
            }
            _logger.Info("Execute Rules Service Method end");
            //_context.EndEventLog();
            return failedRules;
        }

        private FailedExecutionInfo GetFailRule(EFileDocument documentToEvaluate, IRuleExecutionElement<TDocumentType> element)
        {
            string goToLinkData = String.Empty;

            FailedExecutionInfo failedRule = CreateFailRule(element.RuleId, element.RuleNumber, "Rule Logic evaluated to False", element.Categories, element.TaxProfileArea, element.LinkedQuery, documentToEvaluate.NumberOfDiagnostics,
                documentToEvaluate.IsDiagnosable ? documentToEvaluate.logResults : null, documentToEvaluate.IsDiagnosable ? documentToEvaluate.formattedTexts : null);

            if (!(String.IsNullOrEmpty(element.GoToArea) && String.IsNullOrEmpty(element.GoToScreen) && String.IsNullOrEmpty(element.GoToField)))
            {
                goToLinkData = (from log in failedRule.Log
                                where log.logEntries.Count > 0
                                from logEntry in log.logEntries
                                where (!String.IsNullOrEmpty(logEntry.fieldKey))
                                 && (logEntry.fieldKey.StartsWith(element.GoToArea + "," + element.GoToScreen + "," + element.GoToField))
                                select logEntry.fieldKey).FirstOrDefault();

                if (String.IsNullOrEmpty(goToLinkData))
                {
                    goToLinkData = element.GoToArea + "," + element.GoToScreen + "," + element.GoToField + ",0,0,0,0,0,0";
                }
            }

            failedRule.goTo = goToLinkData;
            return failedRule;
        }

        private FailedExecutionInfo CreateFailRule(Guid ruleId, string ruleNumber, string reason, List<dynamic> categories, TaxProfileArea taxProfileArea, string linkedQuery, int numberOfDiagnostics, IEnumerable<FunctionLog> log = null, IEnumerable<FormattedRuleText> formattedTexts = null)
        {
            return new FailedExecutionInfo() { RuleId = ruleId, Reason = reason, RuleNumber = ruleNumber, Categories = categories, TaxProfileArea = taxProfileArea, LinkedQuery = linkedQuery, Log = log, NumberOfDiagnostics = numberOfDiagnostics, FormattedTexts = formattedTexts };
        }

        private XmlDocument CreateXmlDocument(string xmlFileName)
        {
            XmlDocument xmlDocument = null;
            string pathForSaving = System.Configuration.ConfigurationManager.AppSettings["EvaluationRequestXMLPath"].ToString();
            if (Directory.Exists(pathForSaving))
            {
                xmlFileName = Path.Combine(pathForSaving, xmlFileName);
                xmlDocument = new XmlDocument();
                xmlDocument.Load(xmlFileName);
            }
            return xmlDocument;
        }

        private string ExtractDefaultNamespaceUri(XmlDocument xmlDocument)
        {
            IXPathNavigable innerDocument = xmlDocument;
            XPathNavigator rootNavigator = innerDocument.CreateNavigator();
            rootNavigator.MoveToFollowing(XPathNodeType.Element);
            string prefixUsed = rootNavigator.Prefix;
            return rootNavigator.GetNamespacesInScope(XmlNamespaceScope.All).Where(x => x.Key == prefixUsed).Select(x => x.Value).First();
        }
    }
}