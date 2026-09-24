using System;

namespace NinetyNine.Modules.Economy
{
    [Serializable]
    public struct Cost
    {
        public string currencyId;
        public long amount;

        public Cost(string currencyId, long amount)
        {
            this.currencyId = currencyId;
            this.amount = amount;
        }

        public bool IsFree => amount <= 0 || string.IsNullOrEmpty(currencyId);

        public override string ToString() => IsFree ? "Free" : $"{amount} {currencyId}";
    }
}
