using System;
using NinetyNine.Core;

namespace NinetyNine.Persistence
{
    /// <summary>
    ///     Provides <see cref="ISaveService" /> over a storage provider (PlayerPrefs, file, cloud) and a serializer.
    ///     Install right after Core: almost every module keeps state through it.
    /// </summary>
    public sealed class SaveModule : IModule
    {
        private readonly ISaveProvider _provider;
        private readonly ISerializer _serializer;

        public SaveModule(ISaveProvider provider, ISerializer serializer)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public string Name => "Save";

        public void Install(IServiceRegistry registry) =>
            registry.Register<ISaveService>(new SaveService(_provider, _serializer));
    }
}
