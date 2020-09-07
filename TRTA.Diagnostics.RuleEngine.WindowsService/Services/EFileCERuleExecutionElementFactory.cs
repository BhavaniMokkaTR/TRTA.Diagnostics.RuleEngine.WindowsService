using Common.Resources;
using Common.Resources.Cache;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Services;
using TRTA.Diagnostics.RuleEngine.WindowsService.Models;
using Saxon.Api;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public class EFileCERuleExecutionElementFactory : IRuleExecutionElementFactory<EFileDocument>
    {
        private Func<FieldService> _fieldServiceFactory;
        private Func<RuleService> _ruleServiceFactory;
        private Func<IValuesDictionary> _valuesDictionaryProvider;
        private ICache<int, Field> _fieldsCache;

        public EFileCERuleExecutionElementFactory(Func<FieldService> fieldServiceFactory, Func<RuleService> ruleServiceFactory, Func<IValuesDictionary> valuesDictionaryProvider, [Dependency("SingleStopwatchCache")] ICache<int, Field> fieldsCache)
        {
            this._fieldServiceFactory = fieldServiceFactory;
            this._ruleServiceFactory = ruleServiceFactory;
            this._valuesDictionaryProvider = valuesDictionaryProvider;
            this._fieldsCache = fieldsCache;
        }

        public IRuleExecutionElement<EFileDocument> CreateRuleExecutionElement(Rule rule, string currentNamespaceURI = "", Processor processor = null)
        {
            try
            {
                string fieldsId = rule.PlaceholderFields;
                List<int> fieldIdsList = new List<int>();
                if (fieldsId != null)
                    fieldIdsList = JsonConvert.DeserializeObject<List<int>>(fieldsId).ToList();
                return new EFileCERuleExecutionElement(rule.Logic, _fieldServiceFactory, _ruleServiceFactory, rule.Id, (rule.Precision == null || rule.Precision < 0) ? -1 : rule.Precision.Value, _valuesDictionaryProvider, _fieldsCache, rule.XQuery, currentNamespaceURI, processor) { RuleId = rule.Id, RuleNumber = rule.RuleNumber, RuleText = rule.RuleText, AlternateText = rule.AlternateText, Categories = rule.Categories.Select(c => new { Id = c.Id, Name = c.Name }).ToList<dynamic>(), TaxProfileArea = rule.TaxProfileArea, LinkedQuery = rule.LinkedQuery, useAtLeast = rule.UseAtLeast, Fields = fieldIdsList};
            }
            catch (Exception ex)
            {
                return new EFileCERuleExecutionElement(null, _fieldServiceFactory, _ruleServiceFactory, rule.Id, (rule.Precision == null || rule.Precision < 0) ? -1 : rule.Precision.Value, _valuesDictionaryProvider, _fieldsCache, rule.XQuery) { RuleId = rule.Id, RuleNumber = rule.RuleNumber, RuleText = rule.RuleText, AlternateText = rule.AlternateText, Categories = rule.Categories.Select(c => new { Id = c.Id, Name = c.Name }).ToList<dynamic>(), TaxProfileArea = rule.TaxProfileArea, LinkedQuery = rule.LinkedQuery, useAtLeast = rule.UseAtLeast/*, Fields = fieldIdsList*/ };
            }
        }
    }
}