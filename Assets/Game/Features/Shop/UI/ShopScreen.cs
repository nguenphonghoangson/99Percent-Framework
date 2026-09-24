using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.UI;
using TMPro;
using UnityEngine;

namespace NinetyNine.Features.Shop.UI
{
    /// <summary>
    ///     uGUI implementation of <see cref="IShopView" />, the "Shop" tab (key <see cref="ShopFeature.Id" />).
    ///     Products are grouped by their config <c>section</c>, in config order: one header + one grid per
    ///     section, cloned from templates inside the prefab so artists style them in place. Holds no shop logic.
    /// </summary>
    public sealed class ShopScreen : UIScreen, IShopView
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject sectionHeaderTemplate;
        [SerializeField] private RectTransform sectionGridTemplate;
        [SerializeField] private ShopProductItem itemPrefab;
        [SerializeField] private GameObject listRoot;
        [SerializeField] private GameObject lockedLabel;
        [SerializeField] private GameObject busyOverlay;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private UIIconSet icons;

        private readonly List<Section> _sections = new();
        private ShopPresenter _presenter;
        private IShopService _shop;

        private void Awake()
        {
            sectionHeaderTemplate.SetActive(false);
            sectionGridTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            var services = ServiceLocator.Current;
            if (services == null || !services.TryGet(out _shop))
            {
                Debug.LogWarning("[ShopScreen] Shop service unavailable - start from the Intro scene.", this);
                return;
            }

            messageText.text = string.Empty;
            busyOverlay.SetActive(false);
            _presenter = new ShopPresenter(_shop, services.Require<IEventBus>(), this);
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        public void Render(IReadOnlyList<ShopProductView> products)
        {
            lockedLabel.SetActive(!_shop.IsUnlocked);
            listRoot.SetActive(_shop.IsUnlocked);

            foreach (var section in _sections) section.Used = 0;
            foreach (var product in products) SectionFor(product.Section).Add(product, itemPrefab, icons, OnBuy);
            foreach (var section in _sections) section.Trim();
        }

        public void SetBusy(bool busy) => busyOverlay.SetActive(busy);

        public void ShowPurchaseResult(string productId, Result result) => messageText.text = Describe(result);

        private Section SectionFor(string id)
        {
            foreach (var section in _sections)
                if (section.Id == id)
                    return section;

            var header = Instantiate(sectionHeaderTemplate, content);
            header.SetActive(true);
            header.GetComponentInChildren<TMP_Text>().text = Title(id);
            var grid = Instantiate(sectionGridTemplate, content);
            grid.gameObject.SetActive(true);
            var created = new Section(id, header, grid);
            _sections.Add(created);
            return created;
        }

        // async void is confined to this UI edge; the presenter below it returns a Task.
        private async void OnBuy(string productId)
        {
            try
            {
                if (_presenter != null) await _presenter.PurchaseAsync(productId);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        // Placeholder section title from the config id ("gold_packs" → "GOLD PACKS") until Localization.
        private static string Title(string id) => string.IsNullOrEmpty(id) ? "SHOP" : id.Replace('_', ' ').ToUpperInvariant();

        private static string Describe(Result result) => result.Error switch
        {
            null => "Purchased!",
            ShopErrors.SoldOut => "Sold out.",
            ShopErrors.ProductLocked => "Not unlocked yet.",
            ShopErrors.PurchaseCancelled => "Purchase cancelled.",
            ShopErrors.PurchasePending => "Purchase pending - it will arrive once the store confirms.",
            ShopErrors.PurchaseInProgress => "Another purchase is in progress.",
            ShopErrors.IapUnavailable => "Store unavailable.",
            EconomyErrors.InsufficientFunds => "Not enough currency.",
            CommonErrors.FeatureLocked => "Shop is locked.",
            _ => $"Purchase failed ({result.Error})."
        };

        private sealed class Section
        {
            private readonly GameObject _header;
            private readonly RectTransform _grid;
            private readonly List<ShopProductItem> _items = new();

            public Section(string id, GameObject header, RectTransform grid)
            {
                Id = id;
                _header = header;
                _grid = grid;
            }

            public string Id { get; }
            public int Used { get; set; }

            public void Add(ShopProductView product, ShopProductItem prefab, UIIconSet icons, Action<string> onBuy)
            {
                if (Used == _items.Count) _items.Add(Instantiate(prefab, _grid));
                var item = _items[Used++];
                item.gameObject.SetActive(true);
                item.Bind(product, icons, onBuy);
            }

            public void Trim()
            {
                for (var i = Used; i < _items.Count; i++) _items[i].gameObject.SetActive(false);
                _header.SetActive(Used > 0);
                _grid.gameObject.SetActive(Used > 0);
            }
        }
    }
}
