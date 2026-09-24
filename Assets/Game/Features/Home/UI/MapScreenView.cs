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
    public sealed class MapScreenView : UIScreen
    {
        public const string ScreenId = "map";

        [Header("Level path (bottom = current level)")]
        [SerializeField] private TMP_Text[] levelNodes = Array.Empty<TMP_Text>();
        [SerializeField] private MilestoneView[] milestones = Array.Empty<MilestoneView>();
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text playButtonLabel;

        [Header("Side entry points")]
        [SerializeField] private Button dailyButton;
        [SerializeField] private GameObject dailyBadge;

        [Header("Chrome")]
        [SerializeField] private UIIconSet icons;

        [Header("Dev cheats (editor / development builds only)")]
        [SerializeField] private GameObject devPanel;
        [SerializeField] private Button devCompleteLevelButton;
        [SerializeField] private Button devAddCoinsButton;

        private readonly List<IDisposable> _subscriptions = new();
        private IDailyRewardService _daily;
        private IEconomyService _economy;
        private IProfileFeatureService _profile;
        private IProgressionService _progression;

        protected override void OnCreated()
        {
            var services = ServiceLocator.Current ?? throw new InvalidOperationException("Services not booted.");
            _economy = services.Require<IEconomyService>();
            _progression = services.Require<IProgressionService>();
            services.TryGet(out _daily);
            services.TryGet(out _profile);

            if (playButton != null) {
                playButton.gameObject.SetActive(services.Has<IPuzzleGameplayService>());
                var scenes = services.Require<ISceneLoader>();
                playButton.onClick.AddListener(() => Forget(scenes.LoadAsync(GameScenes.Gameplay)));
            }

            if (dailyButton != null) {
                dailyButton.gameObject.SetActive(_daily != null);
                dailyButton.onClick.AddListener(() => Forget(Navigator.ShowPopup(DailyRewardFeature.Id)));
            }

            // Close button (Back to Lobby)
            var closeBtn = transform.Find("Button_Back")?.GetComponent<Button>() ?? transform.Find("Close")?.GetComponent<Button>();
            if (closeBtn != null) {
                closeBtn.onClick.AddListener(() => Forget(Navigator.PopScreen()));
            }

            var events = services.Require<IEventBus>();
            _subscriptions.Add(events.Subscribe<LevelProgressedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<DailyRewardClaimedEvent>(_ => Refresh()));

            SetUpDevCheats();
        }

        protected override void OnOpening(object args) => Refresh();

        protected override void OnRevealed() => Refresh();

        protected override void OnOpened()
        {
            if (_daily is { CanClaim: true }) Forget(Navigator.ShowPopup(DailyRewardFeature.Id));
        }

        private void OnDestroy()
        {
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && _progression != null) Refresh();
        }

        private void Refresh()
        {
            var level = _progression.CurrentLevelNumber;
            for (var i = 0; i < levelNodes.Length; i++) {
                if (levelNodes[i] != null) levelNodes[i].text = (level + i).ToString();
            }
            if (playButtonLabel != null) playButtonLabel.text = "Play";
            if (dailyBadge != null) dailyBadge.SetActive(_daily is { CanClaim: true });
            RefreshMilestones(level);
        }

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
            if (devPanel != null) devPanel.SetActive(true);
            if (devCompleteLevelButton != null) devCompleteLevelButton.onClick.AddListener(() => _progression.CompleteLevel(_progression.CurrentLevelIndex));
            if (devAddCoinsButton != null) devAddCoinsButton.onClick.AddListener(() => _economy.Add("coin", 1000, "dev_cheat"));
#else
            if (devPanel != null) devPanel.SetActive(false);
#endif
        }
    }
}
