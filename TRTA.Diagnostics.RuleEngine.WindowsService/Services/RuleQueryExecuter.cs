using System;
using System.Collections.Generic;
using System.Linq;
using TRTA.Diagnostics.RuleEngine.WindowsService.Models;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using Common.Resources.Cache;
using Microsoft.Practices.Unity;
using Saxon.Api;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public class RuleQueryExecuter<TDocumentType> : IRuleQueryExecuter<TDocumentType>
    {
        private IRepository _repository;
        private ILockProvider _lockProvider;
        private IRuleExecutionElementFactory<TDocumentType> _ruleExecutionElementFactory;
        private ICache<Tuple<string, string, string, string>, IEnumerable<IRuleExecutionElement<TDocumentType>>> _rulesCache;

        public RuleQueryExecuter(IRepository repository, IRuleExecutionElementFactory<TDocumentType> ruleExecutionElementFactory, [Dependency("MultipleStopwatchCache")] ICache<Tuple<string, string, string, string>, IEnumerable<IRuleExecutionElement<TDocumentType>>> rulesCache, ILockProvider lockProvider)
        {
            _repository = repository;
            _ruleExecutionElementFactory = ruleExecutionElementFactory;
            _rulesCache = rulesCache;
            _lockProvider = lockProvider;
        }

        public IEnumerable<IRuleExecutionElement<TDocumentType>> ExecuteRuleQuery(string year, string returnType, string jurisdiction, string schemaType, string defaultNameSpace = "", Processor processor = null)
        {
            var key = new Tuple<string, string, string, string>(year, returnType, jurisdiction, schemaType);
            _rulesCache.CheckCacheState(key);

            if (!_rulesCache.Contains(key))
            {
                lock (_lockProvider.GetLockFor(key))
                {
                    if (!_rulesCache.Contains(key))
                    {
                        List<Guid> lst = new List<Guid>() { new Guid("b23384ef-83f7-4c3c-b5ad-a5fb0113e6dc") };



                        var rules = _repository.GetAll<Rule>().Where(r =>
                            r.IsDeleted == false &&
                            r.IsActive == true &&
                            r.IsPublished == true &&
                            r.Logic != null &&
                            r.Logic != string.Empty &&
                            r.TaxAppYear.Year == year &&
                            (r.ReturnType.Name == returnType || r.SecondaryReturnTypes.Any(x => x.Name == returnType)) &&
                            (r.Jurisdiction.Name == jurisdiction || r.SecondaryJurisdictions.Any(x => x.Name == jurisdiction)) &&
                            r.SchemaType.Name == schemaType && lst.Contains(r.Id)).ToList();
                        var ruleExecutionElements = rules.Select<Rule, IRuleExecutionElement<TDocumentType>>(rule => _ruleExecutionElementFactory.CreateRuleExecutionElement(rule, defaultNameSpace, processor)).ToList();

                        var goToLinkInfo = _repository.GetAll<GoToLink>().Where(g =>
                            g.Area != null && g.Area != string.Empty &&
                            g.Screen != null && g.Screen != string.Empty &&
                            g.Field != null && g.Field != string.Empty);

                        foreach (IRuleExecutionElement<TDocumentType> element in ruleExecutionElements)
                        {
                            var a = goToLinkInfo.FirstOrDefault(x => x.Rule.Id == element.RuleId);
                            if (a != null)
                            {
                                element.GoToArea = a.Area;
                                element.GoToScreen = a.Screen;
                                element.GoToField = a.Field;
                            }
                        }

                        _rulesCache.AddElement(key, ruleExecutionElements);
                        return ruleExecutionElements;
                    }
                }
            }
            return _rulesCache[key];
        }
    }
}