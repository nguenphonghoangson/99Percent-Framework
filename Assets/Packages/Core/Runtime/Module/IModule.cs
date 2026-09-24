namespace NinetyNine.Core
{
    /// <summary>
    ///     A technical or business capability that owns its own state and exposes it through a Service
    ///     (Economy, Inventory, Board, Level…). A module has no UI, knows nothing about features, and is
    ///     always installed.
    /// </summary>
    public interface IModule
    {
        string Name { get; }

        /// <summary>
        ///     Register the services this module provides. Resolve hard dependencies with
        ///     <see cref="IServiceResolver.Require{T}" /> here, so a wrong install order fails at boot rather
        ///     than at the first call.
        /// </summary>
        void Install(IServiceRegistry registry);
    }

    /// <summary>
    ///     A player-facing capability (Shop, Daily Reward…) built by composing modules. A feature owns its
    ///     design config, flow rules and presenter; it may depend on any module but never on another
    ///     feature — cross-feature reactions go through <see cref="IEventBus" /> domain events.
    ///     A feature switched off in <see cref="IFeatureToggles" /> is not installed at all.
    /// </summary>
    public interface IFeature : IModule
    {
        string FeatureId { get; }
    }
}
