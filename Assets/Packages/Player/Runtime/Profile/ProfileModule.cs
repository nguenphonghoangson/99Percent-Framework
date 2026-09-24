using System;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Profile
{
    /// <summary>
    ///     Provides <see cref="IProfileService" />. Requires Core. Uses an <see cref="IProfileNameFilter" /> when
    ///     one is registered earlier.
    /// </summary>
    public sealed class ProfileModule : IModule
    {
        private readonly Func<string> _defaultName;

        /// <param name="defaultName">Name given on a fresh save. Defaults to "Player" + 4 random digits.</param>
        public ProfileModule(Func<string> defaultName = null) =>
            _defaultName = defaultName ?? (() => "Player" + new Random().Next(1000, 10000));

        public string Name => "Profile";

        public void Install(IServiceRegistry registry)
        {
            registry.TryGet<IProfileNameFilter>(out var filter);
            registry.Register<IProfileService>(new ProfileService(registry.Require<ISaveService>(),
                registry.Require<IEventBus>(), filter, _defaultName));
        }
    }
}
