using System.Collections.Generic;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Puzzle.Turns;

namespace NinetyNine.Modules.Puzzle.Results
{
    public enum ResultCheck
    {
        Continue,
        Win,

        /// <summary>Budget spent but the board still has moves: a revive can save this.</summary>
        OutOfMoves,

        /// <summary>No legal move left: a lose no revive can fix.</summary>
        NoValidMoves
    }

    public static class ResultRules
    {
        /// <summary>
        ///     Win is checked first: a last move that both completes the goals and spends the final move is a
        ///     win, never an out-of-moves.
        /// </summary>
        public static ResultCheck Check(IReadOnlyList<IObjective> objectives, TurnState turn, bool hasValidMove)
        {
            var allComplete = objectives.Count > 0;
            foreach (var objective in objectives)
                if (!objective.IsComplete)
                {
                    allComplete = false;
                    break;
                }

            if (allComplete) return ResultCheck.Win;
            if (!hasValidMove) return ResultCheck.NoValidMoves;
            if (!turn.HasMovesLeft) return ResultCheck.OutOfMoves;
            return ResultCheck.Continue;
        }

        /// <summary>
        ///     Stars for a win: one per threshold reached, at least one. <paramref name="thresholds" /> is
        ///     ascending score per star (e.g. [0, 800, 1500]).
        /// </summary>
        public static int Stars(int score, IReadOnlyList<int> thresholds)
        {
            if (thresholds == null || thresholds.Count == 0) return 1;

            var stars = 0;
            foreach (var threshold in thresholds)
                if (score >= threshold)
                    stars++;
            return stars < 1 ? 1 : stars;
        }
    }
}
