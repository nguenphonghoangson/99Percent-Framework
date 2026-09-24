using NUnit.Framework;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.DailyReward.Domain;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Tests
{
    public class DailyRewardFeatureTests
    {
        private static TestGame Game(bool resetOnMiss = true)
        {
            var game = new TestGame();
            for (var day = 1; day <= 3; day++)
                game.DailyReward.days.Add(RewardBundle.Of(RewardItem.Currency("coin", day * 10)));
            game.DailyReward.resetStreakOnMiss = resetOnMiss;
            return game.Build();
        }

        [Test]
        public void Claims_once_per_utc_day()
        {
            using var game = Game();
            var daily = game.Get<IDailyRewardService>();

            Assert.That(daily.TryClaim().IsSuccess, Is.True);
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1010));
            Assert.That(daily.TryClaim().Error, Is.EqualTo(DailyRewardErrors.AlreadyClaimed));
            Assert.That(daily.ClaimNumber, Is.EqualTo(1));
        }

        [Test]
        public void Consecutive_days_build_the_streak_and_the_cycle_wraps()
        {
            using var game = Game();
            var daily = game.Get<IDailyRewardService>();

            for (var i = 0; i < 4; i++)
            {
                daily.TryClaim();
                game.Time.AdvanceDays(1);
            }

            Assert.That(daily.Streak, Is.EqualTo(4));
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1000 + 10 + 20 + 30 + 10));
            Assert.That(daily.ClaimDayIndex, Is.EqualTo(1));
        }

        [Test]
        public void Missing_a_day_resets_when_configured()
        {
            using var game = Game();
            var daily = game.Get<IDailyRewardService>();
            daily.TryClaim();
            game.Time.AdvanceDays(1);
            daily.TryClaim();

            game.Time.AdvanceDays(3);

            Assert.That(daily.IsStreakBroken, Is.True);
            Assert.That(daily.ClaimNumber, Is.EqualTo(1));
        }

        [Test]
        public void Missing_a_day_continues_when_not_resetting()
        {
            using var game = Game(resetOnMiss: false);
            var daily = game.Get<IDailyRewardService>();
            daily.TryClaim();

            game.Time.AdvanceDays(5);

            Assert.That(daily.ClaimNumber, Is.EqualTo(2));
        }

        [Test]
        public void Clock_rollback_blocks_the_claim()
        {
            using var game = Game();
            var daily = game.Get<IDailyRewardService>();
            daily.TryClaim();

            game.Time.AdvanceDays(-2);

            Assert.That(daily.Availability, Is.EqualTo(ClaimDenial.ClockBehind));
            Assert.That(daily.TryClaim().Error, Is.EqualTo(DailyRewardErrors.ClockBehind));
        }

        [Test]
        public void Streak_survives_a_relaunch()
        {
            var save = new NinetyNine.Persistence.InMemorySaveProvider();
            using (var first = new TestGame(save))
            {
                first.DailyReward.days.Add(RewardBundle.Of(RewardItem.Currency("coin", 10)));
                first.Build().Get<IDailyRewardService>().TryClaim();
            }

            using var relaunch = new TestGame(save);
            relaunch.DailyReward.days.Add(RewardBundle.Of(RewardItem.Currency("coin", 10)));
            relaunch.Build();

            Assert.That(relaunch.Get<IDailyRewardService>().Availability, Is.EqualTo(ClaimDenial.AlreadyClaimedToday));
        }

        [Test]
        public void Locked_until_unlock_level()
        {
            using var game = new TestGame();
            game.DailyReward.days.Add(RewardBundle.Of(RewardItem.Currency("coin", 10)));
            game.DailyReward.unlock = UnlockCondition.AtLevel(2);
            game.Build();

            Assert.That(game.Get<IDailyRewardService>().CanClaim, Is.False);
        }
    }
}
