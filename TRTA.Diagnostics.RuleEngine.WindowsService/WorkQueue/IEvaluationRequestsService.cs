using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue
{
    interface IEvaluationRequestsService : IDisposable
    {
        void UpdateRequestStatus(int requestId, string status);
        void UpdateRequestStatusToInPending(int requestId, string status, string instanceName);
        void UpdateRequestStatusToInQueue(int requestId, string status);
        string GetRequestStatus(int requestId);
        IEnumerable<EvaluationRequest> GetRequests(string status);
        IQueryable<EvaluationRequest> QueryRequests(string status);
        IQueryable<EvaluationRequest> GetBigPendingRequest(int fileSizeConfig);
        IQueryable<EvaluationRequest> GetNextRequests(int maxFileSize, int maxFileSizeCount);
    }
}
