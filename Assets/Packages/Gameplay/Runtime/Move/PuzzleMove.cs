using NinetyNine.Modules.Puzzle.Boards;

namespace NinetyNine.Modules.Puzzle.Moves
{
    public enum MoveKind
    {
        Tap,
        Swap
    }

    /// <summary>
    ///     A player action as data. Input code builds these; the rule set decides what they do. Being plain
    ///     data, a move list is also a replay and an analytics payload.
    /// </summary>
    public readonly struct PuzzleMove
    {
        private PuzzleMove(MoveKind kind, GridPos from, GridPos to)
        {
            Kind = kind;
            From = from;
            To = to;
        }

        public MoveKind Kind { get; }
        public GridPos From { get; }

        /// <summary>Second cell for two-cell moves; equals <see cref="From" /> for a tap.</summary>
        public GridPos To { get; }

        public static PuzzleMove Tap(GridPos pos) => new(MoveKind.Tap, pos, pos);

        public static PuzzleMove Swap(GridPos a, GridPos b) => new(MoveKind.Swap, a, b);

        public override string ToString() => Kind == MoveKind.Tap ? $"Tap{From}" : $"Swap{From}->{To}";
    }
}
