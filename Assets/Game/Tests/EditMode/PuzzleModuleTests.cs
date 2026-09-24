using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Moves;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Puzzle.Rules;
using NinetyNine.Modules.Puzzle.Turns;
using NUnit.Framework;

namespace NinetyNine.Tests
{
    public class PuzzleModuleTests
    {
        private static readonly TapClearRules Rules = new();

        private static PuzzleSession Session(int[] tiles, int width, int height, int moveLimit,
            params ObjectiveDefinition[] objectives)
        {
            var level = TestGame.Level("T", tiles, width, height, objectives);
            level.moveLimit = moveLimit;
            return new PuzzleSession(level, 0, Rules);
        }

        [Test]
        public void Tap_clears_the_connected_group_and_tiles_fall()
        {
            // Column 0 bottom→top: 1, 1, 2. Clearing the 1s drops the 2 to the bottom.
            var board = new Board(2, 3, new[] { 1, 3, 1, 3, 2, 3 });

            var outcome = Rules.Apply(board, PuzzleMove.Tap(new GridPos(0, 0)));

            Assert.That(outcome.ClearedCount(1), Is.EqualTo(2));
            Assert.That(board[0, 0], Is.EqualTo(2));
            Assert.That(board[0, 2], Is.EqualTo(Tile.Empty));
        }

        [Test]
        public void Blocked_cells_stop_gravity()
        {
            // Column bottom→top: 1, 1, wall, 2.
            var board = new Board(1, 4, new[] { 1, 1, Tile.Blocked, 2 });

            Rules.Apply(board, PuzzleMove.Tap(new GridPos(0, 0)));

            Assert.That(board[0, 3], Is.EqualTo(2));
            Assert.That(board[0, 2], Is.EqualTo(Tile.Blocked));
        }

        [Test]
        public void Lone_tile_has_no_effect_and_costs_no_move()
        {
            var session = Session(new[] { 1, 2, 2 }, 3, 1, 5, ObjectiveDefinition.ClearBoard());

            Assert.That(session.TryMove(PuzzleMove.Tap(new GridPos(0, 0))), Is.EqualTo(MoveValidation.NoEffect));
            Assert.That(session.Turn.MovesUsed, Is.Zero);
        }

        [Test]
        public void Completing_goals_on_the_last_move_is_a_win()
        {
            var session = Session(new[] { 1, 1, 2, 2 }, 4, 1, 1, ObjectiveDefinition.Collect(1, 2));

            session.TryMove(PuzzleMove.Tap(new GridPos(0, 0)));

            Assert.That(session.State, Is.EqualTo(PuzzleSessionState.Ended));
            Assert.That(session.Result?.Outcome, Is.EqualTo(LevelOutcome.Win));
        }

        [Test]
        public void Out_of_moves_waits_for_revive_then_can_still_win()
        {
            var session = Session(new[] { 1, 1, 2, 2 }, 4, 1, 1, ObjectiveDefinition.ClearBoard());

            session.TryMove(PuzzleMove.Tap(new GridPos(0, 0)));
            Assert.That(session.State, Is.EqualTo(PuzzleSessionState.OutOfMoves));
            Assert.That(session.Result, Is.Null);

            session.Revive(1);
            session.TryMove(PuzzleMove.Tap(new GridPos(2, 0)));

            Assert.That(session.Result?.Outcome, Is.EqualTo(LevelOutcome.Win));
            Assert.That(session.Result?.ReviveCount, Is.EqualTo(1));
        }

        [Test]
        public void Dead_board_is_a_lose_without_revive_offer()
        {
            var session = Session(new[] { 1, 1, 2, 3 }, 4, 1, 5, ObjectiveDefinition.ClearBoard());

            session.TryMove(PuzzleMove.Tap(new GridPos(0, 0)));

            Assert.That(session.Result?.Outcome, Is.EqualTo(LevelOutcome.Lose));
        }

        [Test]
        public void Unlimited_moves_never_run_out()
        {
            var turn = new TurnState(0);
            for (var i = 0; i < 1000; i++) turn.Consume();
            Assert.That(turn.HasMovesLeft, Is.True);
        }

        [TestCase(0, 5, 2, 0)]
        [TestCase(4, 5, 2, 4)]
        [TestCase(5, 5, 2, 2)]
        [TestCase(8, 5, 2, 2)]
        [TestCase(9, 5, 2, 3)]
        [TestCase(7, 5, 9, 2)]
        public void Level_index_loops_after_content_ends(int progress, int count, int loopStart, int expected) =>
            Assert.That(LevelIndexRules.ResolveContentIndex(progress, count, loopStart), Is.EqualTo(expected));

        [TestCase(0, 1)]
        [TestCase(800, 2)]
        [TestCase(5000, 3)]
        public void Stars_follow_thresholds_with_at_least_one(int score, int stars) =>
            Assert.That(ResultRules.Stars(score, new[] { 100, 800, 1500 }), Is.EqualTo(stars));
    }
}
