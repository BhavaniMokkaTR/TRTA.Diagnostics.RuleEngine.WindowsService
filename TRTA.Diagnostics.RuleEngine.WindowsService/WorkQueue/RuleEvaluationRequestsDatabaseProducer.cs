using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using Common.Resources;
//using Common.Logging;
using System.Timers;
using System.Threading;
using System.Configuration;
using log4net;
using log4net.Config;
using TRTA.Diagnostics.RuleEngine.WindowsService.Metrics;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue
{
    class RuleEvaluationRequestsDatabaseProducer : IRuleEvaluationRequestsProducer
    {
        private Func<IEvaluationRequestsService> _evaluationRequestsServiceFactory;
        private BlockingCollection<RuleEvaluationRequest> _workQueue;
        //private readonly ILogger _logger;
        //private readonly TrmrLogContext _context;
        public CancellationToken CancelToken { get; set; }
        private static ILog _logger = LogManager.GetLogger("RuleEngineWindowsService");
        private WorkQueueCounter _counter;
        private PerformanceMetricsCollector _metricsCollector;
        private float _cpuUsageThreshold;
        private float _memoryUsageThreshold;
        private int fileSizeConfig = 0;
        private int maxFileSizeCount = 0;
        private int timeOut = 0;
        public RuleEvaluationRequestsDatabaseProducer(IEvaluationRequestsService evaluationRequestsService, BlockingCollection<RuleEvaluationRequest> workQueue, Func<IEvaluationRequestsService> evaluationRequestsServiceFactory/*, ILoggerFactory loggerFactory*/, WorkQueueCounter counter, PerformanceMetricsCollector performanceMetricsCollector)
        {
            //_logger = loggerFactory.GetLogger("RuleEngine_WindowsService");
            //_context = new TrmrLogContext("RuleEngine_WindowsService");
            this._workQueue = workQueue;
            this._evaluationRequestsServiceFactory = evaluationRequestsServiceFactory;
            this._counter = counter;
            _metricsCollector = performanceMetricsCollector;
            InitializeCapacityThresholds();
        }

        private void InitializeCapacityThresholds()
        {
            // server capacity thresholds
            _cpuUsageThreshold = float.Parse(ConfigurationManager.AppSettings["CpuUsageThreshold"]);
            _memoryUsageThreshold = float.Parse(ConfigurationManager.AppSettings["MemoryUsageThreshold"]);

            if (_cpuUsageThreshold <= 0)
                _cpuUsageThreshold = 70;

            if (_memoryUsageThreshold <= 0)
                _memoryUsageThreshold = 80;

             fileSizeConfig = int.Parse(System.Configuration.ConfigurationManager.AppSettings["EvaluationRequestFileSize"].ToString());
             maxFileSizeCount = int.Parse(System.Configuration.ConfigurationManager.AppSettings["MaxFileSizeCount"].ToString());
             timeOut = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ServiceTimerInterval"].ToString());

        }

        public void CreatePendingRequests()
        {
            //_context.StartEventLog("CreatePendingRequests start");
            _logger.Info("CreatePendingRequests");
            float avgCpu = _metricsCollector.Cpu;
            float commitCharge = _metricsCollector.PercentCommitCharge;


            if ((!this.CancelToken.IsCancellationRequested) && (avgCpu < _cpuUsageThreshold) && (commitCharge < _memoryUsageThreshold) && (_counter.count < _workQueue.BoundedCapacity))
            {
                IEvaluationRequestsService evaluationRequestsService = this._evaluationRequestsServiceFactory();
                try
                {
                    IEnumerable<RuleEvaluationRequest> pendingRequests = GetNextRequests(evaluationRequestsService, fileSizeConfig, maxFileSizeCount);

                    foreach (RuleEvaluationRequest request in pendingRequests)
                    {
                        bool added = _workQueue.TryAdd(request, timeOut, CancelToken);
                        if (added)
                        {
                            _counter.count++;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Error in CreatePendingRequests", e);
                }
                finally
                {
                    //TODO Verify this code if needed
                    if (this.CancelToken.IsCancellationRequested)
                    {
                        foreach (var request in _workQueue.GetConsumingEnumerable())
                            evaluationRequestsService.UpdateRequestStatusToInPending(request.Id, RequestStatusConstants.PENDING, string.Empty);
                    }
                }
            }

        }


        private IEnumerable<RuleEvaluationRequest> GetNextRequests(
            IEvaluationRequestsService evaluationRequestsService, int fileSizeConfig, int maxFileSizeCount)
        {
            _logger.Info("GetNextRequests Start");
            var ruleEvaluationRequests = new List<RuleEvaluationRequest>();

            List<EvaluationRequest> pendingRequests = evaluationRequestsService.GetNextRequests(fileSizeConfig, maxFileSizeCount).ToList();
            foreach (var pendingRequest in pendingRequests)
            {
                ruleEvaluationRequests.Add(new RuleEvaluationRequest()
                {
                    Id = pendingRequest.Id,
                    Year = pendingRequest.TaxAppYear,
                    ReturnType = pendingRequest.ReturnType,
                    SchemaType = pendingRequest.SchemaType,
                    Locator = pendingRequest.Locator,
                    Jurisdiction = pendingRequest.Jurisdiction,
                    DocumentToEvaluate = pendingRequest.DocumentToEvaluate,
                    FileSize = pendingRequest.FileSize
                });
            }

            _logger.Info("GetNextRequests End");
            return ruleEvaluationRequests;
        }
    }
}
