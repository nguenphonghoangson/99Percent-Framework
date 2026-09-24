using System;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Rules;

namespace NinetyNine.Modules.Puzzle.Objectives
{
    /// <summary>A level goal's runtime progress. Fed every resolved move; never reads input.</summary>
    public interface IObjective
    {
        ObjectiveDefinition Definition { get; }

        int Current { get; }

        int Target { get; }

        bool IsComplete { get; }

        void Begin(Board board);

        void OnMoveResolved(Board board, MoveOutcome outcome);
    }

    public abstract class ObjectiveBase : IObjective
    {
        private int _current;

        protected ObjectiveBase(ObjectiveDefinition definition) => Definition = definition;

        public ObjectiveDefinition Definition { get; }

        public int Current
        {
            get => _current;
            protected set => _current = Math.Min(value, Target);
        }

        public int Target { get; protected set; }

        public bool IsComplete => Current >= Target;

        public virtual void Begin(Board board) => Target = Math.Max(1, Definition.target);

        public abstract void OnMoveResolved(Board board, MoveOutcome outcome);
    }

    public sealed class CollectTileObjective : ObjectiveBase
    {
        public CollectTileObjective(ObjectiveDefinition definition) : base(definition) { }

        public override void OnMoveResolved(Board board, MoveOutcome outcome) =>
            Current += outcome.ClearedCount(Definition.tileId);
    }

    public sealed class ReachScoreObjective : ObjectiveBase
    {
        public ReachScoreObjective(ObjectiveDefinition definition) : base(definition) { }

        public override void OnMoveResolved(Board board, MoveOutcome outcome) => Current += outcome.ScoreGained;
    }

    public sealed class ClearBoardObjective : ObjectiveBase
    {
        public ClearBoardObjective(ObjectiveDefinition definition) : base(definition) { }

        public override void Begin(Board board) => Target = Math.Max(1, board.CountPlayable());

        public override void OnMoveResolved(Board board, MoveOutcome outcome) =>
            Current = Target - board.CountPlayable();
    }

    public static class ObjectiveFactory
    {
        public static IObjective Create(ObjectiveDefinition definition) => definition.kind switch
        {
            ObjectiveKind.CollectTile => new CollectTileObjective(definition),
            ObjectiveKind.ReachScore => new ReachScoreObjective(definition),
            ObjectiveKind.ClearBoard => new ClearBoardObjective(definition),
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition.kind, "Unknown objective kind.")
        };
    }
}
