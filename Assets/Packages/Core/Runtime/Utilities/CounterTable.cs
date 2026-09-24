using System;
using System.Collections.Generic;

namespace NinetyNine.Core
{
    [Serializable]
    public sealed class CounterEntry
    {
        public string id;
        public long value;
    }

    /// <summary>
    ///     id → count map that survives JsonUtility (which cannot serialise dictionaries). Linear lookup:
    ///     tables hold a handful of currencies or a few dozen items, well below where hashing pays off.
    /// </summary>
    [Serializable]
    public sealed class CounterTable
    {
        public List<CounterEntry> entries = new();

        public long Get(string id)
        {
            var entry = Find(id);
            return entry?.value ?? 0;
        }

        public bool Contains(string id) => Find(id) != null;

        public void Set(string id, long value)
        {
            var entry = Find(id);
            if (entry == null) entries.Add(new CounterEntry { id = id, value = value });
            else entry.value = value;
        }

        private CounterEntry Find(string id)
        {
            foreach (var entry in entries)
                if (entry.id == id)
                    return entry;
            return null;
        }
    }
}
