using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.DailyReward.UI;
using NinetyNine.Features.Profile;
using NinetyNine.Features.Profile.UI;
using NinetyNine.Features.Shop;
using NinetyNine.Features.Shop.UI;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;

namespace NinetyNine.Tests
{
    public class PresenterTests
    {
        private sealed class FakeShopView : IShopView
        {
            public readonly List<bool> Busy = new();
            public List<ShopProductView> Products = new();
            public Result? LastResult;

            public void Render(IReadOnlyList<ShopProductView> products) => Products = products.ToList();
            public void SetBusy(bool busy) => Busy.Add(busy);
            public void ShowPurchaseResult(string productId, Result result) => LastResult = result;
        }

        private sealed class FakeDailyView : IDailyRewardView
        {
            public List<DayCell> Days = new();
            public bool CanClaim;
            public RewardBundle Claimed;
            public string Error;

            public void Render(IReadOnlyList<DayCell> days, bool canClaim, bool streakBroken)
            {
                Days = days.ToList();
                CanClaim = canClaim;
            }

            public void ShowClaimed(RewardBundle reward) => Claimed = reward;
            public void ShowError(string errorCode) => Error = errorCode;
        }

        private sealed class FakeProfileView : IProfileView
        {
            public string Error;
            public string Name;

            public void Render(string displayName, IReadOnlyList<ProfileOptionView> avatars,
                IReadOnlyList<ProfileOptionView> titles, Cost nextRenameCost) => Name = displayName;

            public void ShowError(string errorCode) => Error = errorCode;
        }

        private static TestGame ProfileGame()
        {
            var game = new TestGame();
            game.Profile.avatars.Add(new ProfileOption { id = "cat" });
            game.Profile.avatars.Add(new ProfileOption { id = "dog" });
            game.Profile.avatars.Add(new ProfileOption { id = "crown", unlock = UnlockCondition.AtLevel(5) });
            game.Profile.freeRenames = 0;
            game.Profile.renameCost = new Cost("gem", 50);
            return game.Build();
        }

        [Test]
        public void Profile_save_applies_name_and_avatar_together()
        {
            using var game = ProfileGame();
            var view = new FakeProfileView();
            using var presenter = new ProfilePresenter(game.Get<IProfileFeatureService>(), game.Get<IEventBus>(), view);

            Assert.That(presenter.Save("Tester", "dog"), Is.True);

            Assert.That(game.Get<IProfileService>().AvatarId, Is.EqualTo("dog"));
            Assert.That(view.Name, Is.EqualTo("Tester"));
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(50));
        }

        [Test]
        public void Profile_save_with_a_locked_avatar_charges_nothing_and_changes_nothing()
        {
            using var game = ProfileGame();
            var view = new FakeProfileView();
            using var presenter = new ProfilePresenter(game.Get<IProfileFeatureService>(), game.Get<IEventBus>(), view);

            Assert.That(presenter.Save("Tester", "crown"), Is.False);

            Assert.That(view.Error, Is.EqualTo(ProfileFeatureErrors.OptionLocked));
            Assert.That(game.Get<IProfileService>().DisplayName, Is.EqualTo("Player1234"));
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(100));
        }

        [Test]
        public void Profile_save_with_unchanged_values_is_free_and_succeeds()
        {
            using var game = ProfileGame();
            var view = new FakeProfileView();
            using var presenter = new ProfilePresenter(game.Get<IProfileFeatureService>(), game.Get<IEventBus>(), view);

            Assert.That(presenter.Save("  Player1234 ", "cat"), Is.True);
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(100));
        }

        private static TestGame DailyGame(UnlockCondition unlock = default)
        {
            var game = new TestGame();
            for (var i = 1; i <= 3; i++) game.DailyReward.days.Add(RewardBundle.Of(RewardItem.Currency("coin", i * 10)));
            game.DailyReward.unlock = unlock;
            return game.Build();
        }

        private static DayCellState[] States(FakeDailyView view) => view.Days.Select(d => d.State).ToArray();

        [Test]
        public void Daily_calendar_marks_today_claimable_then_claimed()
        {
            using var game = DailyGame();
            var view = new FakeDailyView();
            using var presenter = new DailyRewardPresenter(game.Get<IDailyRewardService>(), game.Get<IEventBus>(), view);

            Assert.That(States(view), Is.EqualTo(new[] { DayCellState.Claimable, DayCellState.Upcoming, DayCellState.Upcoming }));
            Assert.That(view.CanClaim, Is.True);

            presenter.Claim();

            Assert.That(view.Claimed.ToString(), Is.EqualTo("10 coin"));
            Assert.That(States(view), Is.EqualTo(new[] { DayCellState.Claimed, DayCellState.Upcoming, DayCellState.Upcoming }));
            Assert.That(view.CanClaim, Is.False);
        }

        [Test]
        public void Daily_calendar_next_day_shows_second_cell_claimable()
        {
            using var game = DailyGame();
            var view = new FakeDailyView();
            using var presenter = new DailyRewardPresenter(game.Get<IDailyRewardService>(), game.Get<IEventBus>(), view);
            presenter.Claim();

            game.Time.AdvanceDays(1);
            presenter.Refresh();

            Assert.That(States(view), Is.EqualTo(new[] { DayCellState.Claimed, DayCellState.Claimable, DayCellState.Upcoming }));
        }

        [Test]
        public void Locked_daily_shows_nothing_claimable_and_reports_why()
        {
            using var game = DailyGame(UnlockCondition.AtLevel(2));
            var view = new FakeDailyView();
            using var presenter = new DailyRewardPresenter(game.Get<IDailyRewardService>(), game.Get<IEventBus>(), view);

            Assert.That(States(view), Has.None.EqualTo(DayCellState.Claimable));
            presenter.Claim();
            Assert.That(view.Error, Is.EqualTo(CommonErrors.FeatureLocked));
        }

        [Test]
        public void Shop_rerenders_after_a_purchase_and_toggles_busy()
        {
            using var game = new TestGame();
            game.Shop.products.Add(new ShopProductDefinition
            {
                id = "hammer", priceKind = PriceKind.Currency, cost = new Cost("coin", 600), purchaseLimit = 1,
                rewards = RewardBundle.Of(RewardItem.Item("booster_hammer", 1))
            });
            game.Build();
            var view = new FakeShopView();
            using var presenter = new ShopPresenter(game.Get<IShopService>(), game.Get<IEventBus>(), view);
            Assert.That(view.Products[0].Availability, Is.EqualTo(ProductAvailability.Available));

            presenter.PurchaseAsync("hammer").Wait();

            Assert.That(view.LastResult?.IsSuccess, Is.True);
            Assert.That(view.Busy, Is.EqualTo(new[] { true, false }));
            Assert.That(view.Products[0].Availability, Is.EqualTo(ProductAvailability.SoldOut));
            Assert.That(view.Products[0].PriceLabel, Is.EqualTo("600 coin"));
        }

        [Test]
        public void Shop_card_turns_unaffordable_when_the_balance_drops()
        {
            using var game = new TestGame();
            game.Shop.products.Add(new ShopProductDefinition
            {
                id = "hammer", priceKind = PriceKind.Currency, cost = new Cost("coin", 600),
                rewards = RewardBundle.Of(RewardItem.Item("booster_hammer", 1))
            });
            game.Build();
            var view = new FakeShopView();
            using var presenter = new ShopPresenter(game.Get<IShopService>(), game.Get<IEventBus>(), view);

            game.Wallet.TrySpend(new Cost("coin", 500), "test");

            Assert.That(view.Products[0].Availability, Is.EqualTo(ProductAvailability.CannotAfford));
        }
    }
}
