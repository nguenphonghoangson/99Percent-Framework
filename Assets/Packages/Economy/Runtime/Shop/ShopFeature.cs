using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Iap;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Features.Shop
{
    /// <summary>
    ///     Provides <see cref="IShopService" />. Requires modules: Economy, Reward, Progression, Inventory.
    ///     Optional: IAP.
    /// </summary>
    public sealed class ShopFeature : IFeature
    {
        public const string Id = "shop";

        private readonly ShopConfig _config;

        public ShopFeature(ShopConfig config) => _config = config;

        public string FeatureId => Id;
        public string Name => Id;

        public void Install(IServiceRegistry registry)
        {
            registry.TryGet<IIapService>(out var iap);
            var unlocks = new UnlockEvaluator(registry.Require<IProgressionService>(), registry.Require<IInventoryService>());
            registry.Register<IShopService>(new ShopService(_config, registry.Require<IEconomyService>(),
                registry.Require<IRewardService>(), iap, unlocks, registry.Require<ISaveService>(),
                registry.Require<IEventBus>()));
        }
    }
}
