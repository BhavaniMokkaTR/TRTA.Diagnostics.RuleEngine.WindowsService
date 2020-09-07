using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public class BaseService
    {
        public IRepository _repository;
        public BaseService(IRepository repository)
        {
            _repository = repository;
        }
    }
}