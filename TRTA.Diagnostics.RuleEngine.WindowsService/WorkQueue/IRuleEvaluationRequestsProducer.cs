using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue
{
    interface IRuleEvaluationRequestsProducer
    {
        CancellationToken CancelToken { get; set; }

        void CreatePendingRequests();
    }
}
