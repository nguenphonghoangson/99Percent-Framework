using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.UI
{
    /// <summary>
    ///     id → sprite for everything data-driven that needs a picture: currencies, items, avatars, products.
    ///     Ids follow the design data ("coin", "avatar_02", "gems_small"). A missing id is not an error: views
    ///     fall back to a generated placeholder, so content can ship before its art does.
    /// </summary>
    [CreateAssetMenu(menuName = "NinetyNine/UI/Icon Set", fileName = "UIIconSet")]
    public sealed class UIIconSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id;
            public Sprite sprite;
        }

        public List<Entry> entries = new();

        /// <summary>First id that has a sprite, or null.</summary>
        public Sprite Find(params string[] ids)
        {
            foreach (var id in ids)
            foreach (var entry in entries)
                if (entry.id == id && entry.sprite)
                    return entry.sprite;
            return null;
        }

        /// <summary>
        ///     Shows the sprite for the first matching id; otherwise a colour derived from the first id plus a short
        ///     label, so different placeholders stay distinguishable.
        /// </summary>
        public static void Show(UIIconSet set, Image image, TMP_Text fallback, params string[] ids)
        {
            var sprite = set ? set.Find(ids) : null;
            var key = ids.Length > 0 ? ids[0] : string.Empty;
            if (image != null) {
                image.sprite = sprite ? sprite : image.sprite;
                image.color = sprite ? Color.white : PlaceholderColor(key);
                image.preserveAspect = sprite;
            }
            if (fallback)
            {
                fallback.gameObject.SetActive(!sprite);
                fallback.text = PlaceholderLabel(key);
            }
        }

        // FNV-1a spreads near-identical ids ("avatar_01", "avatar_02") across the hue wheel.
        public static Color PlaceholderColor(string id)
        {
            var hash = 2166136261u;
            foreach (var c in id ?? string.Empty) hash = (hash ^ c) * 16777619u;
            return Color.HSVToRGB(hash % 360 / 360f, 0.6f, 0.95f);
        }

        /// <summary>"avatar_02" → "02", "avatar_crown" → "CRO".</summary>
        public static string PlaceholderLabel(string id)
        {
            if (string.IsNullOrEmpty(id)) return "?";
            var token = id.Substring(id.LastIndexOf('_') + 1).ToUpperInvariant();
            return token.Length > 3 ? token.Substring(0, 3) : token;
        }
    }
}
