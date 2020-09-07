using Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using TRTA.Diagnostics.RuleEngine.WindowsService.Models;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using Saxon.Api;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public interface IRuleExecutionElementFactory<TDocument>
    {
        IRuleExecutionElement<TDocument> CreateRuleExecutionElement(Rule rule, string defaultNameSpace = "", Processor processor = null);
    }
}
