using System.Collections.Generic;
using NinetyNine.Features.Profile;
using NinetyNine.Features.Shop;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using UnityEditor;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Content the mock-ups show and the default configs lacked: the six "Gold Packs" in the shop and a full 3x3
    ///     avatar grid. Fill-missing only — ids that already exist, and every other product or avatar, are left as the
    ///     designers set them.
    /// </summary>
    internal static class UiShowcaseContent
    {
        private static readonly long[] GoldPackAmounts = { 1000, 5000, 10000, 25000, 50000, 100000 };

        public static void Ensure()
        {
            EnsureGoldPacks(AssetDatabase.LoadAssetAtPath<ShopConfig>(FrameworkSetup.ShopPath));
            EnsureAvatars(AssetDatabase.LoadAssetAtPath<ProfileFeatureConfig>(FrameworkSetup.ProfilePath));
        }

        private static void EnsureGoldPacks(ShopConfig shop)
        {
            if (!shop || shop.Find("gold_pack_1") != null) return;

            var packs = new List<ShopProductDefinition>();
            for (var i = 0; i < GoldPackAmounts.Length; i++)
                packs.Add(new ShopProductDefinition
                {
                    id = $"gold_pack_{i + 1}", section = "gold_packs", priceKind = PriceKind.Iap,
                    iapProductId = $"gold_pack_{i + 1}", rewards = RewardBundle.Of(RewardItem.Currency("coin", GoldPackAmounts[i]))
                });
            shop.products.InsertRange(0, packs);
            EditorUtility.SetDirty(shop);
        }

        // Mock-up order: … avatar_04, spikes, devil, mystery, king (avatar_crown), rocket.
        private static void EnsureAvatars(ProfileFeatureConfig profile)
        {
            if (!profile) return;

            var changed = false;
            var beforeCrown = new[]
            {
                ("avatar_05", UnlockCondition.Always), ("avatar_06", UnlockCondition.AtLevel(15)),
                ("avatar_07", UnlockCondition.AtLevel(25))
            };
            foreach (var (id, unlock) in beforeCrown)
            {
                if (profile.FindAvatar(id) != null) continue;
                var crown = profile.avatars.FindIndex(a => a.id == "avatar_crown");
                profile.avatars.Insert(crown < 0 ? profile.avatars.Count : crown, new ProfileOption { id = id, unlock = unlock });
                changed = true;
            }

            if (profile.FindAvatar("avatar_08") == null)
            {
                profile.avatars.Add(new ProfileOption { id = "avatar_08", unlock = UnlockCondition.AtLevel(30) });
                changed = true;
            }

            if (changed) EditorUtility.SetDirty(profile);
        }
    }
}
