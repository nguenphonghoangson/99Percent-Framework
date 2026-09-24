using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Features.Profile
{
    /// <summary>
    ///     Provides <see cref="IProfileFeatureService" />. Requires modules: Profile, Economy, Progression, Inventory.
    /// </summary>
    public sealed class ProfileFeature : IFeature
    {
        public const string Id = "profile";

        private readonly ProfileFeatureConfig _config;

        public ProfileFeature(ProfileFeatureConfig config) => _config = config;

        public string FeatureId => Id;
        public string Name => Id;

        public void Install(IServiceRegistry registry)
        {
            var unlocks = new UnlockEvaluator(registry.Require<IProgressionService>(), registry.Require<IInventoryService>());
            registry.Register<IProfileFeatureService>(new ProfileFeatureService(_config,
                registry.Require<IProfileService>(), registry.Require<IEconomyService>(), unlocks,
                registry.Require<ISaveService>()));
        }
    }
}
