using NinetyNine.Core;
using UnityEngine;

namespace NinetyNine.Persistence
{
    /// <summary>
    ///     PlayerPrefs buffers writes in memory until <see cref="Flush" />, so the several saves of one
    ///     purchase (spend, grant, count) reach disk together.
    /// </summary>
    public sealed class PlayerPrefsSaveProvider : ISaveProvider
    {
        private readonly string _prefix;

        public PlayerPrefsSaveProvider(string prefix = "nn.") => _prefix = prefix;

        public bool TryRead(string key, out string data)
        {
            var fullKey = _prefix + key;
            data = PlayerPrefs.HasKey(fullKey) ? PlayerPrefs.GetString(fullKey) : null;
            return data != null;
        }

        public void Write(string key, string data) => PlayerPrefs.SetString(_prefix + key, data);

        public void Delete(string key) => PlayerPrefs.DeleteKey(_prefix + key);

        public void Flush() => PlayerPrefs.Save();
    }
}
