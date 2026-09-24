using System;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Infrastructure every other module relies on: time, event bus and feature toggles. Save lives in the
    ///     Persistence package (<c>SaveModule</c>) so Core stays engine-free and storage-agnostic.
    /// </summary>
    public sealed class CoreModule : IModule
    {
        private readonly IEventBus _eventBus;
        private readonly ITimeService _time;
        private readonly IFeatureToggles _toggles;

        public CoreModule(ITimeService time, IEventBus eventBus = null, IFeatureToggles toggles = null)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            _eventBus = eventBus;
            _toggles = toggles;
        }

        public string Name => "Core";

        public void Install(IServiceRegistry registry)
        {
            registry.Register(_time);
            registry.Register(_eventBus ?? new EventBus());
            registry.Register(_toggles ?? new FeatureToggles());
        }
    }
}
