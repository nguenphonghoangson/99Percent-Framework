using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Economy
{
    /// <summary>Provides <see cref="IEconomyService" />. Requires Core.</summary>
    public sealed class EconomyModule : IModule
    {
        private readonly EconomyConfig _config;

        public EconomyModule(EconomyConfig config) => _config = config;

        public string Name => "Economy";

        public void Install(IServiceRegistry registry) =>
            registry.Register<IEconomyService>(new EconomyService(_config,
                registry.Require<ISaveService>(), registry.Require<IEventBus>()));
    }
}
