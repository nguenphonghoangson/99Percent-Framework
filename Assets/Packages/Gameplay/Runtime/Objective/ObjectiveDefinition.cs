using System;

namespace NinetyNine.Modules.Puzzle.Objectives
{
    public enum ObjectiveKind
    {
        CollectTile,
        ReachScore,
        ClearBoard
    }

    /// <summary>Design data for one level goal, authored inside a level definition.</summary>
    [Serializable]
    public sealed class ObjectiveDefinition
    {
        public ObjectiveKind kind;

        /// <summary>Used by <see cref="ObjectiveKind.CollectTile" /> only.</summary>
        public int tileId;

        /// <summary>Ignored by <see cref="ObjectiveKind.ClearBoard" />, whose target is the board's tile count.</summary>
        public int target;

        public static ObjectiveDefinition Collect(int tileId, int count) =>
            new() { kind = ObjectiveKind.CollectTile, tileId = tileId, target = count };

        public static ObjectiveDefinition Score(int score) => new() { kind = ObjectiveKind.ReachScore, target = score };

        public static ObjectiveDefinition ClearBoard() => new() { kind = ObjectiveKind.ClearBoard };
    }
}
