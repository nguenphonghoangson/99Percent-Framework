using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.Shop.UI
{
    public readonly struct ShopProductView
    {
        public ShopProductView(ShopProductDefinition product, string priceLabel, ProductAvailability availability,
            int purchaseCount)
        {
            Id = product.id;
            Section = product.section;
            PriceKind = product.priceKind;
            Rewards = product.rewards;
            PurchaseLimit = product.purchaseLimit;
            PriceLabel = priceLabel;
            Availability = availability;
            PurchaseCount = purchaseCount;
        }

        public string Id { get; }
        public string Section { get; }
        public PriceKind PriceKind { get; }
        public RewardBundle Rewards { get; }
        public string PriceLabel { get; }
        public ProductAvailability Availability { get; }
        public int PurchaseCount { get; }
        public int PurchaseLimit { get; }
    }

    public interface IShopView
    {
        void Render(IReadOnlyList<ShopProductView> products);

        void SetBusy(bool busy);

        void ShowPurchaseResult(string productId, Result result);
    }

    /// <summary>Re-renders whenever something that changes availability happens: balance, items, level, purchases.</summary>
    public sealed class ShopPresenter : IDisposable
    {
        private readonly List<ShopProductView> _buffer = new();
        private readonly IShopService _shop;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly IShopView _view;

        public ShopPresenter(IShopService shop, IEventBus events, IShopView view)
        {
            _shop = shop;
            _view = view;
            _subscriptions.Add(events.Subscribe<CurrencyChangedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<ItemChangedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<LevelProgressedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<ShopPurchasedEvent>(_ => Refresh()));
            Refresh();
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
        }

        public void Refresh()
        {
            _buffer.Clear();
            foreach (var product in _shop.Products)
                _buffer.Add(new ShopProductView(product, _shop.GetPriceLabel(product.id),
                    _shop.GetAvailability(product.id), _shop.GetPurchaseCount(product.id)));
            _view.Render(_buffer);
        }

        public async Task PurchaseAsync(string productId)
        {
            _view.SetBusy(true);
            try
            {
                _view.ShowPurchaseResult(productId, await _shop.PurchaseAsync(productId));
            }
            finally
            {
                _view.SetBusy(false);
            }
        }
    }
}
