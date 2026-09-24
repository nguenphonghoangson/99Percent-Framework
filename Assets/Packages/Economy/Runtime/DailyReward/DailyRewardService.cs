using System;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward.Domain;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Features.DailyReward
{
    [Serializable]
    internal sealed class DailyRewardState
    {
        public int streak;
        public long lastClaimDay = StreakState.NeverClaimed;

        public StreakState ToStreak() => new(streak, lastClaimDay);

        public void Apply(StreakState state)
        {
            streak = state.Streak;
            lastClaimDay = state.LastClaimDay;
        }
    }

    internal sealed class DailyRewardService : IDailyRewardService, IInitializable
    {
        private const string SaveKey = "feature.daily_reward";
        private const string Source = "daily_reward";

        private readonly DailyRewardConfig _config;
        private readonly IEventBus _events;
        private readonly IRewardService _rewards;
        private readonly ISaveService _save;
        private readonly ITimeService _time;
        private readonly UnlockEvaluator _unlocks;
        private DailyRewardState _state = new();

        public DailyRewardService(DailyRewardConfig config, IRewardService rewards, ITimeService time,
            UnlockEvaluator unlocks, ISaveService save, IEventBus events)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _rewards = rewards;
            _time = time;
            _unlocks = unlocks;
            _save = save;
            _events = events;
        }

        public void Initialize() => _state = _save.Load<DailyRewardState>(SaveKey);

        public bool IsUnlocked => _unlocks.IsMet(_config.unlock);

        public int Streak => _state.streak;

        public bool IsStreakBroken => StreakRules.IsStreakBroken(_state.ToStreak(), _time.UtcDay);

        public ClaimDenial Availability => StreakRules.Evaluate(_state.ToStreak(), _time.UtcDay);

        public bool CanClaim => IsUnlocked && CycleLength > 0 && Availability == ClaimDenial.None;

        public int ClaimNumber =>
            StreakRules.Claim(_state.ToStreak(), _time.UtcDay, _config.resetStreakOnMiss).Streak;

        public int ClaimDayIndex => StreakRules.RewardIndex(ClaimNumber, CycleLength);

        public int CycleLength => _config.days.Count;

        public RewardBundle GetDayReward(int dayIndex) => _config.days[dayIndex];

        public Result TryClaim()
        {
            if (!IsUnlocked) return Result.Fail(CommonErrors.FeatureLocked);
            if (CycleLength == 0) return Result.Fail(DailyRewardErrors.NotConfigured);

            switch (Availability)
            {
                case ClaimDenial.AlreadyClaimedToday: return Result.Fail(DailyRewardErrors.AlreadyClaimed);
                case ClaimDenial.ClockBehind: return Result.Fail(DailyRewardErrors.ClockBehind);
            }

            var next = StreakRules.Claim(_state.ToStreak(), _time.UtcDay, _config.resetStreakOnMiss);
            var dayIndex = StreakRules.RewardIndex(next.Streak, CycleLength);
            var reward = _config.days[dayIndex];

            // A misconfigured day must not burn the player's claim.
            var validation = _rewards.Validate(reward);
            if (!validation.IsSuccess) return validation;

            // Persist before granting: a crash in between loses one reward rather than allowing a double claim.
            _state.Apply(next);
            _save.Save(SaveKey, _state);
            _rewards.Grant(reward, Source);

            _events.Publish(new DailyRewardClaimedEvent(next.Streak, dayIndex, reward));
            return Result.Success;
        }
    }
}
