using System;
using System.Collections.Generic;

namespace NinetyNine.Core.Pooling
{
    /// <summary>
    ///     Optional reset hooks for pooled objects that carry state between uses (a tile's colour, a VFX
    ///     timer). Objects without state need not implement it.
    /// </summary>
    public interface IPoolable
    {
        void OnTakenFromPool();

        void OnReturnedToPool();
    }

    public interface IObjectPool<T> where T : class
    {
        int CountInactive { get; }

        T Get();

        /// <summary>Throws on a double release: two owners of one instance is a bug, not a recoverable state.</summary>
        void Release(T item);
    }

    /// <summary>
    ///     Engine-free pool, so pure C# modules (Board, Puzzle) and server code can use it. GameObject pools are
    ///     this class with Instantiate / SetActive / Destroy passed as callbacks from Unity-side code. Lives in its
    ///     own namespace so it never clashes with <c>UnityEngine.Pool.ObjectPool</c> in files that use both.
    /// </summary>
    public sealed class ObjectPool<T> : IObjectPool<T>, IDisposable where T : class
    {
        private readonly Func<T> _create;
        private readonly Stack<T> _inactive = new();
        private readonly int _maxInactive;
        private readonly Action<T> _onDestroy;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        /// <param name="maxInactive">Idle instances kept; releases beyond it are destroyed.</param>
        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null,
            Action<T> onDestroy = null, int maxInactive = 256)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            if (maxInactive < 0) throw new ArgumentOutOfRangeException(nameof(maxInactive));

            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            _maxInactive = maxInactive;
        }

        public int CountInactive => _inactive.Count;

        /// <summary>Instances handed out and not yet released.</summary>
        public int CountActive { get; private set; }

        public T Get()
        {
            var item = _inactive.Count > 0 ? _inactive.Pop() : _create();
            CountActive++;
            _onGet?.Invoke(item);
            (item as IPoolable)?.OnTakenFromPool();
            return item;
        }

        public void Release(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            // Linear, like CounterTable: pools hold tens of idle instances, not thousands.
            if (_inactive.Contains(item)) throw new InvalidOperationException("Item released to the pool twice.");

            CountActive--;
            (item as IPoolable)?.OnReturnedToPool();
            _onRelease?.Invoke(item);

            if (_inactive.Count < _maxInactive) _inactive.Push(item);
            else _onDestroy?.Invoke(item);
        }

        /// <summary>Creates idle instances up front (level load) so the first frames of play do not allocate.</summary>
        public void Prewarm(int count)
        {
            while (_inactive.Count < Math.Min(count, _maxInactive))
            {
                var item = _create();
                _onRelease?.Invoke(item);
                _inactive.Push(item);
            }
        }

        /// <summary>Destroys idle instances. Instances still handed out stay with their owners.</summary>
        public void Clear()
        {
            while (_inactive.Count > 0) _onDestroy?.Invoke(_inactive.Pop());
        }

        public void Dispose() => Clear();
    }
}
