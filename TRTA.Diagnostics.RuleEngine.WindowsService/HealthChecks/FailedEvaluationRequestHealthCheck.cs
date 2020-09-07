using Common.HealthCheck;
using Common.HealthCheck.HealthChecks;
using System;
using System.Diagnostics;
using System.Linq;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.HealthChecks
{
    class FailedEvaluationRequestHealthCheck : HealthCheck
    {
        
        public double Threshold { get; set; }
        public int TimeIntervalInMinutes { get; set; }

        private IRepository _repository;

        public FailedEvaluationRequestHealthCheck()
        {
        }

        public FailedEvaluationRequestHealthCheck(IRepository repository)
        {
            _repository = repository;
        }

        public override HealthResult Execute()
        {
            var healthResult = new HealthResult
            {
                Status = new HealthStatus(HealthStatus.Success),
                Name = Name,
                Message = string.Empty,
                ExecutionTime = "0ms",
                Type = typeof(FailedEvaluationRequestHealthCheck)
            };

            _repository = CreateRepository(healthResult);
            if (_repository == null) return healthResult;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var instanceName = Environment.MachineName;
            var testDate = DateTime.Now.AddMinutes(-TimeIntervalInMinutes);

            var evaluationRequests = _repository.GetAll<EvaluationRequest>().Where(x => x.DateModified != null && x.DateModified > testDate && x.InstanceName == instanceName).ToList();
           
            if (evaluationRequests.Any())
            {
                var failedRequestsCount = evaluationRequests.Count(x => x.RequestStatus == "Failed");

                double percentage = failedRequestsCount * 100 / (double)evaluationRequests.Count;
                if (percentage > Threshold)
                {
                    healthResult.Status = new HealthStatus(HealthStatus.Error);
                    healthResult.ErrorCode = "100";
                    healthResult.Message +=
                        string.Format(
                            "For the instance name {0}, the percentage of failed evaluation requests in the last {1} minutes has exceeded {2}%. Failed: {3}, In Progress: {4}, In Queue: {5}, Pending: {6}, Completed: {7}.",
                            instanceName, TimeIntervalInMinutes, Threshold, failedRequestsCount, evaluationRequests.Count(x => x.RequestStatus == "In Progress"), evaluationRequests.Count(x => x.RequestStatus == "In Queue"),
                            evaluationRequests.Count(x => x.RequestStatus == "Pending"), evaluationRequests.Count(x => x.RequestStatus == "Completed"));
                }
                else
                {
                    healthResult.Message +=
                        string.Format(
                            "For the instance name {0}, the percentage of failed evaluation requests in the last {1} minutes has NOT exceeded {2}%. Failed: {3}, In Progress: {4}, In Queue: {5}, Pending: {6}, Completed: {7}.",
                            instanceName, TimeIntervalInMinutes, Threshold, failedRequestsCount, evaluationRequests.Count(x => x.RequestStatus == "In Progress"), evaluationRequests.Count(x => x.RequestStatus == "In Queue"),
                            evaluationRequests.Count(x => x.RequestStatus == "Pending"), evaluationRequests.Count(x => x.RequestStatus == "Completed"));
                }
            }
            else
            {
                healthResult.Message += string.Format("No evaluation requests found for instance name {0} in the last {1} minutes.", instanceName,
                    TimeIntervalInMinutes);
            }

            stopWatch.Stop();

            healthResult.ExecutionTime = stopWatch.ElapsedMilliseconds + "ms";

            return healthResult;
        }

        private IRepository CreateRepository(HealthResult healthResult)
        {
            IRepository repository = null;

            try
            {
                var sessionManager = new SessionManager();
                repository = new Repository(sessionManager);
            }
            catch (Exception e)
            {
                var exceptionMessage = e.InnerException != null ? e.InnerException.Message : e.Message;
                healthResult.Status = new HealthStatus(HealthStatus.Error);
                healthResult.Message = "Unable to create a database connection. " + exceptionMessage;
                healthResult.ErrorCode = "101";
                return repository;
            }

            return repository;  
        }
    }
}
