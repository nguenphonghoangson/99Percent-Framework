using System;
using System.Collections.Generic;
using UnityEngine;

namespace NinetyNine.Modules.Economy
{
    [Serializable]
    public sealed class CurrencyDefinition
    {
        public string id;

        [Tooltip("Granted once, the first time this currency is seen on a save.")]
        public long initialBalance;

        [Tooltip("0 = no cap.")]
        public long maxBalance;
    }

    [CreateAssetMenu(menuName = "NinetyNine/Modules/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        public List<CurrencyDefinition> currencies = new();

        public CurrencyDefinition Find(string currencyId) => currencies.Find(c => c.id == currencyId);
    }
}
