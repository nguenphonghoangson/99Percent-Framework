using System.Collections.Generic;
using System.Threading.Tasks;
using NinetyNine.Core;

namespace NinetyNine.Features.Shop
{
    public enum ProductAvailability
    {
        Available,
        Locked,
        SoldOut,
        CannotAfford,

        /// <summary>IAP product on a build without the IAP module.</summary>
        Unavailable
    }

    /// <summary>
    ///     Catalog + purchase flow. Pays out through Reward, charges through Economy or IAP, and owns only the
    ///     purchase counts.
    /// </summary>
    public interface IShopService
    {
        bool IsUnlocked { get; }

        IReadOnlyList<ShopProductDefinition> Products { get; }

        int GetPurchaseCount(string productId);

        ProductAvailability GetAvailability(string productId);

        /// <summary>"Free", "500 coin" or the store's localised price.</summary>
        string GetPriceLabel(string productId);

        Task<Result> PurchaseAsync(string productId);
    }

    public static class ShopErrors
    {
        public const string UnknownProduct = "shop.unknown_product";
        public const string ProductLocked = "shop.product_locked";
        public const string SoldOut = "shop.sold_out";
        public const string IapUnavailable = "shop.iap_unavailable";
        public const string PurchaseInProgress = "shop.purchase_in_progress";
        public const string PurchaseCancelled = "shop.purchase_cancelled";
        public const string PurchasePending = "shop.purchase_pending";
        public const string PurchaseFailed = "shop.purchase_failed";
    }

    public readonly struct ShopPurchasedEvent
    {
        public ShopPurchasedEvent(string productId, PriceKind priceKind)
        {
            ProductId = productId;
            PriceKind = priceKind;
        }

        public string ProductId { get; }
        public PriceKind PriceKind { get; }
    }
}
