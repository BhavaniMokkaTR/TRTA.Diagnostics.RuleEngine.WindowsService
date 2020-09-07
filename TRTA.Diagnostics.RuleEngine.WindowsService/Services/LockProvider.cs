using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRTA.Diagnostics.RuleEngine.WindowsService.Services
{
    using System.Collections.Concurrent;
    using RuleGroupKey = Tuple<string, string, string, string>;

    public class LockProvider : ILockProvider
    {
        private ConcurrentDictionary<RuleGroupKey, object> _locks;

        public LockProvider()
        {
            this._locks = new ConcurrentDictionary<RuleGroupKey, object>();
        }

        public object GetLockFor(RuleGroupKey key)
        {
            if (!_locks.ContainsKey(key))
            {
                _locks.TryAdd(key, new object());
            }
            return _locks[key];
        }
    }
}
