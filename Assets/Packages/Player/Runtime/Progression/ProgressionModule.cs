using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Progression
{
    /// <summary>Provides <see cref="IProgressionService" />. Requires Core.</summary>
    public sealed class ProgressionModule : IModule
    {
        public string Name => "Progression";

        public void Install(IServiceRegistry registry) =>
            registry.Register<IProgressionService>(new ProgressionService(
                registry.Require<ISaveService>(), registry.Require<IEventBus>()));
    }
}
