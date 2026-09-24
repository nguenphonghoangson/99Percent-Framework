using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Features.Profile;
using NinetyNine.Modules.Profile;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Home
{
    public sealed class HomeScreenView : UIScreen
    {
        public const string ScreenId = "home";

        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button shopTab;
        [SerializeField] private Button trophyTab;
        [SerializeField] private MainHudView hudPrefab;
        [SerializeField] private Button avatarButton;
        [SerializeField] private Image avatarIcon;
        [SerializeField] private TMP_Text avatarFallback;
        [SerializeField] private UIIconSet icons;

        private MainHudView _hud;
        private IProfileService _profile;
        private readonly List<System.IDisposable> _subscriptions = new();

        protected override void OnCreated()
        {
            var services = ServiceLocator.Current;
            services.TryGet(out _profile);
            services.TryGet<IUIFactory>(out var factory);
            bool Has(string key) => factory != null && factory.Has(key);
            
            if (playButton != null)
            {
                playButton.onClick.AddListener(() => Forget(Navigator.PushScreen(MapScreenView.ScreenId)));
            }

            if (avatarButton != null) {
                avatarButton.interactable = Has(ProfileFeature.Id) && services.Has<IProfileFeatureService>();
                avatarButton.onClick.AddListener(() => Forget(Navigator.ShowPopup(ProfileFeature.Id)));
            }

            if (settingsButton != null) {
                settingsButton.interactable = Has("settings");
                settingsButton.onClick.AddListener(() => Forget(Navigator.ShowPopup("settings")));
            }

            if (shopTab != null) {
                shopTab.interactable = Has("shop");
                shopTab.onClick.AddListener(() => Forget(Navigator.PushScreen("shop")));
            }

            if (trophyTab != null) {
                trophyTab.interactable = Has("leaderboard");
                trophyTab.onClick.AddListener(() => Forget(Navigator.PushScreen("leaderboard")));
            }

            var events = services.Require<IEventBus>();
            _subscriptions.Add(events.Subscribe<ProfileChangedEvent>(_ => RefreshAvatar()));

            RefreshAvatar();

            if (hudPrefab != null) {
                _hud = Instantiate(hudPrefab, services.Require<IUILayers>().HudLayer, false);
                _hud.Bind(services, Navigator);
            }
        }
        
        private void RefreshAvatar()
        {
            var avatarId = _profile?.AvatarId;
            UIIconSet.Show(icons, avatarIcon, avatarFallback, avatarId ?? string.Empty);
        }
        
        private void OnDestroy()
        {
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
            if (_hud) Destroy(_hud.gameObject);
        }
    }
}
