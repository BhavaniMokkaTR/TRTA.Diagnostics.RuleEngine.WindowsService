using Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using Saxon.Api;
namespace TRTA.Diagnostics.RuleEngine.WindowsService.Models
{
    public interface IRuleExecutionElement<TDocumentType>
    {
        Guid RuleId { get; set; }
        string RuleNumber { get; set; }
        string RuleText { get; set; }
        string AlternateText { get; set; }
        List<dynamic> Categories { get; set; }
        TaxProfileArea TaxProfileArea { get; set; }
        string LinkedQuery { get; set; }
        int useAtLeast { get; set; }
        List<int> Fields { get; set; }
        string GoToArea { get; set; }
        string GoToScreen { get; set; }
        string GoToField { get; set; }
        string XQuery { get; set; }
        XQueryExecutable CompiledXQueryExecutable { get; set; }
        bool Evaluate(EFileDocument document, int useAtLeast, string xquery, CurrentSelection currentSelection, string ruleText, Processor processor = null, XdmNode _XdmNode = null, List<int> fields = null, XQueryExecutable compiledXQueryExecutable = null);
    }
}