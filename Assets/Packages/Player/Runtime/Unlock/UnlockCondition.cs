using System;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Progression;

namespace NinetyNine.Modules.Unlock
{
    public enum UnlockKind
    {
        Always,
        ReachLevel,
        OwnItem
    }

    /// <summary>
    ///     Design-side gate shared by features: "Shop opens at level 5", "this avatar needs item X". Not a
    ///     feature itself — a helper features build on, so it may depend on modules only.
    /// </summary>
    [Serializable]
    public struct UnlockCondition
    {
        public UnlockKind kind;

        /// <summary>1-based level number the player must have reached (<see cref="UnlockKind.ReachLevel" />).</summary>
        public int levelNumber;

        public string itemId;

        public static UnlockCondition Always => default;

        public static UnlockCondition AtLevel(int levelNumber) => new() { kind = UnlockKind.ReachLevel, levelNumber = levelNumber };

        public static UnlockCondition WithItem(string itemId) => new() { kind = UnlockKind.OwnItem, itemId = itemId };
    }

    public sealed class UnlockEvaluator
    {
        private readonly IInventoryService _inventory;
        private readonly IProgressionService _progression;

        public UnlockEvaluator(IProgressionService progression, IInventoryService inventory)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public bool IsMet(UnlockCondition condition) => condition.kind switch
        {
            UnlockKind.Always => true,
            UnlockKind.ReachLevel => _progression.CurrentLevelNumber >= condition.levelNumber,
            UnlockKind.OwnItem => _inventory.Owns(condition.itemId),
            _ => false
        };
    }
}
