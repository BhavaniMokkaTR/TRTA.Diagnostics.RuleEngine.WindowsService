using System;
using System.Collections.Generic;
using System.Linq;
using Common.HealthCheck;
using Common.HealthCheck.Template;
using Nancy;
using Newtonsoft.Json;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.HealthChecks
{
    public class HealthCheckModule : NancyModule
    {
        private readonly VelocityTemplate template;

        private readonly IHealthCheckHandler healthCheckHandler;

        private readonly string templateName;

        public HealthCheckModule() : base("/healthcheck")
        {
            RegisterRoutes();

            template = new VelocityTemplate(new List<string>(new[] {
                typeof(IHealthCheck).Assembly.GetName().Name
            }));

            templateName = @"Common.HealthCheck.Template.Views.Index.htm";

            healthCheckHandler = IoCContainer.Instance.HealthCheckHandler;
        }

        public HealthCheckModule(IHealthCheckHandler healthCheckHandler, List<string> templateAssemblies, string templateName)
            : base("/healthcheck")
        {
            RegisterRoutes();
            template = new VelocityTemplate(templateAssemblies);
            this.templateName = templateName;
            this.healthCheckHandler = healthCheckHandler;
        }

        private void RegisterRoutes()
        {
            const string baseRoute = @"/";

            Get[baseRoute]    = _ => Index();
            Post[baseRoute]   = _ => Create();
            Delete[baseRoute] = _ => Destroy();

            Get[@"/status"] = _ => Status();
        }

        public string Index()
        {
            string result;
            var acceptHeader = Request.Headers["Accept"].FirstOrDefault();

            var healthCheckResults = healthCheckHandler.Execute();

            if (null != acceptHeader && acceptHeader.Contains("application/json"))
            {
                result = JsonConvert.SerializeObject(healthCheckResults);
            }
            else
            {
                result = template.RenderTemplate(templateName,
                                                 new Dictionary<string, object> { { "healthCheckResults", healthCheckResults } });
            }

            return result;
        }

        public string Destroy()
        {
            healthCheckHandler.InvalidateCache();
            return string.Empty;
        }

        public string Create()
        {
            throw new NotImplementedException();
        }

        public string Status()
        {
            return healthCheckHandler.Execute().Count(x => x.Status.Value == HealthStatus.Error) > 0
                ? @"0"//Fail
                : @"1";//Pass
        }
    }
}