using TMPro;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Editor
{
    /// <summary>One piece of skin: a sprite and the tint it is drawn with.</summary>
    internal readonly struct SkinPart
    {
        public SkinPart(Sprite sprite, Color tint)
        {
            Sprite = sprite;
            Tint = tint;
        }

        public Sprite Sprite { get; }
        public Color Tint { get; }
    }

    /// <summary>
    ///     What every UI element is drawn with. Uses the reference art under <see cref="ReferenceRoot" /> when it is
    ///     in the project, the generated <see cref="UiArt" /> placeholders otherwise — so replacing or deleting the
    ///     reference art and rebuilding the prefabs is the whole re-skin. Builders only ever ask for roles.
    /// </summary>
    internal static class UiSkin
    {
        public const string ReferenceRoot = "Assets/Game/Art/_ThirdPartyReference/PixelFlow";
        public const string ReferenceFontPath = ReferenceRoot + "/Fonts/LilitaOne SDF.asset";
        public const string ReferenceTitleMaterialPath = ReferenceRoot + "/Fonts/LilitaOne SDF Title.mat";

        private static readonly Color Shop = UiKit.Hex(0x264C9C);

        public static bool UsesReference => Reference("HomeMenu_Bg");

        // ---------------------------------------------------------------- screens

        public static SkinPart HomeBackground => Part("HomeMenu_Bg", UiArt.BackgroundHome, Color.white);
        public static SkinPart ShopBackground => UsesReference ? new SkinPart(null, Shop) : Part(null, UiArt.BackgroundShop, Color.white);
        public static SkinPart ShopRoof => Part("Shop_Roof", UiArt.Pill, UiKit.Hex(0x2EE0F5));
        public static SkinPart SectionTitle => Part("Shop_Section_Title", UiArt.Pill, UiKit.SectionFill);
        public static SkinPart ItemCard => Part("Frame_Item_Card_Yellow", UiArt.Rounded32, UiKit.CardFrame);

        // ---------------------------------------------------------------- buttons

        public static SkinPart GreenButton => Part("Button_Green_Rounder", UiArt.Pill, UiKit.Green);
        public static SkinPart GreenButtonSmall => Part("Button_Green_Rounder_S", UiArt.Pill, UiKit.Green);
        public static SkinPart BlueButton => Part("Button_Blue_Rounder_S", UiArt.Pill, UiKit.Blue);
        public static SkinPart PlayButton => Part("Button_Play", UiArt.Pill, UiKit.Yellow);
        public static SkinPart DarkButton => Part("Button_Dark_Small", UiArt.Rounded16, UiKit.Hex(0x2A3052));
        public static SkinPart CloseButton => Part("Button_Close_Popup", UiArt.Rounded16, UiKit.Red);

        /// <summary>Cross drawn over <see cref="CloseButton" />; the reference button has it baked in.</summary>
        public static SkinPart CloseIcon => UsesReference ? default : Part(null, UiArt.Cross, Color.white);

        // ---------------------------------------------------------------- panels

        public static SkinPart PopupPanel => Part("Popup_With_Header_Blue", UiArt.Rounded32, UiKit.PanelFill);
        public static SkinPart PopupPanelRed => Part("Popup_With_Header_Red", UiArt.Rounded32, UiKit.Red);
        public static SkinPart PopupInset => Part("Popup_Section_Blue_Lilac", UiArt.Rounded32, UiKit.InsetFill);
        public static SkinPart DarkPanel => Part("Popup_Section_BlueDark_BorderRadiusSmall", UiArt.Rounded32, UiKit.SectionFill);
        public static SkinPart InputField => Part("Frame_Basic_Rounded78", UiArt.Pill, UiKit.Hex(0x2E6BD6), UiKit.Hex(0x2E6BD6));
        public static SkinPart Pencil => Part("PictoIcon_Pencil", UiArt.Pencil, Color.white);
        public static SkinPart AvatarFrame => Part("Frame_Avatar_Blue", UiArt.Rounded32, UiKit.TileFill);
        public static SkinPart AvatarFrameSelected => Part("Frame_Avatar_Gold", UiArt.Rounded32, UiKit.Gold);
        public static SkinPart Lock => Part("Icon_Lock_White", UiArt.Lock, Color.white);

        // ---------------------------------------------------------------- HUD

        public static SkinPart HudPill => Part("Resource_Frame", UiArt.Pill, UiKit.HudPill);
        public static SkinPart Heart => Part("Icon_Heart", UiArt.HeartIcon, Color.white);
        public static SkinPart Coin => Part("Icon_Gold", UiArt.CoinIcon, Color.white);
        public static SkinPart Plus => Part("Plus_Yellow", UiArt.Plus, UiKit.Orange);
        public static SkinPart SettingsBackground => Part("Button_Circle_Dark", UiArt.Pill, UiKit.Hex(0x2A3052));
        public static SkinPart SettingsIcon => Part("Icon_Settings", UiArt.Gear, Color.white);
        public static SkinPart NavBar => UsesReference ? Part("Home_Button_Bar", null, Color.white) : new SkinPart(null, UiKit.NavBar);
        public static SkinPart NavSelected => Part("Home_Button_Selected", UiArt.Rounded32, UiKit.NavSelected);
        public static SkinPart NavSeparator => UsesReference ? Part("Home_Button_Bar_Separator", null, Color.white) : new SkinPart(null, UiKit.Navy);
        public static SkinPart ShopTabIcon => Part("Icon_Shop", UiArt.ShopIcon, UiKit.Hex(0xFF5C7A));
        public static SkinPart StartTabIcon => Part("Start_Pig_Turquoise", UiArt.StartIcon, UiKit.Hex(0x4AB8FF));
        public static SkinPart TrophyTabIcon => Part("Icon_Trophy", UiArt.TrophyIcon, UiKit.Gold);

        // ---------------------------------------------------------------- home

        public static SkinPart LevelNode => Part("Level_Normal", UiArt.Hexagon, UiKit.Hex(0x2F6CE8));
        public static SkinPart LevelGlow => Part("Level_Current_Shiny", UiArt.Hexagon, new Color(1, 1, 1, 0.35f));
        public static SkinPart Rail => Part("Level_Tree_Column_Yellow", UiArt.Pill, UiKit.Hex(0xF6B926));
        public static SkinPart RailDim => Part("Level_Tree_Column", UiArt.Pill, UiKit.Navy);
        public static SkinPart MilestoneRing => Part("Button_Event_Circle_Blue", UiArt.Pill, UiKit.Hex(0x2C5FD8));
        public static SkinPart MilestoneLabel => Part("Frame_Label_Rounded_Dark", UiArt.Rounded16, UiKit.NavBar);
        public static SkinPart DailyButton => Part("Daily Bonus Main Button Image", UiArt.CalendarIcon, UiKit.Blue);
        public static SkinPart Badge => Part("Notification_Red", UiArt.Pill, UiKit.Red);

        // ---------------------------------------------------------------- daily / result / gameplay

        public static SkinPart DayUpcoming => Part("DailyBonusCalendarEntryBg", UiArt.Rounded32, UiKit.TileFill);
        public static SkinPart DayToday => Part("DailyBonusCalendarEntryTodayBg", UiArt.Rounded32, UiKit.Green);
        public static SkinPart DayClaimed => Part("DailyBonusCalendarEntryCollectedBg", UiArt.Rounded32, UiKit.Hex(0x2B3B86));
        public static SkinPart Check => Part("Check_Okay_Success_Icon", UiArt.Star, UiKit.Green);
        public static SkinPart StarOn => Part("Icon_Rate_Star", UiArt.Star, UiKit.Gold);
        public static SkinPart StarOff => Part("Icon_Rate_Star_Empty", UiArt.Star, UiKit.Hex(0x2A3A7A));
        public static SkinPart BoardCell => Part("Frame_Basic_Rounded78", UiArt.Rounded16, Color.white);

        // ---------------------------------------------------------------- icons by content id

        /// <summary>Reference sprite for an icon-set id (currencies, items, avatars, products), or null.</summary>
        public static Sprite ContentIcon(string id)
        {
            if (!UsesReference) return null;
            if (id.StartsWith("avatar_"))
            {
                var avatar = id switch
                {
                    "avatar_crown" => 7,
                    _ => int.TryParse(id.Substring("avatar_".Length), out var n) ? n - 1 : -1
                };
                return avatar >= 0 ? Reference($"Avatar Image {avatar}") : null;
            }

            return Reference(id switch
            {
                "coin" => "Icon_Gold",
                "life" => "Icon_Heart",
                "booster_hammer" or "hammer_x3" => "Icon_Hammer_Block_Tight",
                "free_gift" => "Icon_Gold_Gift",
                "coins_500" => "Golds_1_2x",
                "starter_pack" => "Chest_5_S",
                "title_solver" or "title_master" => "Icon_Star_3x",
                _ when id.StartsWith("gold_pack_") && int.TryParse(id.Substring("gold_pack_".Length), out var p) => $"Golds_{p - 1}_2x",
                _ => null
            });
        }

        // ---------------------------------------------------------------- fonts

        public static TMP_FontAsset Font =>
            (UsesReference ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ReferenceFontPath) : null) ?? TMP_Settings.defaultFontAsset;

        /// <summary>Outlined, drop-shadowed title style on <see cref="Font" />.</summary>
        public static Material TitleMaterial
        {
            get
            {
                var font = Font;
                if (font != TMP_Settings.defaultFontAsset) return EnsureTitleMaterial(font, ReferenceTitleMaterialPath);
                return UiArt.TitleMaterial;
            }
        }

        private static Material EnsureTitleMaterial(TMP_FontAsset font, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;

            var material = new Material(font.material) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.1f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, UiKit.Hex(0x121426));
            material.SetColor(ShaderUtilities.ID_UnderlayColor, UiKit.Hex(0x121426));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.8f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- lookup

        private static SkinPart Part(string reference, string generated, Color generatedTint, Color? referenceTint = null)
        {
            var sprite = Reference(reference);
            if (sprite) return new SkinPart(sprite, referenceTint ?? Color.white);
            return new SkinPart(generated == null ? null : UiArt.Get(generated), generatedTint);
        }

        private static Sprite Reference(string name) =>
            string.IsNullOrEmpty(name) ? null : AssetDatabase.LoadAssetAtPath<Sprite>($"{ReferenceRoot}/Sprites/{name}.png");
    }
}
