using System.Collections.Generic;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Moves;

namespace NinetyNine.Modules.Puzzle.Rules
{
    /// <summary>
    ///     Reference mechanic: tap a group of orthogonally connected same tiles to clear it, then tiles fall.
    ///     Score is size² × <c>pointsPerTile</c>, rewarding bigger groups.
    /// </summary>
    public sealed class TapClearRules : IPuzzleRules
    {
        private readonly bool _gravity;
        private readonly int _minGroupSize;
        private readonly int _pointsPerTile;

        public TapClearRules(int minGroupSize = 2, int pointsPerTile = 10, bool gravity = true)
        {
            _minGroupSize = minGroupSize < 1 ? 1 : minGroupSize;
            _pointsPerTile = pointsPerTile;
            _gravity = gravity;
        }

        public MoveValidation Validate(Board board, PuzzleMove move)
        {
            if (move.Kind != MoveKind.Tap) return MoveValidation.NotSupported;
            if (!board.InBounds(move.From)) return MoveValidation.OutOfBounds;
            if (!Tile.IsPlayable(board[move.From])) return MoveValidation.NotPlayable;

            return FloodGroup(board, move.From, null).Count >= _minGroupSize ? MoveValidation.Valid : MoveValidation.NoEffect;
        }

        public MoveOutcome Apply(Board board, PuzzleMove move)
        {
            var tileId = board[move.From];
            var group = FloodGroup(board, move.From, null);
            foreach (var pos in group) board[pos] = Tile.Empty;

            if (_gravity) ApplyGravity(board);

            var byTile = new Dictionary<int, int> { [tileId] = group.Count };
            return new MoveOutcome(group, byTile, group.Count * group.Count * _pointsPerTile);
        }

        public bool HasAnyValidMove(Board board)
        {
            var visited = new HashSet<GridPos>();
            foreach (var pos in board.Positions())
            {
                if (visited.Contains(pos) || !Tile.IsPlayable(board[pos])) continue;
                if (FloodGroup(board, pos, visited).Count >= _minGroupSize) return true;
            }

            return false;
        }

        private static List<GridPos> FloodGroup(Board board, GridPos start, HashSet<GridPos> visited)
        {
            var tileId = board[start];
            var group = new List<GridPos>();
            var seen = visited ?? new HashSet<GridPos>();
            var stack = new Stack<GridPos>();
            stack.Push(start);
            seen.Add(start);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                group.Add(current);
                foreach (var offset in GridPos.Neighbours4)
                {
                    var next = current + offset;
                    if (!board.InBounds(next) || board[next] != tileId || !seen.Add(next)) continue;
                    stack.Push(next);
                }
            }

            return group;
        }

        // Blocked cells split a column into segments; tiles compact to the bottom of their own segment.
        private static void ApplyGravity(Board board)
        {
            for (var x = 0; x < board.Width; x++)
            {
                var write = 0;
                for (var y = 0; y < board.Height; y++)
                {
                    var tile = board[x, y];
                    if (tile == Tile.Blocked)
                    {
                        write = y + 1;
                        continue;
                    }

                    if (tile == Tile.Empty) continue;

                    if (write != y)
                    {
                        board[x, write] = tile;
                        board[x, y] = Tile.Empty;
                    }

                    write++;
                }
            }
        }
    }
}
