using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Iap;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Features.Shop
{
    [Serializable]
    internal sealed class ShopState
    {
        public CounterTable purchases = new();
    }

    internal sealed class ShopService : IShopService, IInitializable, IDisposable
    {
        private const string SaveKey = "feature.shop";

        private readonly ShopConfig _config;
        private readonly IEconomyService _economy;
        private readonly IEventBus _events;
        private readonly IIapService _iap;
        private readonly IRewardService _rewards;
        private readonly ISaveService _save;
        private readonly UnlockEvaluator _unlocks;
        private IDisposable _iapSubscription;
        private ShopState _state = new();

        /// <param name="iap">Null on a build without a store: IAP products report Unavailable.</param>
        public ShopService(ShopConfig config, IEconomyService economy, IRewardService rewards, IIapService iap,
            UnlockEvaluator unlocks, ISaveService save, IEventBus events)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _economy = economy;
            _rewards = rewards;
            _iap = iap;
            _unlocks = unlocks;
            _save = save;
            _events = events;
        }

        public void Initialize()
        {
            _state = _save.Load<ShopState>(SaveKey);
            _iapSubscription = _events.Subscribe<IapPurchaseCompletedEvent>(OnIapPurchaseCompleted);
        }

        public void Dispose() => _iapSubscription?.Dispose();

        public bool IsUnlocked => _unlocks.IsMet(_config.featureUnlock);

        public IReadOnlyList<ShopProductDefinition> Products => _config.products;

        public int GetPurchaseCount(string productId) => (int)_state.purchases.Get(productId);

        public ProductAvailability GetAvailability(string productId)
        {
            var product = _config.Find(productId);
            if (product == null) return ProductAvailability.Unavailable;
            if (product.priceKind == PriceKind.Iap && _iap == null) return ProductAvailability.Unavailable;
            if (!_unlocks.IsMet(product.unlock)) return ProductAvailability.Locked;
            if (product.purchaseLimit > 0 && GetPurchaseCount(productId) >= product.purchaseLimit)
                return ProductAvailability.SoldOut;
            if (product.priceKind == PriceKind.Currency && !_economy.CanAfford(product.cost))
                return ProductAvailability.CannotAfford;
            return ProductAvailability.Available;
        }

        public string GetPriceLabel(string productId)
        {
            var product = _config.Find(productId);
            if (product == null) return string.Empty;

            return product.priceKind switch
            {
                PriceKind.Currency => product.cost.ToString(),
                PriceKind.Iap => _iap?.GetLocalizedPrice(product.iapProductId) ?? string.Empty,
                _ => "Free"
            };
        }

        public async Task<Result> PurchaseAsync(string productId)
        {
            if (!IsUnlocked) return Result.Fail(CommonErrors.FeatureLocked);

            var product = _config.Find(productId);
            if (product == null) return Result.Fail(ShopErrors.UnknownProduct);

            switch (GetAvailability(productId))
            {
                case ProductAvailability.Locked: return Result.Fail(ShopErrors.ProductLocked);
                case ProductAvailability.SoldOut: return Result.Fail(ShopErrors.SoldOut);
                case ProductAvailability.Unavailable: return Result.Fail(ShopErrors.IapUnavailable);
                case ProductAvailability.CannotAfford: return Result.Fail(EconomyErrors.InsufficientFunds);
            }

            // Checked before charging: a misconfigured bundle must fail without taking the player's money.
            var validation = _rewards.Validate(product.rewards);
            if (!validation.IsSuccess) return validation;

            if (product.priceKind == PriceKind.Iap) return await PurchaseIapAsync(product);

            var spend = _economy.TrySpend(product.cost, Source(product));
            if (!spend.IsSuccess) return spend;

            Deliver(product);
            return Result.Success;
        }

        private async Task<Result> PurchaseIapAsync(ShopProductDefinition product)
        {
            // Delivery happens in OnIapPurchaseCompleted, which IIapService raises before this await returns.
            var result = await _iap.PurchaseAsync(product.iapProductId);
            return result.Status switch
            {
                IapPurchaseStatus.Success => Result.Success,
                IapPurchaseStatus.Cancelled => Result.Fail(ShopErrors.PurchaseCancelled),
                IapPurchaseStatus.Pending => Result.Fail(ShopErrors.PurchasePending),
                IapPurchaseStatus.AlreadyInProgress => Result.Fail(ShopErrors.PurchaseInProgress),
                _ => Result.Fail(ShopErrors.PurchaseFailed)
            };
        }

        private void OnIapPurchaseCompleted(IapPurchaseCompletedEvent evt)
        {
            // Products that are not in the shop (a subscription, a no-ads flag) belong to another listener.
            // Limits are not re-checked: the player has paid, so the goods are owed.
            var product = _config.FindByIapProduct(evt.ProductId);
            if (product != null) Deliver(product);
        }

        private void Deliver(ShopProductDefinition product)
        {
            _state.purchases.Set(product.id, _state.purchases.Get(product.id) + 1);
            _save.Save(SaveKey, _state);
            _rewards.Grant(product.rewards, Source(product));
            _events.Publish(new ShopPurchasedEvent(product.id, product.priceKind));
        }

        private static string Source(ShopProductDefinition product) => "shop:" + product.id;
    }
}
