using System;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Iap
{
    /// <summary>
    ///     Provides <see cref="IIapService" /> over the given provider. Requires Core. Leave the module out on a
    ///     build without a store; features treat IAP as optional.
    /// </summary>
    public sealed class IapModule : IModule
    {
        private readonly IIapProvider _provider;

        public IapModule(IIapProvider provider) =>
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        public string Name => "IAP";

        public void Install(IServiceRegistry registry) =>
            registry.Register<IIapService>(new IapService(_provider,
                registry.Require<ISaveService>(), registry.Require<IEventBus>()));
    }
}
