using Common.Resources;
using log4net;
using log4net.Config;
//using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public class RuleExecutionLogService : BaseService
    {
        //private readonly ILogger _logger;
        //private readonly TrmrLogContext _context;
        private static ILog _logger = LogManager.GetLogger("RuleEngineWindowsService");
        public RuleExecutionLogService(IRepository repository/*, ILoggerFactory loggerFactory*/)
            : base(repository)
        {
            //_logger = loggerFactory.GetLogger("RuleEngine_WindowsService");
            //_context = new TrmrLogContext("RuleEngine_WindowsService");
        }

        public string Save(string year, string jurisdiction, string returnType, Guid? locatorId, EfileSchemaType schemaType, EvaluationRequest evaluationRequest, string status, string DiagnosticInfo = null, string Id = null)
        {
            //_context.StartEventLog("Save start");
            _logger.Info("RuleExecutionLogService Save method start");
            try
            {                
                RuleExecutionLog LogInformation = null;
                if (string.IsNullOrEmpty(Id))
                {
                    LogInformation = new RuleExecutionLog();
                    LogInformation.Locator = new Locator();
                    LogInformation.Locator.Id = (Guid)locatorId;
                    LogInformation.Id = Guid.NewGuid();
                    LogInformation.StartTime = DateTime.Now;
                    LogInformation.Status = status;
                    LogInformation.SchemaType = schemaType;
                    LogInformation.EvaluationRequest = evaluationRequest;
                    LogInformation.Endtime = DateTime.Now;
                   // LogInformation.Jurisdiction = jurisdiction;
                    _repository.Insert<RuleExecutionLog>(LogInformation);
                }
                else
                {
                    LogInformation = _repository.Get<RuleExecutionLog>(new Guid(Id));
                    if (LogInformation != null)
                    {
                        LogInformation.Endtime = DateTime.Now;
                        LogInformation.DiagnosticInfo = DiagnosticInfo;
                        LogInformation.Status = status;
                        LogInformation.EvaluationRequest = evaluationRequest;
                        _repository.Merge<RuleExecutionLog>(LogInformation);
                    }
                }
                Id = (LogInformation != null) ? LogInformation.Id.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.Error("Error in RuleExecutionLogService Save Method ", ex);
            }
            _logger.Info("RuleExecutionLogService Save method End");
            //_context.EndEventLog();
            return Id;
        }

        public Locator GetOrSaveLocator(string year, string jurisdiction, string returnType, string locator)
        {
            //_context.StartEventLog("GetOrSaveLocator start");
            _logger.Info("RuleExecutionLogService GetOrSaveLocator Start");
            Locator Locatordetails = null;
            try
            {
                Locatordetails = _repository.GetAll<Locator>().FirstOrDefault(l => l.TaxAppYear == year &&
                                                                                    l.ReturnType == returnType &&
                                                                                    l.Jurisdiction == jurisdiction &&
                                                                                    l.Name == locator);

                if (Locatordetails == null)
                {
                    Locatordetails = new Locator
                    {
                        TaxAppYear = year,
                        ReturnType = returnType,
                        Name = locator,
                        Jurisdiction = jurisdiction
                    };

                    _repository.Save(Locatordetails);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error in RuleExecutionLogService GetOrSaveLocator", ex);
            }
            //_context.EndEventLog();
            _logger.Info("RuleExecutionLogService GetOrSaveLocator End");
            return Locatordetails;
        }
        

    }
}