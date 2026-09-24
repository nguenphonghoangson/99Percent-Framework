using NinetyNine.Core;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Rules;

namespace NinetyNine.Modules.Puzzle
{
    public interface IPuzzleSessionFactory
    {
        IPuzzleRules Rules { get; }

        PuzzleSession Create(LevelDefinition level, int levelIndex);
    }

    internal sealed class PuzzleSessionFactory : IPuzzleSessionFactory
    {
        public PuzzleSessionFactory(IPuzzleRules rules) => Rules = rules;

        public IPuzzleRules Rules { get; }

        public PuzzleSession Create(LevelDefinition level, int levelIndex) => new(level, levelIndex, Rules);
    }

    /// <summary>
    ///     Umbrella of the puzzle sub-modules (Board, Move, Rule, Objective, Turn, Result). Provides
    ///     <see cref="IPuzzleSessionFactory" />. Pass the game's own <see cref="IPuzzleRules" />; the default is
    ///     the <see cref="TapClearRules" /> reference mechanic.
    /// </summary>
    public sealed class PuzzleModule : IModule
    {
        private readonly IPuzzleRules _rules;

        public PuzzleModule(IPuzzleRules rules = null) => _rules = rules ?? new TapClearRules();

        public string Name => "Puzzle";

        public void Install(IServiceRegistry registry) =>
            registry.Register<IPuzzleSessionFactory>(new PuzzleSessionFactory(_rules));
    }
}
