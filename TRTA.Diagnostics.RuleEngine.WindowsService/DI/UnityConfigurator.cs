using Common.Resources;
using Common.Resources.Cache;
using Microsoft.Practices.Unity;
using NHibernate;
using NHibernate.Context;
using Saxon.Api;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using TRTA.Diagnostics.BusinessRules.WebService.Services;
using TRTA.Diagnostics.RuleEngine.WindowsService.Metrics;
using TRTA.Diagnostics.RuleEngine.WindowsService.Services;
using TRTA.Diagnostics.RuleEngine.WindowsService.WorkQueue;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.DI
{
    public class UnityConfigurator
    {
        public void Configure(IUnityContainer container)
        {
            this.addBindings(container);
        }

        private void addBindings(IUnityContainer container) 
        {
            configureNHibernate(container);

            container.RegisterType<IRepository, NHibernateSimpleRepository>(new PerThreadLifetimeManager());
            container.RegisterType(typeof(IRuleLogicExecutionService<>), typeof(RuleLogicExecutionService<>), new TransientLifetimeManager());
            container.RegisterType(typeof(IRuleQueryExecuter<>), typeof(RuleQueryExecuter<>), new TransientLifetimeManager());
            container.RegisterType(typeof(ICache<,>), typeof(MultipleStopwatchCache<,>), "MultipleStopwatchCache", new ContainerControlledLifetimeManager());
            container.RegisterType(typeof(ICache<,>), typeof(SingleStopwatchCache<,>), "SingleStopwatchCache", new ContainerControlledLifetimeManager());
            container.RegisterType<IRuleExecutionElementFactory<EFileDocument>, EFileCERuleExecutionElementFactory>(new TransientLifetimeManager());
            container.RegisterType<Func<FieldService>>(new InjectionFactory(_container => new Func<FieldService>(() => _container.Resolve<FieldService>())));
            container.RegisterType<Func<RuleService>>(new InjectionFactory(_container => new Func<RuleService>(() => _container.Resolve<RuleService>())));
            container.RegisterType<IRuleEvaluationRequestsConsumer, RuleEvaluationRequestsConsumer>(new TransientLifetimeManager());
            container.RegisterType<IRuleEvaluationRequestsProducer, RuleEvaluationRequestsDatabaseProducer>(new TransientLifetimeManager());
            container.RegisterType<IEvaluationRequestsService, EvaluationRequestDBService>();            
            container.RegisterType<IValuesDictionary, ValuesDictionary>(new PerThreadLifetimeManager());
            container.RegisterType<Func<IEvaluationRequestsService>>(
                new InjectionFactory(c =>
                    new Func<IEvaluationRequestsService>(() => c.Resolve<IEvaluationRequestsService>())
                ));
            container.RegisterType<Func<RuleServiceInstance>>(
                new InjectionFactory(c =>
                    new Func<RuleServiceInstance>(() => c.Resolve<RuleServiceInstance>())
                ));
            container.RegisterType<Func<IRuleLogicExecutionService<EFileDocument>>>(
                new InjectionFactory(c =>
                    new Func<IRuleLogicExecutionService<EFileDocument>>(() => c.Resolve<IRuleLogicExecutionService<EFileDocument>>())
                ));
            container.RegisterType<Func<IValuesDictionary>>(
                new InjectionFactory(c =>
                    new Func<IValuesDictionary>(() => c.Resolve<IValuesDictionary>())
                ));

            container.RegisterType<ConcurrentDictionary<Tuple<string, string, string, string>, Stopwatch>>(new ContainerControlledLifetimeManager());
            //container.RegisterType<Common.Logging.ILoggerFactory, Common.Logging.Log4NetLoggerFactory>();
            int queueSize = int.Parse(ConfigurationManager.AppSettings["MaxQueueSize"]);
            BlockingCollection<RuleEvaluationRequest> blockingCollection = new BlockingCollection<RuleEvaluationRequest>(queueSize);
            container.RegisterInstance<BlockingCollection<RuleEvaluationRequest>>(blockingCollection);
            WorkQueueCounter counter = new WorkQueueCounter();
            container.RegisterInstance<WorkQueueCounter>(counter);
            PerformanceMetricsCollector _PerformanceMetricsCollector = new PerformanceMetricsCollector();
            container.RegisterInstance<PerformanceMetricsCollector>(_PerformanceMetricsCollector);
            Processor processor = new Processor();
            container.RegisterInstance<Processor>(processor);
            ILockProvider lockProvider = new LockProvider();
            container.RegisterInstance<ILockProvider>(lockProvider);
        }

        private void configureNHibernate(IUnityContainer container)
        {
            var sessionFactory = NHibernateSessionFactoryHelper.BuildWindowsServiceSessionFactory();

            container.RegisterInstance<ISessionFactory>(sessionFactory);
            container.RegisterType<ISession>(new InjectionFactory(createSession));
            /*container.RegisterType<IActionTransactionHelper, ActionTransactionHelper>();*/
        }

        private object createSession(IUnityContainer container)
        {
            var sessionFactory = container.Resolve<ISessionFactory>();
            if (!CurrentSessionContext.HasBind(sessionFactory))
            {
                var session = sessionFactory.OpenSession(new AuditInterceptor());
                CurrentSessionContext.Bind(session);
            }

            return sessionFactory.GetCurrentSession();
        }
    }
}