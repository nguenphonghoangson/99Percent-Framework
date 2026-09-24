using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward.Domain;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.DailyReward.UI
{
    public enum DayCellState
    {
        Claimed,
        Claimable,
        Upcoming
    }

    public readonly struct DayCell
    {
        public DayCell(int dayIndex, RewardBundle reward, DayCellState state)
        {
            DayIndex = dayIndex;
            Reward = reward;
            State = state;
        }

        public int DayIndex { get; }
        public RewardBundle Reward { get; }
        public DayCellState State { get; }
    }

    public interface IDailyRewardView
    {
        void Render(IReadOnlyList<DayCell> days, bool canClaim, bool streakBroken);

        void ShowClaimed(RewardBundle reward);

        void ShowError(string errorCode);
    }

    public sealed class DailyRewardPresenter : IDisposable
    {
        private readonly List<DayCell> _cells = new();
        private readonly IDailyRewardService _service;
        private readonly IDisposable _subscription;
        private readonly IDailyRewardView _view;

        public DailyRewardPresenter(IDailyRewardService service, IEventBus events, IDailyRewardView view)
        {
            _service = service;
            _view = view;
            _subscription = events.Subscribe<DailyRewardClaimedEvent>(e =>
            {
                _view.ShowClaimed(e.Reward);
                Refresh();
            });
            Refresh();
        }

        public void Dispose() => _subscription.Dispose();

        /// <summary>Call on resume too: the UTC day may have rolled over while the popup was open.</summary>
        public void Refresh()
        {
            _cells.Clear();
            var current = _service.ClaimDayIndex;
            var canClaim = _service.CanClaim;
            var claimedToday = _service.Availability == ClaimDenial.AlreadyClaimedToday;
            for (var i = 0; i < _service.CycleLength; i++)
            {
                var state = i < current ? DayCellState.Claimed
                    : i > current ? DayCellState.Upcoming
                    : claimedToday ? DayCellState.Claimed
                    : canClaim ? DayCellState.Claimable : DayCellState.Upcoming;
                _cells.Add(new DayCell(i, _service.GetDayReward(i), state));
            }

            _view.Render(_cells, canClaim, _service.IsStreakBroken);
        }

        public void Claim()
        {
            var result = _service.TryClaim();
            if (!result.IsSuccess) _view.ShowError(result.Error);
        }
    }
}
