using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using Common.Resources;
using log4net;
namespace TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue
{
    class EvaluationRequestDBService : IEvaluationRequestsService
    {
        private IRepository _repository;
        private string _instanceName;

        private static ILog _logger = LogManager.GetLogger("RuleEngineWindowsService");
        public EvaluationRequestDBService(IRepository repository)
        {
            this._repository = repository;
            this._instanceName = Environment.MachineName;//ConfigurationManager.AppSettings["InstanceName"];
        }

        public void UpdateRequestStatus(int requestId, string status)
        {
            _logger.Info(String.Format("EvaluationRequestDBService UpdateRequestStatus method start RequestId {0} , Status {1}",requestId,status));

            var request = this._repository.Get<EvaluationRequest>(requestId);
            request.RequestStatus = status;
            this._repository.Save(request);

            _logger.Info(String.Format("EvaluationRequestDBService UpdateRequestStatus method end RequestId {0} , Status {1}", requestId, status));
        }

        public void UpdateRequestStatusToInPending(int requestId, string status, string instanceName)
        {
            var request = this._repository.Get<EvaluationRequest>(requestId);
                request.RequestStatus = status;
                request.InstanceName = instanceName;
                this._repository.Save(request);

            _logger.Info(String.Format("EvaluationRequestDBService UpdateRequestStatusToInPending method end RequestId {0} , Status {1}, InstanceName {2}", requestId, status, instanceName));
        }

        public void UpdateRequestStatusToInQueue(int requestId, string status)
        {
            var request = this._repository.Get<EvaluationRequest>(requestId);

            if (request.RequestStatus == RequestStatusConstants.PENDING)
            {
                request.RequestStatus = status;
                request.InstanceName = Environment.MachineName;
                this._repository.Save(request);
            }

            _logger.Info(String.Format("EvaluationRequestDBService UpdateRequestStatusToInQueue method end RequestId {0} , Status {1} ", requestId, status));

        }
        public string GetRequestStatus(int requestId)
        {
            return this._repository.Get<EvaluationRequest>(requestId).RequestStatus;
        }

        public IEnumerable<EvaluationRequest> GetRequests(string status)
        {
            return this._repository.GetAll<EvaluationRequest>().Where(x => x.RequestStatus == status && x.InstanceName == this._instanceName).ToList();
        }

        public IQueryable<EvaluationRequest> QueryRequests(string status)
        {
            return this._repository.GetAll<EvaluationRequest>().Where(x => x.RequestStatus == status && x.InstanceName == this._instanceName);
        }

        public IQueryable<EvaluationRequest> GetBigPendingRequest(int fileSizeConfig)
        {
            return this._repository.GetAll<EvaluationRequest>().Where(x => x.RequestStatus != RequestStatusConstants.PENDING 
                                                && x.RequestStatus != RequestStatusConstants.COMPLETED
                                                && x.RequestStatus != RequestStatusConstants.FAILED
                                                && x.FileSize >= fileSizeConfig && x.InstanceName == this._instanceName);
        }

        public IQueryable<EvaluationRequest> GetNextRequests(int maxFileSize, int maxFileSizeCount)
        {
            string spQuery = "EXEC USP_Get_NextRequest @InstanceName=:instanceName, @MaxFileSize=:maxFileSize, @MaxFileSizeCount=:maxFileSizeCount";

            IDictionary<string, object> parametersForQuery = new Dictionary<string, object>();
            parametersForQuery.Add("instanceName", this._instanceName);
            parametersForQuery.Add("maxFileSize", maxFileSize);
            parametersForQuery.Add("maxFileSizeCount", maxFileSizeCount);

            return this._repository.ExecuteStoredProcedure<EvaluationRequest>(spQuery, parametersForQuery).AsQueryable();
        }

        public void Dispose()
        {
            this._repository.Dispose();
        }
    }
}
