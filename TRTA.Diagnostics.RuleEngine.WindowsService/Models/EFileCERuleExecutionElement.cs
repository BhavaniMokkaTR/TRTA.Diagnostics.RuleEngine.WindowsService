using CodeEffects.Rule.Attributes;
using CodeEffects.Rule.Core;
using Common.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using TRTA.Diagnostics.BusinessRules.WebService.Services;
using Saxon.Api;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Configuration;
using Common.Resources.Cache;
namespace TRTA.Diagnostics.RuleEngine.WindowsService.Models
{
    public class EFileCERuleExecutionElement : IRuleExecutionElement<EFileDocument>
    {
        public Guid RuleId { get; set; }
        public string RuleNumber { get; set; }
        public string RuleText { get; set; }
        public string AlternateText { get; set; }
        public List<dynamic> Categories { get; set; }
        public TaxProfileArea TaxProfileArea { get; set; }
        public string LinkedQuery { get; set; }
        public int useAtLeast { get; set; }
        public List<int> Fields { get; set; }
        public Evaluator<EFileDocument> Evaluator { get; set; }
        private EFileCERuleEvaluator _evaluator;
        public string GoToArea { get; set; }
        public string GoToScreen { get; set; }
        public string GoToField { get; set; }
        public string XQuery { get; set; }
        public XQueryExecutable CompiledXQueryExecutable { get; set; }

        public EFileCERuleExecutionElement(string codeEffectsRuleXML, Func<FieldService> fieldServiceFactory, Func<RuleService> ruleServiceFactory, Guid ruleId, int precision, Func<IValuesDictionary> valuesDictionaryProvider, ICache<int, Field> fieldsCache, string xQuery, string currentNamespaceURI = "", Processor processor = null)
        {
            if (!string.IsNullOrEmpty(codeEffectsRuleXML))
            {
                MidpointRounding midpointRounding = MidpointRounding.ToEven;
                if (precision > -1)
                    midpointRounding = MidpointRounding.AwayFromZero;                

                Evaluator = new Evaluator<EFileDocument>(codeEffectsRuleXML, new EvaluationParameters { MidpointRounding = midpointRounding, Precision = precision });
                _evaluator = new EFileCERuleEvaluator(codeEffectsRuleXML, fieldServiceFactory, ruleServiceFactory, valuesDictionaryProvider, ruleId, fieldsCache: fieldsCache);
                _evaluator.Evaluator = Evaluator;
                XQuery = xQuery;
                CompiledXQueryExecutable = CreateXQueryExecutable(currentNamespaceURI, processor);
            }
            else
            {
                throw new InvalidOperationException("Error in EFileCERuleExecutionElement constructor");
            }
        }

        public bool Evaluate(EFileDocument document, int useAtLeast, string xquery, CurrentSelection currentSelection, string ruleText = "", Processor processor = null, XdmNode _XdmNode = null, List<int> fields = null, XQueryExecutable compiledXQueryExecutable = null)
        {
            if (_evaluator == null)
                throw new InvalidOperationException("Rule XML was invalid.");
            return _evaluator.Evaluate(document, useAtLeast, currentSelection, true, xquery, ruleText, processor, _XdmNode, fields, compiledXQueryExecutable);
        }

        private XQueryExecutable CreateXQueryExecutable(string currentNamespaceURI = "", Processor processor = null)
        {
            XQueryExecutable xQueryExecutable = null;
            if (processor != null && !string.IsNullOrEmpty(XQuery))
            {
                XQueryCompiler xQueryCompiler = processor.NewXQueryCompiler();
                xQueryCompiler.DeclareNamespace(_evaluator.RuleNamespace, currentNamespaceURI);
                xQueryExecutable = xQueryCompiler.Compile(XQuery);
            }
            return xQueryExecutable;
        }
    }
}