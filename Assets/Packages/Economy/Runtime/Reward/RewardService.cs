using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Inventory;

namespace NinetyNine.Modules.Reward
{
    internal sealed class RewardService : IRewardService
    {
        private readonly IEconomyService _economy;
        private readonly IEventBus _events;
        private readonly IInventoryService _inventory;

        public RewardService(IEconomyService economy, IInventoryService inventory, IEventBus events)
        {
            _economy = economy;
            _inventory = inventory;
            _events = events;
        }

        public Result Validate(RewardBundle bundle)
        {
            if (bundle == null || bundle.IsEmpty) return Result.Fail(RewardErrors.EmptyBundle);

            foreach (var item in bundle.items)
            {
                if (string.IsNullOrEmpty(item.id) || item.amount <= 0) return Result.Fail(RewardErrors.InvalidEntry);
                if (item.kind == RewardKind.Currency && !_economy.IsKnown(item.id))
                    return Result.Fail(RewardErrors.UnknownCurrency);
            }

            return Result.Success;
        }

        public Result Grant(RewardBundle bundle, string source)
        {
            var validation = Validate(bundle);
            if (!validation.IsSuccess) return validation;

            foreach (var item in bundle.items)
                if (item.kind == RewardKind.Currency) _economy.Add(item.id, item.amount, source);
                else _inventory.Add(item.id, item.amount, source);

            _events.Publish(new RewardGrantedEvent(bundle, source));
            return Result.Success;
        }
    }
}
