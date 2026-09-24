using NinetyNine.Core;

namespace NinetyNine.Modules.Inventory
{
    /// <summary>
    ///     Item ownership and quantity (boosters, avatars, cosmetics). The catalog is open: any id can be
    ///     held, and whoever defines the item (a feature's config) decides what it means.
    /// </summary>
    public interface IInventoryService
    {
        long GetQuantity(string itemId);

        bool Owns(string itemId);

        Result Add(string itemId, long quantity, string source);

        /// <summary>All-or-nothing, like spending currency.</summary>
        Result TryConsume(string itemId, long quantity, string source);
    }

    public static class InventoryErrors
    {
        public const string InvalidItem = "inventory.invalid_item";
        public const string InvalidQuantity = "inventory.invalid_quantity";
        public const string NotEnoughItems = "inventory.not_enough_items";
    }

    public readonly struct ItemChangedEvent
    {
        public ItemChangedEvent(string itemId, long previous, long current, string source)
        {
            ItemId = itemId;
            Previous = previous;
            Current = current;
            Source = source;
        }

        public string ItemId { get; }
        public long Previous { get; }
        public long Current { get; }
        public string Source { get; }
    }
}
