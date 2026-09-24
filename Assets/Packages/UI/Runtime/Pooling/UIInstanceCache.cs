using System;
using System.Collections.Generic;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Idle keep-alive instances, one per key. One is enough: the same screen is rarely open twice, and a
    ///     second concurrent opening simply gets a fresh instance that is destroyed on release.
    /// </summary>
    internal sealed class UIInstanceCache
    {
        private readonly Dictionary<string, UIView> _idle = new();

        public bool TryTake(string key, out UIView view)
        {
            if (!_idle.TryGetValue(key, out view) || !view) return false;

            _idle.Remove(key);
            return true;
        }

        /// <summary>False when an idle instance is already cached — the caller destroys the extra one.</summary>
        public bool Put(string key, UIView view)
        {
            if (_idle.TryGetValue(key, out var existing) && existing) return false;

            _idle[key] = view;
            return true;
        }

        /// <summary>Removes and returns the idle instances whose key matches <paramref name="match" /> (all when null).</summary>
        public List<UIView> Drain(Func<string, bool> match = null)
        {
            var drained = new List<UIView>();
            foreach (var key in new List<string>(_idle.Keys))
            {
                if (match != null && !match(key)) continue;

                if (_idle[key]) drained.Add(_idle[key]);
                _idle.Remove(key);
            }

            return drained;
        }
    }
}
