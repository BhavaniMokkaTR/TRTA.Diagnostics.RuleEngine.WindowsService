using Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue
{
    public class RuleEvaluationRequest
    {
        public int Id { get; set; }
        public string Year { get; set; }
        public string ReturnType { get; set; }
        public string Jurisdiction { get; set; }
        public string Locator { get; set; }
        public string SchemaType { get; set; }
        public string DocumentToEvaluate { get; set; }
        public Int64 FileSize { get; set; }
    }
}
