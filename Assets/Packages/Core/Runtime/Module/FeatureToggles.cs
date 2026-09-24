using System.Collections.Generic;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Kill switch per feature, read once at boot. Remote Config provides its own implementation later;
    ///     modules are never toggled — only features are.
    /// </summary>
    public interface IFeatureToggles
    {
        bool IsEnabled(string featureId);
    }

    public sealed class FeatureToggles : IFeatureToggles
    {
        private readonly bool _enabledByDefault;
        private readonly Dictionary<string, bool> _overrides = new();

        public FeatureToggles(bool enabledByDefault = true) => _enabledByDefault = enabledByDefault;

        public FeatureToggles Set(string featureId, bool enabled)
        {
            _overrides[featureId] = enabled;
            return this;
        }

        public bool IsEnabled(string featureId) =>
            _overrides.TryGetValue(featureId, out var enabled) ? enabled : _enabledByDefault;
    }
}
