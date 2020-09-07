using Common.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRTA.Diagnostics.RuleEngine.WindowsService.Models;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue;
using Saxon.Api;
namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public interface IRuleLogicExecutionService<TDocumentType> where TDocumentType : IDiagnosable
    {
        IEnumerable<FailedExecutionInfo> ExecuteRules(RuleEvaluationRequest nextRequest, Processor processor);
    }
}
