using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    using RuleGroupKey = Tuple<string, string, string, string>;

    public interface ILockProvider
    {
        object GetLockFor(RuleGroupKey key);
    }
}
