using System;
using System.Collections.Generic;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Domain events (CurrencyChanged, LevelFinished…). Events are structs so publishing allocates
    ///     nothing beyond the handler snapshot.
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<T>(Action<T> handler) where T : struct;

        void Publish<T>(T evt) where T : struct;
    }

    public sealed class EventBus : IEventBus, IDisposable
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private readonly Action<Exception> _onListenerError;

        /// <param name="onListenerError">
        ///     Where a throwing listener's exception goes. The remaining listeners still run — one broken
        ///     view must not stop a purchase from being recorded. Null rethrows after all listeners ran.
        /// </param>
        public EventBus(Action<Exception> onListenerError = null) => _onListenerError = onListenerError;

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(typeof(T), out var list))
                _handlers[typeof(T)] = list = new List<Delegate>();

            list.Add(handler);
            return new Subscription(() => list.Remove(handler));
        }

        public void Publish<T>(T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list) || list.Count == 0) return;

            List<Exception> errors = null;
            foreach (var handler in list.ToArray())
                try
                {
                    ((Action<T>)handler)(evt);
                }
                catch (Exception e)
                {
                    if (_onListenerError != null) _onListenerError(e);
                    else (errors ??= new List<Exception>()).Add(e);
                }

            if (errors != null) throw new AggregateException(errors);
        }

        public void Dispose() => _handlers.Clear();

        private sealed class Subscription : IDisposable
        {
            private Action _unsubscribe;

            public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;

            public void Dispose()
            {
                _unsubscribe?.Invoke();
                _unsubscribe = null;
            }
        }
    }
}
