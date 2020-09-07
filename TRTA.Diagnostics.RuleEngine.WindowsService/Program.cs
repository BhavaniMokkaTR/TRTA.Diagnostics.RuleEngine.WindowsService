//using Common.Logging;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Topshelf.Unity;
using TRTA.Diagnostics.RuleEngine.WindowsService.DI;

namespace TRTA.Diagnostics.RuleEngine.WindowsService
{
    
    class Program
    {
        static void Main(string[] args)
        {
            //System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            var container = new UnityContainer();
            container.RegisterType<BootstrapService>();

            var configurator = new UnityConfigurator();

            //Log4NetSetup.Configure();

            configurator.Configure(container);

            HostFactory.Run(x =>
            {
                x.UseUnityContainer(container);
                x.Service<BootstrapService>(s =>
                {
                    s.ConstructUsingUnityContainer();
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Rule Engine Jobs Processor");
                x.SetDisplayName("Rule Engine Evaluator");
                x.SetServiceName("RuleEngineEvaluator");
                x.SetInstanceName(ConfigurationManager.AppSettings["InstanceName"]);
            });
        }
    }
}
