using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Inventory
{
    /// <summary>Provides <see cref="IInventoryService" />. Requires Core.</summary>
    public sealed class InventoryModule : IModule
    {
        public string Name => "Inventory";

        public void Install(IServiceRegistry registry) =>
            registry.Register<IInventoryService>(new InventoryService(
                registry.Require<ISaveService>(), registry.Require<IEventBus>()));
    }
}
