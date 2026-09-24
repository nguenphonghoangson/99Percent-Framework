using System;
using System.Collections.Generic;
using NinetyNine.Core;

namespace NinetyNine.Persistence
{
    /// <summary>Storage backend: PlayerPrefs, file, cloud. Knows strings, not types.</summary>
    public interface ISaveProvider
    {
        bool TryRead(string key, out string data);

        void Write(string key, string data);

        void Delete(string key);

        /// <summary>Commit buffered writes to disk. Called on pause/quit.</summary>
        void Flush();
    }

    /// <summary>
    ///     Typed save/load on top of a provider. Each module or feature owns one key and one state class, so
    ///     a module can be reset or migrated on its own.
    /// </summary>
    public interface ISaveService
    {
        /// <summary>The stored value, or a fresh <typeparamref name="T" /> when missing or unreadable.</summary>
        T Load<T>(string key) where T : class, new();

        void Save<T>(string key, T value) where T : class;

        void Delete(string key);

        void Flush();
    }

    internal sealed class SaveService : ISaveService
    {
        private readonly ISaveProvider _provider;
        private readonly ISerializer _serializer;

        public SaveService(ISaveProvider provider, ISerializer serializer)
        {
            _provider = provider;
            _serializer = serializer;
        }

        public T Load<T>(string key) where T : class, new()
        {
            if (!_provider.TryRead(key, out var raw) || string.IsNullOrEmpty(raw)) return new T();

            try
            {
                return _serializer.Deserialize<T>(raw) ?? new T();
            }
            catch (Exception)
            {
                // Keep the unreadable blob: the next Save overwrites the key, and support may need it back.
                _provider.Write(key + ".corrupt", raw);
                return new T();
            }
        }

        public void Save<T>(string key, T value) where T : class =>
            _provider.Write(key, _serializer.Serialize(value ?? throw new ArgumentNullException(nameof(value))));

        public void Delete(string key) => _provider.Delete(key);

        public void Flush() => _provider.Flush();
    }

    public sealed class InMemorySaveProvider : ISaveProvider
    {
        private readonly Dictionary<string, string> _data = new();

        public bool TryRead(string key, out string data) => _data.TryGetValue(key, out data);

        public void Write(string key, string data) => _data[key] = data;

        public void Delete(string key) => _data.Remove(key);

        public void Flush() { }
    }
}
