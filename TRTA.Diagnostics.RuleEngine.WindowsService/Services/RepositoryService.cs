using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRTA.Diagnostics.BusinessRules.WebService.Domain.Database;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    public class RepositoryService
    {
        protected IRepository _repository;

        public RepositoryService(IRepository repository)
        {
            this._repository = repository;
        }

    }
}