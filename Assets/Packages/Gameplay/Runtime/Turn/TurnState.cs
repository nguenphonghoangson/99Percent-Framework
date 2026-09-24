using System;

namespace NinetyNine.Modules.Puzzle.Turns
{
    /// <summary>Move budget of a session. A limit of 0 means unlimited.</summary>
    public sealed class TurnState
    {
        public TurnState(int moveLimit) => MoveLimit = Math.Max(0, moveLimit);

        public int MoveLimit { get; }

        public int MovesUsed { get; private set; }

        /// <summary>Moves added after the start — revives, +moves boosters.</summary>
        public int BonusMoves { get; private set; }

        public bool IsUnlimited => MoveLimit == 0;

        public int MovesLeft => IsUnlimited ? int.MaxValue : Math.Max(0, MoveLimit + BonusMoves - MovesUsed);

        public bool HasMovesLeft => MovesLeft > 0;

        public void Consume()
        {
            if (!HasMovesLeft) throw new InvalidOperationException("No moves left.");
            MovesUsed++;
        }

        public void AddMoves(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            BonusMoves += count;
        }
    }
}
