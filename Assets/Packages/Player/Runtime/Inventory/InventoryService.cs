using System;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Inventory
{
    [Serializable]
    internal sealed class InventoryState
    {
        public CounterTable items = new();
    }

    internal sealed class InventoryService : IInventoryService, IInitializable
    {
        private const string SaveKey = "module.inventory";

        private readonly IEventBus _events;
        private readonly ISaveService _save;
        private InventoryState _state = new();

        public InventoryService(ISaveService save, IEventBus events)
        {
            _save = save;
            _events = events;
        }

        public void Initialize() => _state = _save.Load<InventoryState>(SaveKey);

        public long GetQuantity(string itemId) => _state.items.Get(itemId);

        public bool Owns(string itemId) => GetQuantity(itemId) > 0;

        public Result Add(string itemId, long quantity, string source)
        {
            if (string.IsNullOrEmpty(itemId)) return Result.Fail(InventoryErrors.InvalidItem);
            if (quantity <= 0) return Result.Fail(InventoryErrors.InvalidQuantity);

            Apply(itemId, GetQuantity(itemId) + quantity, source);
            return Result.Success;
        }

        public Result TryConsume(string itemId, long quantity, string source)
        {
            if (string.IsNullOrEmpty(itemId)) return Result.Fail(InventoryErrors.InvalidItem);
            if (quantity <= 0) return Result.Fail(InventoryErrors.InvalidQuantity);
            if (GetQuantity(itemId) < quantity) return Result.Fail(InventoryErrors.NotEnoughItems);

            Apply(itemId, GetQuantity(itemId) - quantity, source);
            return Result.Success;
        }

        private void Apply(string itemId, long next, string source)
        {
            var previous = GetQuantity(itemId);
            _state.items.Set(itemId, next);
            _save.Save(SaveKey, _state);
            _events.Publish(new ItemChangedEvent(itemId, previous, next, source));
        }
    }
}
