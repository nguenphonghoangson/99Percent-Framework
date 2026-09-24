using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Moves;
using NUnit.Framework;

namespace NinetyNine.Tests
{
    public class PuzzleGameplayFeatureTests
    {
        private static void ClearLevel(PuzzleSession session)
        {
            session.TryMove(PuzzleMove.Tap(new GridPos(0, 0)));
            session.TryMove(PuzzleMove.Tap(new GridPos(2, 0)));
        }

        [Test]
        public void First_clear_advances_and_pays_replay_pays_nothing()
        {
            using var game = new TestGame().Build();
            var gameplay = game.Get<IPuzzleGameplayService>();

            ClearLevel(gameplay.StartCurrentLevel());
            Assert.That(game.Progression.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1050));

            ClearLevel(gameplay.StartLevel(0));
            Assert.That(game.Progression.CurrentLevelIndex, Is.EqualTo(1));
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1050));
        }

        [Test]
        public void Revive_charges_once_and_respects_the_limit()
        {
            using var game = new TestGame();
            game.Levels.levels[0].moveLimit = 1;
            game.Build();
            var gameplay = game.Get<IPuzzleGameplayService>();
            var session = gameplay.StartCurrentLevel();

            session.TryMove(PuzzleMove.Tap(new GridPos(0, 0)));
            Assert.That(gameplay.CanRevive, Is.True);
            Assert.That(gameplay.TryRevive().IsSuccess, Is.True);
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(900));
            Assert.That(session.State, Is.EqualTo(PuzzleSessionState.Playing));
        }

        [Test]
        public void Revive_without_funds_is_refused()
        {
            using var game = new TestGame();
            game.Levels.levels[0].moveLimit = 1;
            game.Build();
            game.Wallet.TrySpend(new Cost("coin", 1000), "test");
            var gameplay = game.Get<IPuzzleGameplayService>();

            gameplay.StartCurrentLevel().TryMove(PuzzleMove.Tap(new GridPos(0, 0)));

            Assert.That(gameplay.TryRevive().Error, Is.EqualTo(EconomyErrors.InsufficientFunds));
        }

        [Test]
        public void Starting_a_new_level_quits_the_running_one()
        {
            using var game = new TestGame().Build();
            var gameplay = game.Get<IPuzzleGameplayService>();
            var first = gameplay.StartCurrentLevel();

            gameplay.StartCurrentLevel();

            Assert.That(first.State, Is.EqualTo(PuzzleSessionState.Ended));
        }
    }
}
