using System;
using NinetyNine.Core;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Provides <see cref="INavigator" />, <see cref="IUIFactory" /> and <see cref="IUILayers" />. Requires Core.
    ///     Creates the persistent
    ///     <see cref="UIRoot" /> at install; disposing the host destroys it.
    /// </summary>
    public sealed class UIModule : IModule
    {
        private readonly UICatalog _catalog;

        public UIModule(UICatalog catalog) =>
            _catalog = catalog ? catalog : throw new ArgumentNullException(nameof(catalog));

        public string Name => "UI";

        public void Install(IServiceRegistry registry)
        {
            var root = UIRoot.Create(_catalog);
            var factory = new PrefabUIFactory(_catalog, root.Staging);
            registry.Register<IUIFactory>(factory);
            registry.Register<IUILayers>(root);
            registry.Register<INavigator>(new Navigator(factory, root, registry.Require<IEventBus>()));
        }
    }
}
