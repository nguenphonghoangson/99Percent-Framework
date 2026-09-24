using System;
using System.Collections.Generic;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using UnityEngine;

namespace NinetyNine.Features.Shop
{
    public enum PriceKind
    {
        Free,
        Currency,
        Iap
    }

    [Serializable]
    public sealed class ShopProductDefinition
    {
        public string id;
        public string section;
        public PriceKind priceKind;

        [Tooltip("PriceKind.Currency only.")]
        public Cost cost;

        [Tooltip("PriceKind.Iap only: the store product id.")]
        public string iapProductId;

        public RewardBundle rewards = new();

        [Tooltip("Lifetime purchase limit. 0 = unlimited.")]
        public int purchaseLimit;

        public UnlockCondition unlock;
    }

    [CreateAssetMenu(menuName = "NinetyNine/Features/Shop Config", fileName = "ShopConfig")]
    public sealed class ShopConfig : ScriptableObject
    {
        public UnlockCondition featureUnlock;
        public List<ShopProductDefinition> products = new();

        public ShopProductDefinition Find(string productId) => products.Find(p => p.id == productId);

        public ShopProductDefinition FindByIapProduct(string iapProductId) =>
            products.Find(p => p.priceKind == PriceKind.Iap && p.iapProductId == iapProductId);

        private void OnValidate()
        {
            var ids = new HashSet<string>();
            foreach (var product in products)
            {
                if (!ids.Add(product.id)) Debug.LogWarning($"[ShopConfig] Duplicate product id {product.id}.", this);
                if (product.priceKind == PriceKind.Iap && string.IsNullOrEmpty(product.iapProductId))
                    Debug.LogWarning($"[ShopConfig] {product.id} is an IAP product without a store id.", this);
            }
        }
    }
}
