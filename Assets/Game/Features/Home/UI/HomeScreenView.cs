using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Game.Flow;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Unlock;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Home
{
    /// <summary>
    ///     Home hub, the root screen of the Home scene and the "Start" tab: level path, upcoming unlocks, Play. The
    ///     one feature allowed to know other features, because its whole job is to route to them — it reads their
    ///     public services and opens them by feature id through the navigator, never touching their view types.
    ///     Play loads the Gameplay scene. It also creates the persistent <see cref="MainHudView" /> on the Hud layer.
    /// </summary>
    public sealed class HomeScreenView : UIScreen
    {
        public const string ScreenId = "home";

        [Header("Level path (bottom = current level)")]
        [SerializeField] private TMP_Text[] levelNodes = Array.Empty<TMP_Text>();
        [SerializeField] private MilestoneView[] milestones = Array.Empty<MilestoneView>();
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text playButtonLabel;

        [Header("Side entry points")]
        [SerializeField] private Button dailyButton;
        [SerializeField] private GameObject dailyBadge;

        [Header("Chrome")]
        [SerializeField] private MainHudView hudPrefab;
        [SerializeField] private UIIconSet icons;

        [Header("Dev cheats (editor / development builds only)")]
        [SerializeField] private GameObject devPanel;
        [SerializeField] private Button devCompleteLevelButton;
        [SerializeField] private Button devAddCoinsButton;

        private readonly List<IDisposable> _subscriptions = new();
        private IDailyRewardService _daily;
        private IEconomyService _economy;
        private MainHudView _hud;
        private IProfileFeatureService _profile;
        private IProgressionService _progression;

        protected override void OnCreated()
        {
            var services = ServiceLocator.Current ?? throw new InvalidOperationException("Services not booted.");
            _economy = services.Require<IEconomyService>();
            _progression = services.Require<IProgressionService>();
            services.TryGet(out _daily);
            services.TryGet(out _profile);

            // A feature switched off by toggles is not installed; its entry point simply disappears.
            playButton.gameObject.SetActive(services.Has<IPuzzleGameplayService>());
            dailyButton.gameObject.SetActive(_daily != null);
            var scenes = services.Require<ISceneLoader>();
            playButton.onClick.AddListener(() => Forget(scenes.LoadAsync(GameScenes.Gameplay)));
            dailyButton.onClick.AddListener(() => Forget(Navigator.ShowPopup(DailyRewardFeature.Id)));

            var events = services.Require<IEventBus>();
            _subscriptions.Add(events.Subscribe<LevelProgressedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<DailyRewardClaimedEvent>(_ => Refresh()));

            _hud = Instantiate(hudPrefab, services.Require<IUILayers>().HudLayer, false);
            _hud.Bind(services, Navigator);

            SetUpDevCheats();
        }

        protected override void OnOpening(object args) => Refresh();

        protected override void OnRevealed() => Refresh();

        protected override void OnOpened()
        {
            // Queued behind this open by the navigator, so it lands on top once Home is fully shown.
            if (_daily is { CanClaim: true }) Forget(Navigator.ShowPopup(DailyRewardFeature.Id));
        }

        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
            if (_hud) Destroy(_hud.gameObject);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _progression != null) Refresh();
        }

        private void Refresh()
        {
            var level = _progression.CurrentLevelNumber;
            for (var i = 0; i < levelNodes.Length; i++) levelNodes[i].text = (level + i).ToString();
            playButtonLabel.text = "Play";
            dailyBadge.SetActive(_daily is { CanClaim: true });
            RefreshMilestones(level);
        }

        // Upcoming level-gated unlocks the design data already knows about (avatars, titles), nearest first.
        private void RefreshMilestones(int currentLevel)
        {
            var upcoming = new List<(int level, string id)>();
            if (_profile != null)
            {
                Collect(_profile.GetAvatars(), currentLevel, upcoming);
                Collect(_profile.GetTitles(), currentLevel, upcoming);
            }

            upcoming.Sort((a, b) => a.level.CompareTo(b.level));
            for (var i = 0; i < milestones.Length; i++)
                if (i < upcoming.Count) milestones[i].Bind(upcoming[i].id, upcoming[i].level, icons);
                else milestones[i].Hide();
        }

        private static void Collect(IReadOnlyList<ProfileOptionView> options, int currentLevel, List<(int, string)> into)
        {
            foreach (var option in options)
                if (option.Unlock.kind == UnlockKind.ReachLevel && option.Unlock.levelNumber > currentLevel)
                    into.Add((option.Unlock.levelNumber, option.Id));
        }

        private void SetUpDevCheats()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            devPanel.SetActive(true);
            devCompleteLevelButton.onClick.AddListener(() => _progression.CompleteLevel(_progression.CurrentLevelIndex));
            devAddCoinsButton.onClick.AddListener(() => _economy.Add("coin", 1000, "dev_cheat"));
#else
            devPanel.SetActive(false);
#endif
        }
    }
}
