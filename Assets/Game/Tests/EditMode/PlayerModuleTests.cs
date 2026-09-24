using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Reward;
using NUnit.Framework;

namespace NinetyNine.Tests
{
    public class PlayerModuleTests
    {
        [Test]
        public void Initial_balance_is_granted_once_not_on_every_launch()
        {
            using var first = new TestGame().Build();
            Assert.That(first.Wallet.GetBalance("coin"), Is.EqualTo(1000));
            first.Wallet.TrySpend(new Cost("coin", 1000), "test");

            using var relaunch = new TestGame(first.Save).Build();

            Assert.That(relaunch.Wallet.GetBalance("coin"), Is.EqualTo(0));
        }

        [Test]
        public void Overspend_is_refused_and_changes_nothing()
        {
            using var game = new TestGame().Build();

            var result = game.Wallet.TrySpend(new Cost("gem", 101), "test");

            Assert.That(result.Error, Is.EqualTo(EconomyErrors.InsufficientFunds));
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(100));
        }

        [Test]
        public void Add_clamps_at_max_balance()
        {
            using var game = new TestGame();
            game.Economy.currencies[1].maxBalance = 150;
            game.Build();

            game.Wallet.Add("gem", 500, "test");

            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(150));
        }

        [Test]
        public void Bundle_with_one_bad_entry_grants_nothing()
        {
            using var game = new TestGame().Build();
            var bundle = RewardBundle.Of(RewardItem.Currency("coin", 10), RewardItem.Currency("ruby", 5));

            var result = game.Get<IRewardService>().Grant(bundle, "test");

            Assert.That(result.Error, Is.EqualTo(RewardErrors.UnknownCurrency));
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1000));
        }

        [Test]
        public void Reward_routes_currency_to_wallet_and_items_to_inventory()
        {
            using var game = new TestGame().Build();

            game.Get<IRewardService>().Grant(
                RewardBundle.Of(RewardItem.Currency("coin", 10), RewardItem.Item("booster_hammer", 2)), "test");

            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1010));
            Assert.That(game.Inventory.GetQuantity("booster_hammer"), Is.EqualTo(2));
        }

        [TestCase("  Sơn   Nguyễn ", "Sơn Nguyễn")]
        [TestCase("a\tb", "a b")]
        public void Names_are_normalised(string raw, string expected) =>
            Assert.That(ProfileRules.NormalizeName(raw), Is.EqualTo(expected));

        [TestCase("ab", ProfileErrors.NameTooShort)]
        [TestCase("abcdefghijklmnopq", ProfileErrors.NameTooLong)]
        [TestCase("bad<name>", ProfileErrors.NameInvalidCharacters)]
        public void Invalid_names_are_rejected(string name, string error) =>
            Assert.That(ProfileRules.ValidateName(name).Error, Is.EqualTo(error));

        [Test]
        public void Replaying_an_old_level_does_not_advance_progression()
        {
            using var game = new TestGame().Build();
            game.Progression.CompleteLevel(0);

            Assert.That(game.Progression.CompleteLevel(0), Is.False);
            Assert.That(game.Progression.CurrentLevelNumber, Is.EqualTo(2));
        }
    }
}
