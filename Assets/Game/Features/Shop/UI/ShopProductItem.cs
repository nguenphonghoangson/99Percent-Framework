using System;
using System.Globalization;
using NinetyNine.Modules.Reward;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Shop.UI
{
    /// <summary>
    ///     One product card: icon, headline amount, price button, status line. Rebound in place on every render,
    ///     so it never keeps stale state.
    /// </summary>
    public sealed class ShopProductItem : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text iconFallback;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buyButton;

        private Action<string> _onBuy;
        private string _productId;

        private void Awake() => buyButton.onClick.AddListener(() => _onBuy?.Invoke(_productId));

        public void Bind(ShopProductView product, UIIconSet icons, Action<string> onBuy)
        {
            _productId = product.Id;
            _onBuy = onBuy;
            name = "Product_" + product.Id;

            var single = product.Rewards is { items: { Count: 1 } } ? product.Rewards.items[0] : (RewardItem?)null;
            UIIconSet.Show(icons, icon, iconFallback, product.Id, single?.id ?? product.Id);
            if (amountText) amountText.text = single.HasValue ? single.Value.amount.ToString("N0", CultureInfo.InvariantCulture) : Prettify(product.Id);

            if (priceText) priceText.text = product.PriceLabel;
            if (buyButton) buyButton.interactable = product.Availability == ProductAvailability.Available;
            if (statusText) statusText.text = product.Availability switch
            {
                ProductAvailability.SoldOut => "Sold out",
                ProductAvailability.Locked => "Locked",
                ProductAvailability.CannotAfford => "Not enough",
                ProductAvailability.Unavailable => "Unavailable",
                _ => product.PurchaseLimit > 0 ? $"{product.PurchaseCount}/{product.PurchaseLimit}" : string.Empty
            };
        }

        // Placeholder title from the id ("starter_pack" → "Starter pack") until names come from Localization.
        private static string Prettify(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;
            var text = id.Replace('_', ' ');
            return char.ToUpperInvariant(text[0]) + text.Substring(1);
        }
    }
}
