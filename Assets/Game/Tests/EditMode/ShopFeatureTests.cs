using NUnit.Framework;
using NinetyNine.Core;
using NinetyNine.Features.Shop;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Iap;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;

namespace NinetyNine.Tests
{
    public class ShopFeatureTests
    {
        private static ShopProductDefinition Hammer(int limit = 0) => new()
        {
            id = "hammer_x3", priceKind = PriceKind.Currency, cost = new Cost("coin", 300), purchaseLimit = limit,
            rewards = RewardBundle.Of(RewardItem.Item("booster_hammer", 3))
        };

        private static ShopProductDefinition GemPack() => new()
        {
            id = "gems_small", priceKind = PriceKind.Iap, iapProductId = "com.percas.gems_small",
            rewards = RewardBundle.Of(RewardItem.Currency("gem", 500))
        };

        private static Result Buy(TestGame game, string id) => game.Get<IShopService>().PurchaseAsync(id).Result;

        [Test]
        public void Currency_purchase_spends_grants_and_counts()
        {
            using var game = new TestGame();
            game.Shop.products.Add(Hammer());
            game.Build();

            Assert.That(Buy(game, "hammer_x3").IsSuccess, Is.True);
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(700));
            Assert.That(game.Inventory.GetQuantity("booster_hammer"), Is.EqualTo(3));
            Assert.That(game.Get<IShopService>().GetPurchaseCount("hammer_x3"), Is.EqualTo(1));
        }

        [Test]
        public void Purchase_limit_sells_out()
        {
            using var game = new TestGame();
            game.Shop.products.Add(Hammer(limit: 1));
            game.Build();

            Buy(game, "hammer_x3");

            Assert.That(Buy(game, "hammer_x3").Error, Is.EqualTo(ShopErrors.SoldOut));
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(700));
        }

        [Test]
        public void Misconfigured_bundle_fails_without_charging()
        {
            using var game = new TestGame();
            var product = Hammer();
            product.rewards = RewardBundle.Of(RewardItem.Currency("ruby", 1));
            game.Shop.products.Add(product);
            game.Build();

            Assert.That(Buy(game, "hammer_x3").Error, Is.EqualTo(RewardErrors.UnknownCurrency));
            Assert.That(game.Wallet.GetBalance("coin"), Is.EqualTo(1000));
        }

        [Test]
        public void Shop_is_locked_until_its_level()
        {
            using var game = new TestGame();
            game.Shop.featureUnlock = UnlockCondition.AtLevel(2);
            game.Shop.products.Add(Hammer());
            game.Build();

            Assert.That(Buy(game, "hammer_x3").Error, Is.EqualTo(CommonErrors.FeatureLocked));
            game.Progression.CompleteLevel(0);
            Assert.That(Buy(game, "hammer_x3").IsSuccess, Is.True);
        }

        [Test]
        public void Iap_purchase_grants_once()
        {
            using var game = new TestGame();
            game.Shop.products.Add(GemPack());
            game.Build();

            Assert.That(Buy(game, "gems_small").IsSuccess, Is.True);
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(600));
        }

        [Test]
        public void Replayed_transaction_is_not_paid_twice()
        {
            using var game = new TestGame();
            game.Shop.products.Add(GemPack());
            game.Build();

            game.Iap.Deliver("com.percas.gems_small", "tx-1");
            game.Iap.Deliver("com.percas.gems_small", "tx-1");

            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(600));
        }

        [Test]
        public void Cancelled_iap_grants_nothing()
        {
            using var game = new TestGame();
            game.Shop.products.Add(GemPack());
            game.Build();
            game.Iap.NextStatus = IapPurchaseStatus.Cancelled;

            Assert.That(Buy(game, "gems_small").Error, Is.EqualTo(ShopErrors.PurchaseCancelled));
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(100));
        }

        [Test]
        public void Without_iap_module_store_products_are_unavailable()
        {
            using var game = new TestGame();
            game.Shop.products.Add(GemPack());
            game.Build(withIap: false);

            Assert.That(game.Get<IShopService>().GetAvailability("gems_small"), Is.EqualTo(ProductAvailability.Unavailable));
        }

        [Test]
        public void Disabled_shop_is_not_installed()
        {
            using var game = new TestGame();
            game.Toggles.Set(ShopFeature.Id, false);
            game.Build();

            Assert.That(game.Services.Has<IShopService>(), Is.False);
        }
    }
}
