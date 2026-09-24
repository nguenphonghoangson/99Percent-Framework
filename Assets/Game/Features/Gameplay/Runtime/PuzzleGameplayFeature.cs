using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.PuzzleGameplay
{
    /// <summary>
    ///     Provides <see cref="IPuzzleGameplayService" />. Requires modules: Puzzle, Puzzle.Level, Progression,
    ///     Economy, Reward.
    /// </summary>
    public sealed class PuzzleGameplayFeature : IFeature
    {
        public const string Id = "puzzle_gameplay";

        private readonly PuzzleGameplayConfig _config;

        public PuzzleGameplayFeature(PuzzleGameplayConfig config) => _config = config;

        public string FeatureId => Id;
        public string Name => Id;

        public void Install(IServiceRegistry registry) =>
            registry.Register<IPuzzleGameplayService>(new PuzzleGameplayService(_config,
                registry.Require<ILevelService>(), registry.Require<IPuzzleSessionFactory>(),
                registry.Require<IProgressionService>(), registry.Require<IEconomyService>(),
                registry.Require<IRewardService>(), registry.Require<IEventBus>()));
    }
}
