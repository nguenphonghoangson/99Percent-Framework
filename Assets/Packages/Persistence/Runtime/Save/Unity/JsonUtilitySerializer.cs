using NinetyNine.Core;
using UnityEngine;

namespace NinetyNine.Persistence
{
    /// <summary>
    ///     JsonUtility: fields only, no dictionaries (hence <see cref="CounterTable" />). Swap for Newtonsoft or
    ///     MemoryPack behind <see cref="ISerializer" /> without touching any module.
    /// </summary>
    public sealed class JsonUtilitySerializer : ISerializer
    {
        public string Serialize<T>(T value) => JsonUtility.ToJson(value);

        public T Deserialize<T>(string data) => JsonUtility.FromJson<T>(data);
    }
}
