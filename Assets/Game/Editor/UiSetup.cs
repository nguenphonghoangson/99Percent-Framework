using System;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.DailyReward.UI;
using NinetyNine.Features.Home;
using NinetyNine.Features.Profile;
using NinetyNine.Features.Profile.UI;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.PuzzleGameplay.UI;
using NinetyNine.Features.Result;
using NinetyNine.Features.Shop;
using NinetyNine.Features.Shop.UI;
using NinetyNine.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static NinetyNine.Editor.UiKit;
using Object = UnityEngine.Object;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Builds the UI prefabs, the global UI catalog and the icon set. Layouts follow the game mock-ups at the
    ///     iPhone 12 Pro reference size (1170x2532, positions measured on the 924 px screenshots and scaled by
    ///     <see cref="UiKit.Mock" />); art comes from <see cref="UiSkin" />. Existing prefabs are left alone, so artists
    ///     can restyle one and re-running only fills in what is missing; <see cref="UiRebuild" /> throws the prefabs
    ///     away and builds them again.
    /// </summary>
    public static class UiSetup
    {
        public const string MainHudPath = "Assets/Game/Features/Home/UI/MainHud.prefab";
        public const string HomeScreenPath = "Assets/Game/Features/Home/UI/HomeScreen.prefab";
        public const string ShopItemPath = "Assets/Game/Features/Shop/UI/ShopItem.prefab";
        public const string ShopScreenPath = "Assets/Game/Features/Shop/UI/ShopScreen.prefab";
        public const string ProfilePopupPath = "Assets/Game/Features/Profile/UI/ProfilePopup.prefab";
        public const string DayCellPath = "Assets/Game/Features/DailyReward/UI/DayCell.prefab";
        public const string DailyPopupPath = "Assets/Game/Features/DailyReward/UI/DailyRewardPopup.prefab";
        public const string GameplayScreenPath = "Assets/Game/Features/Gameplay/UI/GameplayScreen.prefab";
        public const string WinPopupPath = "Assets/Game/Features/Result/UI/WinPopup.prefab";
        public const string LosePopupPath = "Assets/Game/Features/Result/UI/LosePopup.prefab";
        public const string CatalogPath = "Assets/Game/Content/UI/UICatalog.asset";
        public const string IconSetPath = "Assets/Game/Content/UI/UIIconSet.asset";

        private const float HudTop = 203; // centre line of the top bar, from the screen top
        private const float NavHeight = 247;

        [MenuItem("NinetyNine/Setup/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (HasTmpEssentials()) return;

            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro");
            AssetDatabase.ImportPackage(package.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage", false);
        }

        /// <summary>
        ///     Art → content → icon set → prefabs → catalog. False when TMP Essentials had to be imported first —
        ///     Unity finishes that import after this call returns, so the caller runs again.
        /// </summary>
        public static bool CreatePrefabsAndCatalog()
        {
            if (!HasTmpEssentials())
            {
                ImportTmpEssentials();
                Debug.LogWarning("[UiSetup] TMP Essentials were just imported. Run NinetyNine/Setup again to build the UI.");
                return false;
            }

            UiArt.EnsureAll();
            UiShowcaseContent.Ensure();
            var icons = EnsureIconSet();

            var hud = GetOrCreatePrefab(MainHudPath, () => BuildMainHud(icons));
            var home = GetOrCreatePrefab(HomeScreenPath, () => BuildHomeScreen(hud.GetComponent<MainHudView>(), icons));
            var shopItem = GetOrCreatePrefab(ShopItemPath, BuildShopItem);
            var shop = GetOrCreatePrefab(ShopScreenPath, () => BuildShopScreen(shopItem.GetComponent<ShopProductItem>(), icons));
            var profile = GetOrCreatePrefab(ProfilePopupPath, () => BuildProfilePopup(icons));
            var dayCell = GetOrCreatePrefab(DayCellPath, () => BuildDayCell(icons));
            var daily = GetOrCreatePrefab(DailyPopupPath, () => BuildDailyPopup(dayCell.GetComponent<DayCellView>()));
            var gameplay = GetOrCreatePrefab(GameplayScreenPath, BuildGameplayScreen);
            var win = GetOrCreatePrefab(WinPopupPath, BuildWinPopup);
            var lose = GetOrCreatePrefab(LosePopupPath, BuildLosePopup);

            var catalog = AssetDatabase.LoadAssetAtPath<UICatalog>(CatalogPath);
            if (!catalog)
            {
                FrameworkSetup.EnsureFolder(System.IO.Path.GetDirectoryName(CatalogPath)?.Replace('\\', '/'));
                catalog = ScriptableObject.CreateInstance<UICatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            // The canvas must match the size the prefabs are laid out for, so these are not designer-tunable.
            var changed = catalog.referenceResolution != new Vector2(ReferenceWidth, ReferenceHeight) ||
                          catalog.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand;
            catalog.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            catalog.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            // Fills in only what is missing: entries a designer already tuned (keepInstance, a swapped prefab) stay.
            changed |= AddEntry(catalog, HomeScreenView.ScreenId, home);
            changed |= AddEntry(catalog, ShopFeature.Id, shop);
            changed |= AddEntry(catalog, ProfileFeature.Id, profile);
            changed |= AddEntry(catalog, DailyRewardFeature.Id, daily);
            changed |= AddEntry(catalog, PuzzleGameplayFeature.Id, gameplay);
            changed |= AddEntry(catalog, LevelEndKeys.Win, win);
            changed |= AddEntry(catalog, LevelEndKeys.Lose, lose);
            if (changed) EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            return true;
        }

        // ---------------------------------------------------------------- HUD (top bar + bottom nav)

        private static GameObject BuildMainHud(UIIconSet icons)
        {
            var root = Stretch(Node("MainHud", null));

            // Top bar: left group anchored left, coins + settings anchored right (wider screens spread them apart).
            var avatar = At(Node("AvatarButton", root), 0, 1, 156, 161, new Vector2(108, -HudTop));
            var avatarFrame = Img(avatar, UiSkin.AvatarFrame, true);
            var avatarIcon = Img(Stretch(Node("Icon", avatar), 16, 17, 16, 15), UiSkin.AvatarFrame);
            var avatarFallback = Title(avatarIcon.rectTransform, "Fallback", "?", 52);
            var avatarButton = avatar.gameObject.AddComponent<Button>();
            avatarButton.targetGraphic = avatarFrame;

            var lives = At(Node("LivesGroup", root), 0, 1, 294, 96, new Vector2(438, -HudTop));
            Img(lives, UiSkin.HudPill, false, 0.9f);
            Icon(At(Node("Heart", lives), 0, 0.5f, 124, 103, new Vector2(0, 2)), UiSkin.Heart);
            var livesText = Title(At(Node("LivesTextBox", lives), 0, 0.5f, 110, 90, new Vector2(0, 6)), "LivesText", "5", 60);
            var livesStatus = Title(Place(Node("StatusBox", lives), 0.22f, 0, 1, 1, 0, 0, 12, 0), "LivesStatusText", "MAX", 64);

            var coins = At(Node("CoinGroup", root), 1, 1, 258, 96, new Vector2(-370, -HudTop));
            Img(coins, UiSkin.HudPill, false, 0.9f);
            Icon(At(Node("Coin", coins), 0, 0.5f, 114, 129), UiSkin.Coin);
            var coinText = Title(Place(Node("CoinTextBox", coins), 0.2f, 0, 0.84f, 1), "CoinText", "500", 62);
            var addCoins = IconButton(coins, "AddCoinsButton", UiSkin.Plus, default, 0);
            At((RectTransform)addCoins.transform, 1, 0.5f, 102, 99, new Vector2(0, 0));

            var settings = IconButton(root, "SettingsButton", UiSkin.SettingsBackground, UiSkin.SettingsIcon, 0.2f);
            At((RectTransform)settings.transform, 1, 1, 110, 115, new Vector2(-91, -HudTop));

            // Bottom nav: bar, three equal tabs, separators. The raised "Selected" art covers the resting icon and
            // carries the lifted icon + label, so MainHudView only toggles one object per tab.
            var nav = Bottom(Node("BottomNav", root), 0, NavHeight);
            Img(nav, UiSkin.NavBar, true);
            var tabs = new (string name, SkinPart icon, string label, Vector2 size)[]
            {
                ("ShopTab", UiSkin.ShopTabIcon, "Shop", new Vector2(152, 158)),
                ("StartTab", UiSkin.StartTabIcon, "Start", new Vector2(165, 176)),
                ("TrophyTab", UiSkin.TrophyTabIcon, "Trophy", new Vector2(160, 145))
            };
            var buttons = new Button[3];
            var selected = new GameObject[3];
            for (var i = 0; i < tabs.Length; i++)
            {
                var tab = Place(Node(tabs[i].name, nav), i / 3f, 0, (i + 1) / 3f, 1);
                buttons[i] = tab.gameObject.AddComponent<Button>();
                buttons[i].targetGraphic = Solid(tab, Color.clear, true);
                Icon(At(Node("Icon", tab), 0.5f, 0, tabs[i].size.x, tabs[i].size.y, new Vector2(0, 139)), tabs[i].icon);

                var raised = Place(Node("Selected", tab), 0, 0, 1, 0, 3, 0, 3, -276);
                Img(raised, UiSkin.NavSelected);
                Icon(At(Node("Icon", raised), 0.5f, 0, tabs[i].size.x * 1.08f, tabs[i].size.y * 1.08f, new Vector2(0, 186)), tabs[i].icon);
                Title(Bottom(Node("LabelBox", raised), 38, 76), "Label", tabs[i].label, 64);
                selected[i] = raised.gameObject;

                if (i > 0) Img(At(Node("Separator", nav), i / 3f, 0, 6, 120, new Vector2(0, 110)), UiSkin.NavSeparator);
            }

            var view = root.gameObject.AddComponent<MainHudView>();
            Wire(view, "avatarButton", avatarButton);
            Wire(view, "avatarIcon", avatarIcon);
            Wire(view, "avatarFallback", avatarFallback);
            Wire(view, "livesGroup", lives.gameObject);
            Wire(view, "livesText", livesText);
            Wire(view, "livesStatusText", livesStatus);
            Wire(view, "coinText", coinText);
            Wire(view, "addCoinsButton", addCoins);
            Wire(view, "settingsButton", settings);
            Wire(view, "shopTab", buttons[0]);
            Wire(view, "startTab", buttons[1]);
            Wire(view, "trophyTab", buttons[2]);
            Wire(view, "shopTabSelected", selected[0]);
            Wire(view, "startTabSelected", selected[1]);
            Wire(view, "trophyTabSelected", selected[2]);
            Wire(view, "icons", icons);
            return root.gameObject;
        }

        // ---------------------------------------------------------------- Home (level path)

        private static GameObject BuildHomeScreen(MainHudView hud, UIIconSet icons)
        {
            var root = Stretch(Node("HomeScreen", null));
            Img(root, UiSkin.HomeBackground, true);

            // Everything on the path is placed from the screen centre (mock-up y 1000), so taller or shorter
            // screens keep it centred. Node y: current level at -276, then 175, 627, 1076 (under the top bar).
            float[] nodeY = { -276, 175, 627, 1076 };
            Img(At(Node("RailLit", root), 0.5f, 0.5f, 40, nodeY[2] - nodeY[0], new Vector2(0, (nodeY[0] + nodeY[2]) / 2)), UiSkin.Rail);
            Img(At(Node("RailAhead", root), 0.5f, 0.5f, 34, nodeY[3] - nodeY[2], new Vector2(0, (nodeY[2] + nodeY[3]) / 2)), UiSkin.RailDim);
            Icon(At(Node("CurrentGlow", root), 0.5f, 0.5f, 446, 459, new Vector2(0, nodeY[0])), UiSkin.LevelGlow);

            var numbers = new TMP_Text[nodeY.Length];
            for (var i = 0; i < nodeY.Length; i++)
            {
                var scale = i == 0 ? 1.05f : 0.95f;
                var node = At(Node($"LevelNode{i}", root), 0.5f, 0.5f, 241 * scale, 272 * scale, new Vector2(0, nodeY[i]));
                Icon(node, UiSkin.LevelNode);
                numbers[i] = Title(Place(Node("NumberBox", node), 0, 0.08f, 1, 1), "Number", (11 + i).ToString(), i == 0 ? 124 : 112);
                if (i == nodeY.Length - 1) node.gameObject.AddComponent<CanvasGroup>().alpha = 0.55f;
            }

            // Milestone badges: ring centre positions from the mock-up (left x 162, right x 163 from the edge).
            (float anchorX, float x, float y)[] slots = { (0, 162, 734), (1, -163, 734), (0, 162, 393), (1, -163, 393), (1, -163, 70) };
            var milestones = new MilestoneView[slots.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = At(Node($"Milestone{i}", root), slots[i].anchorX, 0.5f, 220, 290, new Vector2(slots[i].x, slots[i].y - 40));
                var ring = At(Node("Ring", slot), 0.5f, 1, 205, 210, new Vector2(0, -105));
                Icon(ring, UiSkin.MilestoneRing);
                var icon = Img(Stretch(Node("Icon", ring), 36, 36, 36, 36), UiSkin.AvatarFrame);
                icon.preserveAspect = true;
                var fallback = Title(icon.rectTransform, "Fallback", "?", 52);
                var tag = At(Node("LevelTag", slot), 0.5f, 0, 190, 60, new Vector2(0, 30));
                Img(tag, UiSkin.MilestoneLabel);
                var level = Title(Place(Node("LevelBox", tag), 0, 0, 1, 1, 12, 2, 12, 0), "LevelText", "LEVEL 31", 40);
                milestones[i] = slot.gameObject.AddComponent<MilestoneView>();
                Wire(milestones[i], "icon", icon);
                Wire(milestones[i], "iconFallback", fallback);
                Wire(milestones[i], "levelText", level);
            }

            var play = SkinButton(root, "PlayButton", UiSkin.PlayButton, "Play", 116, out var playLabel, 0.97f);
            At((RectTransform)play.transform, 0.5f, 0.5f, 517, 233, new Vector2(0, -658));

            var daily = IconButton(root, "DailyButton", UiSkin.DailyButton, default, 0);
            At((RectTransform)daily.transform, 0, 0.5f, 150, 173, new Vector2(162, 90));
            Title(At(Node("DailyLabel", (RectTransform)daily.transform), 0.5f, 0, 220, 64, new Vector2(0, -26)), "Label", "Daily", 48);
            var badge = At(Node("Badge", (RectTransform)daily.transform), 1, 1, 64, 68, new Vector2(-6, -6));
            Icon(badge, UiSkin.Badge);
            Title(Place(Node("LabelBox", badge), 0, 0.08f, 1, 1), "Label", "!", 46);

            var devPanel = At(Node("DevPanel", root), 1, 0, 270, 196, new Vector2(-160, NavHeight + 150));
            var column = devPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 16;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            var devLevel = SkinButton(devPanel, "DevCompleteLevel", UiSkin.DarkButton, "DEV +1 level", 32, out _, 0.8f);
            Height(devLevel, 90);
            var devCoins = SkinButton(devPanel, "DevAddCoins", UiSkin.DarkButton, "DEV +1000 coin", 32, out _, 0.8f);
            Height(devCoins, 90);

            var view = root.gameObject.AddComponent<HomeScreenView>();
            WireArray(view, "levelNodes", numbers);
            WireArray(view, "milestones", milestones);
            Wire(view, "playButton", play);
            Wire(view, "playButtonLabel", playLabel);
            Wire(view, "dailyButton", daily);
            Wire(view, "dailyBadge", badge.gameObject);
            Wire(view, "hudPrefab", hud);
            Wire(view, "icons", icons);
            Wire(view, "devPanel", devPanel.gameObject);
            Wire(view, "devCompleteLevelButton", devLevel);
            Wire(view, "devAddCoinsButton", devCoins);
            Wire(view, "transition", root.gameObject.AddComponent<FadeTransition>());
            return root.gameObject;
        }

        // ---------------------------------------------------------------- Shop

        private static GameObject BuildShopItem()
        {
            // Grid cell 325x460: card art on top (392 tall), price button hanging over its bottom edge.
            var root = Node("ShopItem", null);
            root.sizeDelta = new Vector2(325, 460);
            Img(Top(Node("Card", root), 0, 392), UiSkin.ItemCard, false, 0.84f);

            var icon = Icon(FromTopLeft(Node("Icon", root), 162, 150, 230, 180), UiSkin.Coin);
            var fallback = Title(icon.rectTransform, "Fallback", "?", 60);
            var amount = Title(Top(Node("AmountBox", root), 236, 84, 16, 16), "AmountText", "1,000", 74);
            var status = Label(Top(Node("StatusBox", root), 26, 40, 30, 30), "StatusText", string.Empty, 30, Hex(0x8A5A12));

            var buy = SkinButton(root, "BuyButton", UiSkin.GreenButtonSmall, "Price", 44, out var price, 0.7f);
            At((RectTransform)buy.transform, 0.5f, 0, 268, 105, new Vector2(0, 52));

            var view = root.gameObject.AddComponent<ShopProductItem>();
            Wire(view, "icon", icon);
            Wire(view, "iconFallback", fallback);
            Wire(view, "amountText", amount);
            Wire(view, "statusText", status);
            Wire(view, "priceText", price);
            Wire(view, "buyButton", buy);
            return root.gameObject;
        }

        private static GameObject BuildShopScreen(ShopProductItem itemPrefab, UIIconSet icons)
        {
            var root = Stretch(Node("ShopScreen", null));
            Img(root, UiSkin.ShopBackground, true);
            Img(Top(Node("Roof", root), 0, 405), UiSkin.ShopRoof);

            var list = Stretch(Node("List", root), 0, NavHeight, 0, 470);
            var viewport = Stretch(Node("Viewport", list));
            viewport.gameObject.AddComponent<RectMask2D>();
            Solid(viewport, Color.clear, true);
            var content = Node("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            var column = content.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(42, 42, 28, 160);
            column.spacing = 44;
            column.childControlWidth = column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40;

            var header = Node("SectionHeaderTemplate", content);
            Height(header, 80);
            Img(header, UiSkin.SectionTitle, false, 0.86f);
            Title(Place(Node("TitleBox", header), 0, 0.06f, 1, 1), "Title", "GOLD PACKS", 58);
            header.gameObject.SetActive(false);

            var grid = Node("SectionGridTemplate", content);
            var cells = grid.gameObject.AddComponent<GridLayoutGroup>();
            cells.cellSize = new Vector2(325, 460);
            cells.spacing = new Vector2(56, 44);
            cells.padding = new RectOffset(0, 0, 0, 16);
            cells.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cells.constraintCount = 3;
            cells.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.SetActive(false);

            var lockedBox = Stretch(Node("LockedBox", root), 80, NavHeight, 80, 470);
            Title(lockedBox, "LockedLabel", "The shop unlocks at a later level.\nKeep playing!", 60).enableWordWrapping = true;

            var message = Title(Bottom(Node("MessageBox", root), NavHeight + 40, 90, 40, 40), "MessageText", string.Empty, 52);

            var busy = Stretch(Node("BusyOverlay", root));
            Solid(busy, Dim, true);
            Title(busy, "Label", "Processing...", 70);
            busy.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<ShopScreen>();
            Wire(view, "content", content);
            Wire(view, "sectionHeaderTemplate", header.gameObject);
            Wire(view, "sectionGridTemplate", grid);
            Wire(view, "itemPrefab", itemPrefab);
            Wire(view, "listRoot", list.gameObject);
            Wire(view, "lockedLabel", lockedBox.gameObject);
            Wire(view, "busyOverlay", busy.gameObject);
            Wire(view, "messageText", message);
            Wire(view, "icons", icons);
            Wire(view, "transition", root.gameObject.AddComponent<FadeTransition>());
            return root.gameObject;
        }

        // ---------------------------------------------------------------- Profile ("Edit Profile" mock-up)

        private static GameObject BuildProfilePopup(UIIconSet icons)
        {
            var panel = PopupShell<ProfilePopup>("ProfilePopup", new Vector2(1007, 1637), 160, UiSkin.PopupPanel, "EDIT PROFILE",
                out var view, out _, out var close, true);

            var preview = FromTopLeft(Node("Preview", panel), 188, 327, 233, 242);
            Img(preview, UiSkin.AvatarFrame);
            var previewIcon = Img(Stretch(Node("Icon", preview), 24, 25, 24, 23), UiSkin.AvatarFrame);
            var previewFallback = Title(previewIcon.rectTransform, "Fallback", "?", 70);

            var input = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            input.name = "NameInput";
            SetLayerRecursively(input);
            var inputRect = (RectTransform)input.transform;
            inputRect.SetParent(panel, false);
            FromTopLeft(inputRect, 653, 325, 567, 154);
            var field = input.GetComponent<TMP_InputField>();
            var fieldImage = input.GetComponent<Image>();
            fieldImage.sprite = UiSkin.InputField.Sprite;
            fieldImage.color = UiSkin.InputField.Tint;
            fieldImage.type = Image.Type.Sliced;
            fieldImage.pixelsPerUnitMultiplier = 1 / 1.5f;
            ((RectTransform)field.textViewport).offsetMin = new Vector2(43, 12);
            ((RectTransform)field.textViewport).offsetMax = new Vector2(-130, -12);
            foreach (var text in new[] { (TextMeshProUGUI)field.textComponent, (TextMeshProUGUI)field.placeholder })
            {
                text.font = UiSkin.Font;
                text.fontSharedMaterial = UiSkin.TitleMaterial ? UiSkin.TitleMaterial : UiSkin.Font.material;
                text.fontSize = 74;
                text.alignment = TextAlignmentOptions.MidlineLeft;
            }

            field.textComponent.color = Color.white;
            field.placeholder.color = new Color(1, 1, 1, 0.5f);
            ((TextMeshProUGUI)field.placeholder).text = "Your name";
            field.caretColor = Color.white;
            field.customCaretColor = true;
            Icon(At(Node("Pencil", inputRect), 1, 0.5f, 89, 89, new Vector2(-76, 0)), UiSkin.Pencil);

            // Under the name field (x 370..937), clear of the avatar preview on the left.
            var hint = Label(Top(Node("HintBox", panel), 408, 40, 382, 70), "HintText", "First rename is free", 34, Color.white,
                TextAlignmentOptions.MidlineLeft);
            var error = Title(Top(Node("ErrorBox", panel), 450, 42, 382, 70), "ErrorText", string.Empty, 36, TextAlignmentOptions.MidlineLeft);
            error.color = Hex(0xFFE066);

            var inset = FromTopLeft(Node("AvatarGrid", panel), 503.5f, 905, 867, 814);
            Img(inset, UiSkin.PopupInset, false, 0.7f);
            var viewport = Stretch(Node("Viewport", inset), 8, 8, 8, 8);
            viewport.gameObject.AddComponent<RectMask2D>();
            Solid(viewport, Color.clear, true);
            var gridContent = Node("Content", viewport);
            gridContent.anchorMin = new Vector2(0, 1);
            gridContent.anchorMax = new Vector2(1, 1);
            gridContent.pivot = new Vector2(0.5f, 1);
            gridContent.sizeDelta = Vector2.zero;
            var gridLayout = gridContent.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(209, 215);
            gridLayout.spacing = new Vector2(58, 51);
            gridLayout.padding = new RectOffset(53, 53, 52, 52);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = gridContent;
            scroll.horizontal = false;

            var option = Node("AvatarOptionTemplate", gridContent);
            var tile = Img(option, UiSkin.AvatarFrame, true);
            var selectedFrame = Img(Stretch(Node("SelectedFrame", option)), UiSkin.AvatarFrameSelected);
            var optionIcon = Img(Stretch(Node("Icon", option), 21, 22, 21, 20), UiSkin.AvatarFrame);
            var optionFallback = Title(optionIcon.rectTransform, "Fallback", "?", 60);
            var lockOverlay = Stretch(Node("LockOverlay", option), 21, 22, 21, 20);
            Img(lockOverlay, new SkinPart(UiSkin.BoardCell.Sprite, new Color(0.04f, 0.05f, 0.14f, 0.6f)), false, 1.2f);
            Icon(At(Node("Lock", lockOverlay), 0.5f, 0.58f, 76, 108), UiSkin.Lock);
            var lockText = Title(Bottom(Node("LockTextBox", lockOverlay), 6, 52), "LockText", "Lv 10", 42);
            var optionButton = option.gameObject.AddComponent<Button>();
            optionButton.targetGraphic = tile;
            var optionView = option.gameObject.AddComponent<ProfileAvatarOption>();
            Wire(optionView, "button", optionButton);
            Wire(optionView, "icon", optionIcon);
            Wire(optionView, "iconFallback", optionFallback);
            Wire(optionView, "selectedFrame", selectedFrame.gameObject);
            Wire(optionView, "lockOverlay", lockOverlay.gameObject);
            Wire(optionView, "lockText", lockText);
            option.gameObject.SetActive(false);

            var save = SkinButton(panel, "SaveButton", UiSkin.GreenButton, "Save", 100, out _, 0.875f);
            At((RectTransform)save.transform, 0.5f, 0, 496, 196, new Vector2(0, 174));

            Wire(view, "previewIcon", previewIcon);
            Wire(view, "previewFallback", previewFallback);
            Wire(view, "nameInput", field);
            Wire(view, "grid", gridContent);
            Wire(view, "optionTemplate", optionView);
            Wire(view, "hintText", hint);
            Wire(view, "errorText", error);
            Wire(view, "saveButton", save);
            Wire(view, "closeButton", close);
            Wire(view, "icons", icons);
            return panel.parent.gameObject;
        }

        // ---------------------------------------------------------------- Daily Reward

        private static GameObject BuildDayCell(UIIconSet icons)
        {
            var root = Node("DayCell", null);
            root.sizeDelta = new Vector2(270, 312);
            var background = Img(root, UiSkin.DayUpcoming);

            var day = Title(Top(Node("DayBox", root), 14, 60, 10, 10), "DayText", "Day 1", 46);
            var rewardIcon = Icon(FromTopLeft(Node("RewardIcon", root), 135, 165, 150, 130), UiSkin.Coin);
            var rewardFallback = Title(rewardIcon.rectTransform, "Fallback", "?", 44);
            var reward = Title(Bottom(Node("RewardBox", root), 22, 62, 10, 10), "RewardText", "50", 50);
            var claimed = Icon(At(Node("ClaimedMark", root), 0.5f, 0.5f, 120, 118, new Vector2(0, -6)), UiSkin.Check);

            var view = root.gameObject.AddComponent<DayCellView>();
            Wire(view, "background", background);
            Wire(view, "dayText", day);
            Wire(view, "rewardText", reward);
            Wire(view, "rewardIcon", rewardIcon);
            Wire(view, "rewardIconFallback", rewardFallback);
            Wire(view, "icons", icons);
            Wire(view, "claimedMark", claimed.gameObject);
            Wire(view, "claimedSprite", UiSkin.DayClaimed.Sprite);
            Wire(view, "claimableSprite", UiSkin.DayToday.Sprite);
            Wire(view, "upcomingSprite", UiSkin.DayUpcoming.Sprite);
            SetColor(view, "claimedColor", UiSkin.DayClaimed.Tint);
            SetColor(view, "claimableColor", UiSkin.DayToday.Tint);
            SetColor(view, "upcomingColor", UiSkin.DayUpcoming.Tint);
            return root.gameObject;
        }

        private static GameObject BuildDailyPopup(DayCellView cellPrefab)
        {
            var panel = PopupShell<DailyRewardPopup>("DailyRewardPopup", new Vector2(1007, 1760), 160, UiSkin.PopupPanel,
                "DAILY REWARD", out var view, out _, out var close, true);

            var streak = Title(Top(Node("StreakBox", panel), 180, 70), "StreakText", "Streak: 0 day(s)", 52);
            streak.color = Hex(0xFFE066);

            var inset = Top(Node("Calendar", panel), 265, 3 * 312 + 2 * 34 + 70, 48, 48);
            Img(inset, UiSkin.PopupInset, false, 0.7f);
            var grid = Stretch(Node("Grid", inset), 10, 10, 10, 10);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(270, 312);
            layout.spacing = new Vector2(34, 34);
            layout.padding = new RectOffset(0, 0, 25, 25);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.UpperCenter;

            var message = Title(Bottom(Node("MessageBox", panel), 290, 64, 50, 50), "MessageText", string.Empty, 50);
            var claim = SkinButton(panel, "ClaimButton", UiSkin.GreenButton, "Claim", 90, out var claimLabel, 0.875f);
            At((RectTransform)claim.transform, 0.5f, 0, 540, 196, new Vector2(0, 160));

            Wire(view, "grid", grid);
            Wire(view, "cellPrefab", cellPrefab);
            Wire(view, "streakText", streak);
            Wire(view, "messageText", message);
            Wire(view, "claimButton", claim);
            Wire(view, "claimLabel", claimLabel);
            Wire(view, "closeButton", close);
            return panel.parent.gameObject;
        }

        // ---------------------------------------------------------------- Gameplay

        private static GameObject BuildGameplayScreen()
        {
            var root = Stretch(Node("GameplayScreen", null));
            Img(root, UiSkin.HomeBackground, true);

            var top = Top(Node("TopBar", root), HudTop - 60, 120, 40, 40);
            var home = SkinButton(top, "HomeButton", UiSkin.BlueButton, "Home", 48, out _, 0.7f);
            At((RectTransform)home.transform, 0, 0.5f, 230, 105, new Vector2(115, 0));
            var level = Title(Place(Node("LevelBox", top), 0.24f, 0, 0.7f, 1), "LevelText", "Level 1", 84);
            var movesPill = At(Node("MovesPill", top), 1, 0.5f, 310, 100, new Vector2(-155, 0));
            Img(movesPill, UiSkin.HudPill, false, 0.9f);
            var moves = Title(Place(Node("MovesBox", movesPill), 0, 0, 1, 1, 16, 4, 16, 0), "MovesText", "Moves 20", 56);
            moves.color = Hex(0xFFE066);

            var goals = Top(Node("Objectives", root), HudTop + 100, 220, 50, 50);
            Img(goals, UiSkin.DarkPanel, false, 0.6f);
            var objectives = Title(Top(Node("ObjectivesBox", goals), 22, 110, 30, 30), "ObjectivesText", "Red 0/12", 66);
            objectives.richText = true;
            var score = Label(Bottom(Node("ScoreBox", goals), 22, 60, 30, 30), "ScoreText", "Score 0", 46, new Color(1, 1, 1, 0.85f));

            var frame = Stretch(Node("BoardFrame", root), 36, 280, 36, HudTop + 360);
            Img(frame, UiSkin.PopupInset, false, 0.7f);
            var boardRect = Stretch(Node("Board", frame), 34, 34, 34, 34);
            var board = boardRect.gameObject.AddComponent<BoardView>();
            Wire(board, "cellSprite", UiSkin.BoardCell.Sprite);

            var view = root.gameObject.AddComponent<GameplayScreen>();
            Wire(view, "levelText", level);
            Wire(view, "movesText", moves);
            Wire(view, "scoreText", score);
            Wire(view, "objectivesText", objectives);
            Wire(view, "board", board);
            Wire(view, "homeButton", home);
            Wire(view, "transition", root.gameObject.AddComponent<FadeTransition>());
            return root.gameObject;
        }

        // ---------------------------------------------------------------- Result

        private static GameObject BuildWinPopup()
        {
            var panel = PopupShell<WinPopup>("WinPopup", new Vector2(1007, 1400), 160, UiSkin.PopupPanel, "LEVEL COMPLETE",
                out var view, out var title, out var close, false);
            Object.DestroyImmediate(close.gameObject);

            var stars = new Image[3];
            for (var i = 0; i < stars.Length; i++)
            {
                var size = i == 1 ? 280 : 230;
                stars[i] = Icon(FromTopLeft(Node($"Star{i + 1}", panel), 503.5f + (i - 1) * 270, i == 1 ? 330 : 360, size, size), UiSkin.StarOn);
            }

            var score = Title(Top(Node("ScoreBox", panel), 520, 90), "ScoreText", "Score 0", 66);
            var reward = Title(Top(Node("RewardBox", panel), 615, 90), "RewardText", "+20 coin", 66);
            reward.color = Hex(0xB8FF8A);

            var next = SkinButton(panel, "NextButton", UiSkin.GreenButton, "Next level", 84, out _, 0.875f);
            At((RectTransform)next.transform, 0.5f, 0, 600, 196, new Vector2(0, 300));
            var home = SkinButton(panel, "HomeButton", UiSkin.BlueButton, "Home", 60, out _, 0.8f);
            At((RectTransform)home.transform, 0.5f, 0, 400, 140, new Vector2(0, 115));

            Wire(view, "titleText", title);
            Wire(view, "scoreText", score);
            Wire(view, "rewardText", reward);
            WireArray(view, "stars", stars);
            Wire(view, "starOnSprite", UiSkin.StarOn.Sprite);
            Wire(view, "starOffSprite", UiSkin.StarOff.Sprite);
            SetColor(view, "starOn", UiSkin.StarOn.Tint);
            SetColor(view, "starOff", UiSkin.StarOff.Tint);
            Wire(view, "nextButton", next);
            Wire(view, "homeButton", home);
            return panel.parent.gameObject;
        }

        private static GameObject BuildLosePopup()
        {
            var panel = PopupShell<LosePopup>("LosePopup", new Vector2(1007, 1180), 160, UiSkin.PopupPanelRed, "OUT OF MOVES",
                out var view, out var title, out var close, false);
            Object.DestroyImmediate(close.gameObject);

            var body = Title(Top(Node("BodyBox", panel), 230, 360, 70, 70), "BodyText", "Keep playing with +5 moves.", 62);
            body.enableWordWrapping = true;

            var primary = SkinButton(panel, "PrimaryButton", UiSkin.GreenButton, "Revive", 76, out var primaryLabel, 0.875f);
            At((RectTransform)primary.transform, 0.5f, 0, 620, 196, new Vector2(0, 300));
            var secondary = SkinButton(panel, "SecondaryButton", UiSkin.BlueButton, "Give up", 60, out var secondaryLabel, 0.8f);
            At((RectTransform)secondary.transform, 0.5f, 0, 400, 140, new Vector2(0, 115));

            Wire(view, "titleText", title);
            Wire(view, "bodyText", body);
            Wire(view, "primaryButton", primary);
            Wire(view, "primaryLabel", primaryLabel);
            Wire(view, "secondaryButton", secondary);
            Wire(view, "secondaryLabel", secondaryLabel);
            return panel.parent.gameObject;
        }

        // ---------------------------------------------------------------- icon set

        private static readonly (string id, string generated)[] ManagedIcons =
        {
            ("coin", UiArt.CoinIcon), ("gem", UiArt.GemIcon), ("life", UiArt.HeartIcon), ("booster_hammer", UiArt.HammerIcon),
            ("hammer_x3", UiArt.HammerIcon), ("title_solver", UiArt.Star), ("title_master", UiArt.Star), ("free_gift", null),
            ("coins_500", null), ("starter_pack", null), ("gold_pack_1", null), ("gold_pack_2", null), ("gold_pack_3", null),
            ("gold_pack_4", null), ("gold_pack_5", null), ("gold_pack_6", null), ("avatar_01", null), ("avatar_02", null),
            ("avatar_03", null), ("avatar_04", null), ("avatar_05", null), ("avatar_06", null), ("avatar_07", null),
            ("avatar_08", null), ("avatar_crown", null)
        };

        /// <summary>
        ///     Points the known content ids at the current skin (reference art, else generated placeholders). An
        ///     entry a designer pointed at a sprite of their own is never overwritten — only entries that are empty
        ///     or still hold generated/reference art are managed here.
        /// </summary>
        private static UIIconSet EnsureIconSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<UIIconSet>(IconSetPath);
            if (!set)
            {
                FrameworkSetup.EnsureFolder(System.IO.Path.GetDirectoryName(IconSetPath)?.Replace('\\', '/'));
                set = ScriptableObject.CreateInstance<UIIconSet>();
                AssetDatabase.CreateAsset(set, IconSetPath);
            }

            var changed = false;
            foreach (var (id, generated) in ManagedIcons)
            {
                var sprite = UiSkin.ContentIcon(id) ?? (generated == null ? null : UiArt.Get(generated));
                var entry = set.entries.Find(e => e.id == id);
                if (entry == null)
                {
                    if (!sprite) continue;
                    set.entries.Add(entry = new UIIconSet.Entry { id = id });
                }
                else if (entry.sprite && !IsManaged(AssetDatabase.GetAssetPath(entry.sprite)))
                {
                    continue;
                }

                if (entry.sprite == sprite) continue;
                entry.sprite = sprite;
                changed = true;
            }

            changed |= set.entries.RemoveAll(e => !e.sprite) > 0;
            if (changed) EditorUtility.SetDirty(set);
            return set;
        }

        private static bool IsManaged(string path) => path.StartsWith(UiArt.Folder) || path.StartsWith(UiSkin.ReferenceRoot);

        // ---------------------------------------------------------------- helpers

        // Fills a missing key, or repairs an entry whose prefab reference broke (prefab deleted and rebuilt).
        private static bool AddEntry(UICatalog catalog, string key, GameObject prefab)
        {
            var entry = catalog.Find(key);
            if (entry != null && entry.prefab) return false;

            if (entry == null) catalog.entries.Add(entry = new UICatalogEntry { key = key, keepInstance = true });
            entry.prefab = prefab.GetComponent<UIView>();
            return true;
        }

        private static bool HasTmpEssentials() => AssetDatabase.FindAssets("t:TMP_Settings", new[] { "Assets" }).Length > 0;

        private static GameObject GetOrCreatePrefab(string path, Func<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) return existing;

            FrameworkSetup.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var root = build();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        internal static void Wire(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field) ??
                           throw new InvalidOperationException($"{target.GetType().Name}.{field} not found.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetBool(Object target, string field, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetColor(Object target, string field, Color value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).colorValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void WireArray(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
