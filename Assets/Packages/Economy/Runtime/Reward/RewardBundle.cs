using System;
using System.Collections.Generic;

namespace NinetyNine.Modules.Reward
{
    public enum RewardKind
    {
        Currency,
        Item
    }

    [Serializable]
    public struct RewardItem
    {
        public RewardKind kind;
        public string id;
        public long amount;

        public RewardItem(RewardKind kind, string id, long amount)
        {
            this.kind = kind;
            this.id = id;
            this.amount = amount;
        }

        public static RewardItem Currency(string currencyId, long amount) => new(RewardKind.Currency, currencyId, amount);

        public static RewardItem Item(string itemId, long quantity) => new(RewardKind.Item, itemId, quantity);

        public override string ToString() => $"{amount} {id}";
    }

    /// <summary>
    ///     What a feature pays out, as design data. Features describe rewards with this type and hand them to
    ///     <see cref="IRewardService" />; none of them writes to the wallet or inventory directly.
    /// </summary>
    [Serializable]
    public sealed class RewardBundle
    {
        public List<RewardItem> items = new();

        public bool IsEmpty => items == null || items.Count == 0;

        public static RewardBundle Of(params RewardItem[] items) => new() { items = new List<RewardItem>(items) };

        /// <summary>"50 coin + 1 booster_hammer" — debug and placeholder UI; real UI shows icons.</summary>
        public override string ToString() => IsEmpty ? string.Empty : string.Join(" + ", items);
    }
}
