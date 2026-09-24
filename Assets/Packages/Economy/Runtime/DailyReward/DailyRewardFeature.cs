using NinetyNine.Core;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Features.DailyReward
{
    /// <summary>
    ///     Provides <see cref="IDailyRewardService" />. Requires modules: Reward, Progression, Inventory, and
    ///     Core's time service.
    /// </summary>
    public sealed class DailyRewardFeature : IFeature
    {
        public const string Id = "daily_reward";

        private readonly DailyRewardConfig _config;

        public DailyRewardFeature(DailyRewardConfig config) => _config = config;

        public string FeatureId => Id;
        public string Name => Id;

        public void Install(IServiceRegistry registry)
        {
            var unlocks = new UnlockEvaluator(registry.Require<IProgressionService>(), registry.Require<IInventoryService>());
            registry.Register<IDailyRewardService>(new DailyRewardService(_config, registry.Require<IRewardService>(),
                registry.Require<ITimeService>(), unlocks, registry.Require<ISaveService>(),
                registry.Require<IEventBus>()));
        }
    }
}
