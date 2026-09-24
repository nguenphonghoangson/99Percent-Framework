using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.PuzzleGameplay
{
    /// <summary>
    ///     The main level loop: pick the level from Progression + Level, run a Puzzle session, then apply the
    ///     result — advance, pay the first-clear reward, sell a revive.
    /// </summary>
    public interface IPuzzleGameplayService
    {
        PuzzleSession ActiveSession { get; }

        int CurrentLevelNumber { get; }

        /// <summary>Quits any session still running first.</summary>
        PuzzleSession StartCurrentLevel();

        /// <summary>Replay of a cleared level, or the current one. Throws for a level not reached yet.</summary>
        PuzzleSession StartLevel(int progressIndex);

        Cost ReviveCost { get; }

        int ReviveMoves { get; }

        /// <summary>Paid on the first clear of a level.</summary>
        RewardBundle FirstClearReward { get; }

        /// <summary>Out of moves and the per-level revive limit not reached, regardless of funds.</summary>
        bool ReviveAvailable { get; }

        /// <summary><see cref="ReviveAvailable" /> and affordable.</summary>
        bool CanRevive { get; }

        Result TryRevive();
    }

    public static class PuzzleGameplayErrors
    {
        public const string NoActiveSession = "puzzle.no_active_session";
        public const string NotOutOfMoves = "puzzle.not_out_of_moves";
        public const string ReviveLimitReached = "puzzle.revive_limit_reached";
    }

    public readonly struct PuzzleLevelStartedEvent
    {
        public PuzzleLevelStartedEvent(int levelIndex, string levelId)
        {
            LevelIndex = levelIndex;
            LevelId = levelId;
        }

        public int LevelIndex { get; }
        public string LevelId { get; }
    }

    public readonly struct PuzzleLevelFinishedEvent
    {
        public PuzzleLevelFinishedEvent(LevelResult result, bool firstClear)
        {
            Result = result;
            FirstClear = firstClear;
        }

        public LevelResult Result { get; }

        /// <summary>True when this win advanced progression (and paid the first-clear reward).</summary>
        public bool FirstClear { get; }
    }
}
