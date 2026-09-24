using System.Collections.Generic;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Moves;

namespace NinetyNine.Modules.Puzzle.Rules
{
    public enum MoveValidation
    {
        Valid,
        OutOfBounds,
        NotPlayable,
        NotSupported,

        /// <summary>Legal input that would change nothing (e.g. tapping a lone tile). Costs no move.</summary>
        NoEffect,
        SessionNotPlaying
    }

    /// <summary>What one resolved move did to the board. Objectives and scoring read this, not the board diff.</summary>
    public sealed class MoveOutcome
    {
        public MoveOutcome(IReadOnlyList<GridPos> cleared, IReadOnlyDictionary<int, int> clearedByTile, int scoreGained)
        {
            Cleared = cleared;
            ClearedByTile = clearedByTile;
            ScoreGained = scoreGained;
        }

        public IReadOnlyList<GridPos> Cleared { get; }

        /// <summary>tile id → number of that tile cleared by this move.</summary>
        public IReadOnlyDictionary<int, int> ClearedByTile { get; }

        public int ScoreGained { get; }

        public int ClearedCount(int tileId) => ClearedByTile.TryGetValue(tileId, out var count) ? count : 0;
    }

    /// <summary>
    ///     The game's mechanic. Swap this to change the puzzle; board, turns, objectives and results stay.
    ///     Implementations must be stateless — one instance serves every session.
    /// </summary>
    public interface IPuzzleRules
    {
        MoveValidation Validate(Board board, PuzzleMove move);

        /// <summary>Mutates <paramref name="board" />. Only called after <see cref="Validate" /> returned Valid.</summary>
        MoveOutcome Apply(Board board, PuzzleMove move);

        /// <summary>False means the board is dead: extra moves cannot help, so there is nothing to revive.</summary>
        bool HasAnyValidMove(Board board);
    }
}
