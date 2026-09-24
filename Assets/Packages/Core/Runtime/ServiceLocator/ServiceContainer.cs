using System;
using System.Collections.Generic;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Service Locator keyed by contract type. One instance may be registered under several contracts;
    ///     it is initialised and disposed once.
    /// </summary>
    public sealed class ServiceContainer : IServiceRegistry, IDisposable
    {
        private readonly List<object> _instances = new();
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));

            var key = typeof(T);
            if (_services.ContainsKey(key))
                throw new InvalidOperationException($"Service {key.Name} is already registered.");

            _services[key] = service;
            if (!_instances.Contains(service)) _instances.Add(service);
        }

        public T Require<T>() where T : class =>
            TryGet<T>(out var service)
                ? service
                : throw new InvalidOperationException(
                    $"Service {typeof(T).Name} is not registered. Install the module that provides it before the one that needs it.");

        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var instance))
            {
                service = (T)instance;
                return true;
            }

            service = null;
            return false;
        }

        public bool Has<T>() where T : class => _services.ContainsKey(typeof(T));

        public void InitializeAll()
        {
            // Snapshot: an Initialize that registers something late must not break the iteration.
            foreach (var instance in _instances.ToArray())
                if (instance is IInitializable initializable)
                    initializable.Initialize();
        }

        public void Dispose()
        {
            for (var i = _instances.Count - 1; i >= 0; i--)
                if (_instances[i] is IDisposable disposable)
                    disposable.Dispose();

            _instances.Clear();
            _services.Clear();
        }
    }
}
