using Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using TRTA.Diagnostics.RuleEngine.WindowsService.Models;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using Saxon.Api;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public interface IRuleQueryExecuter<TDocumentType>
    {
        IEnumerable<IRuleExecutionElement<TDocumentType>> ExecuteRuleQuery(string year, string returnType, string jurisdiction, string schemaType, string defaultNameSpace = "", Processor processor = null);
    }
}
