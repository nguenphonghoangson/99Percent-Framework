using System;
using System.Collections.Generic;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Composition root helper. Installs every module first, then every enabled feature, then initialises
    ///     all services in registration order. The split is enforced here so a feature can always assume the
    ///     modules it needs are present, whatever order the caller listed things in.
    /// </summary>
    public sealed class ModuleHost : IDisposable
    {
        private readonly ServiceContainer _container = new();
        private readonly List<IFeature> _features = new();
        private readonly List<IModule> _modules = new();
        private readonly HashSet<string> _names = new();
        private readonly List<string> _skippedFeatures = new();
        private bool _built;

        public IServiceResolver Services => _container;

        /// <summary>Feature ids that <see cref="IFeatureToggles" /> switched off during <see cref="Build" />.</summary>
        public IReadOnlyList<string> SkippedFeatures => _skippedFeatures;

        public ModuleHost AddModule(IModule module)
        {
            if (module is IFeature)
                throw new ArgumentException($"{module.Name} is a feature; add it with AddFeature.", nameof(module));

            Track(module);
            _modules.Add(module);
            return this;
        }

        public ModuleHost AddFeature(IFeature feature)
        {
            Track(feature);
            _features.Add(feature);
            return this;
        }

        public IServiceResolver Build()
        {
            if (_built) throw new InvalidOperationException("ModuleHost.Build can only run once.");
            _built = true;

            foreach (var module in _modules) Install(module);

            _container.TryGet<IFeatureToggles>(out var toggles);
            foreach (var feature in _features)
            {
                if (toggles != null && !toggles.IsEnabled(feature.FeatureId))
                {
                    _skippedFeatures.Add(feature.FeatureId);
                    continue;
                }

                Install(feature);
            }

            _container.InitializeAll();
            return _container;
        }

        public void Dispose() => _container.Dispose();

        private void Track(IModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            if (_built) throw new InvalidOperationException("Cannot add modules after Build.");
            if (!_names.Add(module.Name))
                throw new ArgumentException($"A module named {module.Name} is already added.", nameof(module));
        }

        private void Install(IModule module)
        {
            try
            {
                module.Install(_container);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Installing {module.Name} failed: {e.Message}", e);
            }
        }
    }
}
