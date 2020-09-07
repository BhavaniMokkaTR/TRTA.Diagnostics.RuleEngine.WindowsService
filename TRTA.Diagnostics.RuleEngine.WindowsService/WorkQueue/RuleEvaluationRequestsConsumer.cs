using Common.Resources;
//using Common.Logging;
using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TRTA.Diagnostics.RuleEngine.WindowsService.Services;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using log4net;
using log4net.Config;
using Saxon.Api;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue
{
    class RuleEvaluationRequestsConsumer : IRuleEvaluationRequestsConsumer
    {
        private BlockingCollection<RuleEvaluationRequest> _workQueue;
        private Func<IRuleLogicExecutionService<EFileDocument>> _logicExecutionServiceFactory;
        private Func<IEvaluationRequestsService> _evaluationRequestsServiceFactory;
        private IList<Task> _allTasks;
        public CancellationToken CancelToken { get; set; }
        //private readonly ILogger _logger;
        //private readonly TrmrLogContext _context;
        private static ILog _logger = LogManager.GetLogger("RuleEngineWindowsService");
        private Semaphore sem;
        WorkQueueCounter _counter;
        Processor _processor;
        public RuleEvaluationRequestsConsumer(BlockingCollection<RuleEvaluationRequest> workQueue, Func<IRuleLogicExecutionService<EFileDocument>> logicExecutionServiceFactory, Func<IEvaluationRequestsService> evaluationRequestsServiceFactory, WorkQueueCounter counter, Processor processor/*, ILoggerFactory loggerFactory*/)
        {
            //_logger = loggerFactory.GetLogger("RuleEngine_WindowsService");
            //_context = new TrmrLogContext("RuleEngine_WindowsService");
            this._workQueue = workQueue;
            this._logicExecutionServiceFactory = logicExecutionServiceFactory;
            this._evaluationRequestsServiceFactory = evaluationRequestsServiceFactory;
            this._allTasks = new List<Task>();
            this.sem = new Semaphore(workQueue.BoundedCapacity, workQueue.BoundedCapacity);
            this._counter = counter;
            this._processor = processor;

        }

        public void ProcessRequests()
        {
            //_context.StartEventLog("ProcessRequests start");
            _logger.Info("ProcessRequests");
            while (!CancelToken.IsCancellationRequested)
            {
                try
                {
                    RuleEvaluationRequest nextRequest = new RuleEvaluationRequest()
                    {
                        Id = 43888,
                        Year = "2015",
                        ReturnType = "1120",
                        Locator = "CTEST075",
                        SchemaType = "Jurisdiction",
                        DocumentToEvaluate = "9ca715b2-c923-4ede-8f95-619f96e91117-CTEST075.xil",
                        Jurisdiction = "Illinois"
                    };

                    //this.sem.WaitOne();

                    //Task newTask = Task.Run(() =>
                    //{
                    IEvaluationRequestsService evaluationRequestService = this._evaluationRequestsServiceFactory();
                        try
                        {
                            _logger.Info(String.Format("Processing RequestId {0} - Start", nextRequest.Id));

                            evaluationRequestService.UpdateRequestStatus(nextRequest.Id, RequestStatusConstants.IN_PROGRESS);
                            IRuleLogicExecutionService<EFileDocument> newService = _logicExecutionServiceFactory();

                            Stopwatch sw = Stopwatch.StartNew();

                            _logger.Info(String.Format("Start ExecuteRules for RequestId {0}", nextRequest.Id));
                            newService.ExecuteRules(nextRequest, _processor);
                            sw.Stop();
                            _logger.Info(String.Format("End ExecuteRules for RequestId {0} - Time  {1:0.00}s", nextRequest.Id, sw.ElapsedMilliseconds / 1000));

                            evaluationRequestService.UpdateRequestStatus(nextRequest.Id, RequestStatusConstants.COMPLETED);
                            _logger.Info(String.Format("Processing RequestId {0} - End", nextRequest.Id));
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(string.Format("Error Processing RequestId {0}", nextRequest.Id), ex);
                            evaluationRequestService.UpdateRequestStatus(nextRequest.Id, RequestStatusConstants.FAILED);
                        }
                        finally
                        {
                            _counter.count--;
                            sem.Release();
                        }
                    //});
                    //_allTasks.Add(newTask);

                    //newTask.ContinueWith(
                    //    t => { _allTasks.Remove(t); }, CancelToken
                    //);

                }
                catch (OperationCanceledException e)
                {
                    _logger.Error("Operation was cancelled.", e);
                }
            }
            Task.WaitAll(_allTasks.Where(t => t.Status != TaskStatus.RanToCompletion).ToArray());
            this.sem.Dispose();
            //_context.EndEventLog();
        }
    }
}
