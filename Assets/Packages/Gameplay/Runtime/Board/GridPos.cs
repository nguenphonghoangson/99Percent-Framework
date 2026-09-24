using System;

namespace NinetyNine.Modules.Puzzle.Boards
{
    /// <summary>Cell coordinate. y = 0 is the bottom row, so gravity moves tiles towards lower y.</summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public static readonly GridPos[] Neighbours4 = { new(0, 1), new(1, 0), new(0, -1), new(-1, 0) };

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public static GridPos operator +(GridPos a, GridPos b) => new(a.X + b.X, a.Y + b.Y);

        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);

        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        public override int GetHashCode() => (X * 397) ^ Y;

        public override string ToString() => $"({X},{Y})";
    }
}
