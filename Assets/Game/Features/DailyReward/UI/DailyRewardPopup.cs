using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward.Domain;
using NinetyNine.Modules.Reward;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.DailyReward.UI
{
    /// <summary>
    ///     uGUI implementation of <see cref="IDailyRewardView" />: calendar grid + claim button. Opened by the
    ///     navigator under key <see cref="DailyRewardFeature.Id" />.
    /// </summary>
    public sealed class DailyRewardPopup : UIPopup, IDailyRewardView
    {
        [SerializeField] private RectTransform grid;
        [SerializeField] private DayCellView cellPrefab;
        [SerializeField] private TMP_Text streakText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private Button closeButton;

        private readonly List<DayCellView> _cells = new();
        private DailyRewardPresenter _presenter;
        private IDailyRewardService _service;

        private void Awake()
        {
            claimButton.onClick.AddListener(() => _presenter?.Claim());
            closeButton.onClick.AddListener(() => Close());
        }

        private void OnEnable()
        {
            var services = ServiceLocator.Current;
            if (services == null || !services.TryGet(out _service))
            {
                Debug.LogWarning("[DailyRewardPopup] Daily reward service unavailable — start from the Intro scene.", this);
                return;
            }

            messageText.text = string.Empty;
            _presenter = new DailyRewardPresenter(_service, services.Require<IEventBus>(), this);
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        // The UTC day can roll over while the app sits in the background with the popup open.
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) _presenter?.Refresh();
        }

        public void Render(IReadOnlyList<DayCell> days, bool canClaim, bool streakBroken)
        {
            while (_cells.Count < days.Count) _cells.Add(Instantiate(cellPrefab, grid));
            for (var i = 0; i < _cells.Count; i++)
            {
                var visible = i < days.Count;
                _cells[i].gameObject.SetActive(visible);
                if (visible) _cells[i].Bind(days[i]);
            }

            claimButton.interactable = canClaim;
            claimLabel.text = canClaim ? "Claim"
                : !_service.IsUnlocked ? "Locked"
                : _service.Availability == ClaimDenial.ClockBehind ? "Check device time"
                : "Come back tomorrow";
            streakText.text = streakBroken ? "Streak lost - back to day 1" : $"Streak: {_service.Streak} day(s)";
        }

        public void ShowClaimed(RewardBundle reward) => messageText.text = "+" + reward;

        public void ShowError(string errorCode) => messageText.text = errorCode switch
        {
            DailyRewardErrors.AlreadyClaimed => "Already claimed today.",
            DailyRewardErrors.ClockBehind => "Device time looks wrong.",
            CommonErrors.FeatureLocked => "Not unlocked yet.",
            _ => $"Could not claim ({errorCode})."
        };
    }
}
