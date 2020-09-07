using Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Models
{
    public class FailedExecutionInfo
    {
        public Guid RuleId { get; set; }
        public string RuleNumber { get; set; }
        //public string RuleText { get; set; }
        public string Reason { get; set; }
        public List<dynamic> Categories { get; set; }
        public TaxProfileArea TaxProfileArea { get; set; }
        public string LinkedQuery { get; set; }
        public IEnumerable<FunctionLog> Log { get; set; }
        public IEnumerable<FormattedRuleText> FormattedTexts { get; set; }
        public int NumberOfDiagnostics { get; set; }
        public string goTo { get; set; }
    }
}