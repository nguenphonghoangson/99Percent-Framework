using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Inventory;

namespace NinetyNine.Modules.Reward
{
    /// <summary>Provides <see cref="IRewardService" />. Requires Economy and Inventory.</summary>
    public sealed class RewardModule : IModule
    {
        public string Name => "Reward";

        public void Install(IServiceRegistry registry) =>
            registry.Register<IRewardService>(new RewardService(registry.Require<IEconomyService>(),
                registry.Require<IInventoryService>(), registry.Require<IEventBus>()));
    }
}
