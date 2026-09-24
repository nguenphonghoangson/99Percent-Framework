using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.PuzzleGameplay
{
    /// <summary>
    ///     Contract between the gameplay screen and whatever presents the end of a level (the Result feature's
    ///     popups). The screen opens a popup by key with these args and acts on the <see cref="LevelEndChoice" />
    ///     it closes with, so neither side references the other's views.
    /// </summary>
    public static class LevelEndKeys
    {
        public const string Win = "result_win";
        public const string Lose = "result_lose";
    }

    public enum LevelEndChoice
    {
        Home,
        Next,
        Retry,
        Revive,
        GiveUp
    }

    public sealed class WinArgs
    {
        public WinArgs(LevelResult result, bool firstClear, RewardBundle reward)
        {
            Result = result;
            FirstClear = firstClear;
            Reward = reward;
        }

        public LevelResult Result { get; }
        public bool FirstClear { get; }

        /// <summary>What was paid for this win; empty on a replay.</summary>
        public RewardBundle Reward { get; }
    }

    public sealed class LoseArgs
    {
        private LoseArgs(int levelNumber, bool outOfMoves, bool reviveAvailable, bool canAffordRevive, Cost reviveCost,
            int reviveMoves)
        {
            LevelNumber = levelNumber;
            OutOfMoves = outOfMoves;
            ReviveAvailable = reviveAvailable;
            CanAffordRevive = canAffordRevive;
            ReviveCost = reviveCost;
            ReviveMoves = reviveMoves;
        }

        public int LevelNumber { get; }

        /// <summary>True: the level can still be saved (offer Revive / Give up). False: final (Retry / Home).</summary>
        public bool OutOfMoves { get; }

        /// <summary>Revive limit not reached yet. May still be unaffordable — show it, disabled.</summary>
        public bool ReviveAvailable { get; }

        public bool CanAffordRevive { get; }
        public Cost ReviveCost { get; }
        public int ReviveMoves { get; }

        public static LoseArgs ReviveOffer(int levelNumber, bool reviveAvailable, bool canAfford, Cost cost, int moves) =>
            new(levelNumber, true, reviveAvailable, canAfford, cost, moves);

        public static LoseArgs Final(int levelNumber) => new(levelNumber, false, false, false, default, 0);
    }
}
