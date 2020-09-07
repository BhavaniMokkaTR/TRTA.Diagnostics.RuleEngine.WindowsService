using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;
using TRTA.Diagnostics.BusinessRules.WebService.Domain;
using Common.Resources;
using System.Timers;
using System.Threading;
using System.Configuration;
//using Common.Logging;
using log4net;
using log4net.Config;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    class RuleServiceInstance : BaseService
    {
        //private readonly ILogger _logger;
        //private readonly TrmrLogContext _context;
        private static ILog _logger = LogManager.GetLogger("RuleEngineWindowsService");
        public RuleServiceInstance(IRepository repository/*, ILoggerFactory loggerFactory*/)
            : base(repository)
        {
            //_logger = loggerFactory.GetLogger("RuleEngine_WindowsService");
            //_context = new TrmrLogContext("RuleEngine_WindowsService");
        }

        public void CreateOrUpdateRuleInstance()
        {
            //_context.StartEventLog("CreateOrUpdateRuleInstance start");
            _logger.Info("CreateOrUpdateRuleInstance");
            RuleInstance ruleInstance = null;
            try
            {
                ruleInstance = _repository.GetAll<RuleInstance>().FirstOrDefault(l => l.InstanceName == Environment.MachineName);

                //if thre is not rule instance then create
                if (ruleInstance == null)
                {
                    ruleInstance = new RuleInstance
                    {
                        InstanceName = Environment.MachineName,
                        QueueSize = int.Parse(ConfigurationManager.AppSettings["MaxQueueSize"]),
                        Active = true
                    };

                    _repository.Save(ruleInstance);
                }
                else
                {
                    int queueSizeConfig = int.Parse(ConfigurationManager.AppSettings["MaxQueueSize"]);
                    //if exist rule instance check if is diference to updated.
                    if ((!ruleInstance.Active) || (ruleInstance.QueueSize != queueSizeConfig))
                    {
                        ruleInstance.QueueSize = queueSizeConfig;
                        ruleInstance.Active = true;
                        _repository.Save(ruleInstance);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error in locator CreateOrUpdateRuleInstance", ex);
            }
            //_context.EndEventLog();
        }

        public void MakeInactiveRuleInstance()
        {
            RuleInstance ruleInstance = null; ;
            try
            {
                ruleInstance = _repository.GetAll<RuleInstance>().FirstOrDefault(l => l.InstanceName == Environment.MachineName);

                if (ruleInstance != null)
                {
                    ruleInstance.Active = false;
                    _repository.Save(ruleInstance);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
