using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Features.Profile;
using NinetyNine.Features.Shop;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Home
{
    /// <summary>
    ///     Persistent chrome on the UI root's Hud layer: top bar (avatar, lives, coins, settings) and the bottom
    ///     tab bar (Shop / Start / Trophy). Shown only on the tab screens, so tab switches never flicker it; popups
    ///     still cover it. Entry points whose UI is not in the catalog (settings, leaderboard) are shown disabled.
    /// </summary>
    public sealed class MainHudView : MonoBehaviour
    {
        public const string SettingsKey = "settings";
        public const string LeaderboardKey = "leaderboard";
        public const string LivesCurrency = "life";
        public const string CoinCurrency = "coin";

        [Header("Top bar")]
        [SerializeField] private Button avatarButton;
        [SerializeField] private Image avatarIcon;
        [SerializeField] private TMP_Text avatarFallback;
        [SerializeField] private GameObject livesGroup;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text livesStatusText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private Button addCoinsButton;
        [SerializeField] private Button settingsButton;

        [Header("Chrome Structure")]
        [SerializeField] private GameObject topBar;
        [SerializeField] private GameObject bottomBar;

        [Header("Bottom nav")]
        [SerializeField] private Button shopTab;
        [SerializeField] private Button startTab;
        [SerializeField] private Button trophyTab;
        [SerializeField] private GameObject shopTabSelected;
        [SerializeField] private GameObject startTabSelected;
        [SerializeField] private GameObject trophyTabSelected;

        [SerializeField] private UIIconSet icons;

        private readonly List<IDisposable> _subscriptions = new();
        private IEconomyService _economy;
        private INavigator _navigator;
        private IProfileService _profile;

        /// <summary>Screens the HUD shows on — the bottom-nav tabs.</summary>
        public static readonly string[] TabScreens = { HomeScreenView.ScreenId, ShopFeature.Id, LeaderboardKey };

        public void Bind(IServiceResolver services, INavigator navigator)
        {
            _navigator = navigator;
            _economy = services.Require<IEconomyService>();
            services.TryGet(out _profile);
            services.TryGet<IUIFactory>(out var factory);
            bool Has(string key) => factory != null && factory.Has(key);

            if (avatarButton != null) {
                avatarButton.interactable = Has(ProfileFeature.Id) && services.Has<IProfileFeatureService>();
                avatarButton.onClick.AddListener(() => Forget(_navigator.ShowPopup(ProfileFeature.Id)));
            }
            if (settingsButton != null) {
                settingsButton.interactable = Has(SettingsKey);
                settingsButton.onClick.AddListener(() => Forget(_navigator.ShowPopup(SettingsKey)));
            }

            if (shopTab != null) {
                shopTab.interactable = Has(ShopFeature.Id) && services.Has<IShopService>();
                shopTab.onClick.AddListener(() => OpenTab(ShopFeature.Id));
            }
            if (trophyTab != null) {
                trophyTab.interactable = Has(LeaderboardKey);
                trophyTab.onClick.AddListener(() => OpenTab(LeaderboardKey));
            }
            
            if (addCoinsButton != null) addCoinsButton.onClick.AddListener(() => OpenTab(ShopFeature.Id));
            if (startTab != null) startTab.onClick.AddListener(() => OpenTab(HomeScreenView.ScreenId));

            var events = services.Require<IEventBus>();
            _subscriptions.Add(events.Subscribe<CurrencyChangedEvent>(_ => RefreshCurrencies()));
            _subscriptions.Add(events.Subscribe<ProfileChangedEvent>(_ => RefreshAvatar()));
            _subscriptions.Add(events.Subscribe<UIViewOpenedEvent>(_ => RefreshTabs()));
            _subscriptions.Add(events.Subscribe<UIViewClosedEvent>(_ => RefreshTabs()));

            RefreshCurrencies();
            RefreshAvatar();
            RefreshTabs();
        }

        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
        }

        // Tabs are screens on the navigator stack with Home as the root: another tab is pushed on top of Home,
        // Start returns to the root. Back from a tab therefore always lands on Home.
        private void OpenTab(string key)
        {
            var current = _navigator.CurrentScreen ? _navigator.CurrentScreen.Key : null;
            if (current == key || _navigator.IsBusy) return;

            if (key == HomeScreenView.ScreenId) Forget(_navigator.PopToRoot());
            else if (current == HomeScreenView.ScreenId) Forget(_navigator.PushScreen(key));
            else Forget(_navigator.ReplaceScreen(key));
        }

        private void RefreshTabs()
        {
            var current = _navigator.CurrentScreen ? _navigator.CurrentScreen.Key : null;
            bool isTabScreen = Array.IndexOf(TabScreens, current) >= 0;
            
            if (bottomBar != null) bottomBar.SetActive(isTabScreen);
            // Optionally, if topBar needs to be hidden in some screens, you can add logic here.
            // For now, topBar remains active as long as the HUD is alive.
            
            if (shopTabSelected != null) shopTabSelected.SetActive(current == ShopFeature.Id);
            if (startTabSelected != null) startTabSelected.SetActive(current == HomeScreenView.ScreenId);
            if (trophyTabSelected != null) trophyTabSelected.SetActive(current == LeaderboardKey);
        }

        private void RefreshCurrencies()
        {
            if (coinText != null) coinText.text = _economy.GetBalance(CoinCurrency).ToString("N0");

            var hasLives = _economy.IsKnown(LivesCurrency);
            if (livesGroup != null) livesGroup.SetActive(hasLives);
            if (!hasLives) return;

            var lives = _economy.GetBalance(LivesCurrency);
            var cap = _economy.GetCap(LivesCurrency);
            if (livesText != null) livesText.text = lives.ToString();
            if (livesStatusText != null) livesStatusText.text = cap > 0 && lives >= cap ? "MAX" : string.Empty;
        }

        private void RefreshAvatar()
        {
            var avatarId = _profile?.AvatarId;
            UIIconSet.Show(icons, avatarIcon, avatarFallback, avatarId ?? string.Empty);
        }

        private void Forget(System.Threading.Tasks.Task task) => ForgetAsync(task, this);

        private static async void ForgetAsync(System.Threading.Tasks.Task task, UnityEngine.Object context)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Debug.LogException(e, context);
            }
        }
    }
}
