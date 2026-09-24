using System;
using System.Collections.Generic;

namespace NinetyNine.Modules.Puzzle.Boards
{
    public static class Tile
    {
        public const int Empty = 0;

        /// <summary>A wall: never moves, never matches, blocks gravity.</summary>
        public const int Blocked = -1;

        /// <summary>Tile ids above zero are playable (colours, pieces).</summary>
        public static bool IsPlayable(int tileId) => tileId > 0;
    }

    /// <summary>
    ///     Grid state and nothing else — no rules. Cells are stored row-major from the bottom row:
    ///     index = y * width + x.
    /// </summary>
    public sealed class Board
    {
        private readonly int[] _cells;

        public Board(int width, int height, int[] cells = null)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Board must be at least 1x1.");
            if (cells != null && cells.Length != width * height)
                throw new ArgumentException($"Expected {width * height} cells, got {cells.Length}.", nameof(cells));

            Width = width;
            Height = height;
            _cells = cells != null ? (int[])cells.Clone() : new int[width * height];
        }

        public int Width { get; }
        public int Height { get; }

        public int this[GridPos pos]
        {
            get => _cells[IndexOf(pos)];
            set => _cells[IndexOf(pos)] = value;
        }

        public int this[int x, int y]
        {
            get => this[new GridPos(x, y)];
            set => this[new GridPos(x, y)] = value;
        }

        public bool InBounds(GridPos pos) => pos.X >= 0 && pos.X < Width && pos.Y >= 0 && pos.Y < Height;

        public int Count(int tileId)
        {
            var count = 0;
            foreach (var cell in _cells)
                if (cell == tileId)
                    count++;
            return count;
        }

        public int CountPlayable()
        {
            var count = 0;
            foreach (var cell in _cells)
                if (Tile.IsPlayable(cell))
                    count++;
            return count;
        }

        public IEnumerable<GridPos> Positions()
        {
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                yield return new GridPos(x, y);
        }

        public Board Clone() => new(Width, Height, _cells);

        public int[] ToArray() => (int[])_cells.Clone();

        private int IndexOf(GridPos pos)
        {
            if (!InBounds(pos)) throw new ArgumentOutOfRangeException(nameof(pos), $"{pos} is outside {Width}x{Height}.");
            return pos.Y * Width + pos.X;
        }
    }
}
