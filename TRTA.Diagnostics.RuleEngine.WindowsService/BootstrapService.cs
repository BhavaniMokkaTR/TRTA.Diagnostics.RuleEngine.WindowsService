using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue;
using TRTA.Diagnostics.RuleEngine.WindowsService.Services;
using Timer = System.Threading.Timer;
using System.Configuration;
using System.ServiceModel;
using System.ServiceModel.Web;
using Nancy.Hosting.Wcf;
using log4net.Config;
using log4net;
using TRTA.Diagnostics.RuleEngine.WindowsService.Metrics;

namespace TRTA.Diagnostics.RuleEngine.WindowsService
{
    class BootstrapService
    {
        private Timer _producerTimer;
        private IRuleEvaluationRequestsConsumer _consumer;
        private Task _consumerTask;
        private IRuleEvaluationRequestsProducer _producer;
        private CancellationTokenSource _cancellationTokenSource;
        private Func<RuleServiceInstance> _ruleServiceInstanceFactory;
        private PerformanceMetricsCollector _metricsCollector;
        public BootstrapService(IRuleEvaluationRequestsConsumer consumer, IRuleEvaluationRequestsProducer producer, Func<RuleServiceInstance> ruleServiceInstanceFactory, PerformanceMetricsCollector performanceMetricsCollector)
        {
            this._consumer = consumer;
            this._producer = producer;
            this._ruleServiceInstanceFactory = ruleServiceInstanceFactory;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._consumer.CancelToken = this._cancellationTokenSource.Token;
            this._producer.CancelToken = this._cancellationTokenSource.Token;
            _metricsCollector = performanceMetricsCollector;
        }
        public void Start()
        {
            XmlConfigurator.Configure();
            StartHealthCheckMonitor();
            _ruleServiceInstanceFactory().CreateOrUpdateRuleInstance();

            int timerInterval; //milliseconds
            int.TryParse(ConfigurationManager.AppSettings["ServiceTimerInterval"], out timerInterval);

            this._producerTimer = new Timer(
                new TimerCallback(
                    target =>
                    {
                        Console.WriteLine("It is {0} and all is well", DateTime.Now);
                        _producer.CreatePendingRequests();
                    }), null, timerInterval, timerInterval);
            this._consumerTask = Task.Run(() => _consumer.ProcessRequests());
        }
        public void Stop()
        {
            StopHealthCheckMonitor();
            this._cancellationTokenSource.Cancel();
            this._cancellationTokenSource.Dispose();
            WaitHandle handle = new AutoResetEvent(false);
            _producerTimer.Dispose(handle);
            _consumerTask.Wait();
            handle.WaitOne();
            _ruleServiceInstanceFactory().MakeInactiveRuleInstance();
            _metricsCollector.Stop();
        }

        public ServiceHost serviceHost = null;
        private WebServiceHost _healthCheckServiceHost;

        private void StartHealthCheckMonitor()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
            }
            serviceHost = new ServiceHost(typeof(BootstrapService));
            serviceHost.Open();

            string httpEndPoint = ConfigurationManager.AppSettings["HttpEndPoint"];
            _healthCheckServiceHost = new WebServiceHost(new NancyWcfGenericService(),
                new Uri(httpEndPoint));
            _healthCheckServiceHost.AddServiceEndpoint(typeof(NancyWcfGenericService), new WebHttpBinding(), "");
            _healthCheckServiceHost.Open();
        }

        private void StopHealthCheckMonitor()
        {
            _healthCheckServiceHost.Close();
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }
    }
}
