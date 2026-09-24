using System;
using System.Collections.Generic;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Moves;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Puzzle.Rules;
using NinetyNine.Modules.Puzzle.Turns;

namespace NinetyNine.Modules.Puzzle
{
    public enum PuzzleSessionState
    {
        Playing,

        /// <summary>Waiting for the player to revive or give up. Nothing is final yet.</summary>
        OutOfMoves,
        Ended
    }

    /// <summary>
    ///     One play-through of one level: wires Board + Rule + Turn + Objective and produces a Result. It does
    ///     not grant rewards or advance progression — that is the gameplay feature's job, so the same session
    ///     runs a main level, a challenge or a tutorial.
    /// </summary>
    public sealed class PuzzleSession
    {
        private readonly List<IObjective> _objectives = new();
        private readonly IPuzzleRules _rules;

        public PuzzleSession(LevelDefinition level, int levelIndex, IPuzzleRules rules)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            var error = level.Validate();
            if (error != null) throw new ArgumentException(error, nameof(level));

            Level = level;
            LevelIndex = levelIndex;
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Board = level.CreateBoard();
            Turn = new TurnState(level.moveLimit);

            foreach (var definition in level.objectives)
            {
                var objective = ObjectiveFactory.Create(definition);
                objective.Begin(Board);
                _objectives.Add(objective);
            }
        }

        public LevelDefinition Level { get; }
        public int LevelIndex { get; }
        public Board Board { get; }
        public TurnState Turn { get; }
        public IReadOnlyList<IObjective> Objectives => _objectives;
        public int Score { get; private set; }
        public int ReviveCount { get; private set; }
        public PuzzleSessionState State { get; private set; } = PuzzleSessionState.Playing;

        /// <summary>Set once <see cref="State" /> is <see cref="PuzzleSessionState.Ended" />.</summary>
        public LevelResult? Result { get; private set; }

        public event Action<PuzzleMove, MoveOutcome> MoveResolved;
        public event Action OutOfMoves;
        public event Action<LevelResult> Ended;

        public MoveValidation TryMove(PuzzleMove move)
        {
            if (State != PuzzleSessionState.Playing) return MoveValidation.SessionNotPlaying;

            var validation = _rules.Validate(Board, move);
            if (validation != MoveValidation.Valid) return validation;

            var outcome = _rules.Apply(Board, move);
            Turn.Consume();
            Score += outcome.ScoreGained;
            foreach (var objective in _objectives) objective.OnMoveResolved(Board, outcome);

            MoveResolved?.Invoke(move, outcome);
            Evaluate();
            return MoveValidation.Valid;
        }

        public void Revive(int extraMoves)
        {
            if (State != PuzzleSessionState.OutOfMoves) throw new InvalidOperationException("Can only revive when out of moves.");

            Turn.AddMoves(extraMoves);
            ReviveCount++;
            State = PuzzleSessionState.Playing;
        }

        /// <summary>Declines the revive offer.</summary>
        public void GiveUp()
        {
            if (State != PuzzleSessionState.OutOfMoves) throw new InvalidOperationException("Can only give up when out of moves.");
            End(LevelOutcome.Lose);
        }

        public void Quit()
        {
            if (State != PuzzleSessionState.Ended) End(LevelOutcome.Quit);
        }

        private void Evaluate()
        {
            switch (ResultRules.Check(_objectives, Turn, _rules.HasAnyValidMove(Board)))
            {
                case ResultCheck.Win:
                    End(LevelOutcome.Win);
                    break;
                case ResultCheck.NoValidMoves:
                    End(LevelOutcome.Lose);
                    break;
                case ResultCheck.OutOfMoves:
                    State = PuzzleSessionState.OutOfMoves;
                    OutOfMoves?.Invoke();
                    break;
            }
        }

        private void End(LevelOutcome outcome)
        {
            State = PuzzleSessionState.Ended;
            var stars = outcome == LevelOutcome.Win ? ResultRules.Stars(Score, Level.starScores) : 0;
            var result = new LevelResult(Level.id, LevelIndex, outcome, Score, stars, Turn.MovesUsed, ReviveCount);
            Result = result;
            Ended?.Invoke(result);
        }
    }
}
