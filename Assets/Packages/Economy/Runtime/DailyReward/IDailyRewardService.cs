using NinetyNine.Core;
using NinetyNine.Features.DailyReward.Domain;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.DailyReward
{
    /// <summary>
    ///     Login streak + claim. Decides whether today can be claimed and what it pays; does not decide when to
    ///     show the popup — that is the Home screen's popup queue.
    /// </summary>
    public interface IDailyRewardService
    {
        bool IsUnlocked { get; }

        int Streak { get; }

        bool IsStreakBroken { get; }

        ClaimDenial Availability { get; }

        bool CanClaim { get; }

        /// <summary>
        ///     1-based claim the calendar stands on: the one <see cref="TryClaim" /> would make now, or today's
        ///     once made. Computed by the same rule as the claim, so view and claim never disagree.
        /// </summary>
        int ClaimNumber { get; }

        /// <summary>0-based cell of <see cref="ClaimNumber" /> in the cycle.</summary>
        int ClaimDayIndex { get; }

        int CycleLength { get; }

        RewardBundle GetDayReward(int dayIndex);

        Result TryClaim();
    }

    public static class DailyRewardErrors
    {
        public const string AlreadyClaimed = "daily_reward.already_claimed";
        public const string ClockBehind = "daily_reward.clock_behind";
        public const string NotConfigured = "daily_reward.not_configured";
    }

    public readonly struct DailyRewardClaimedEvent
    {
        public DailyRewardClaimedEvent(int claimNumber, int dayIndex, RewardBundle reward)
        {
            ClaimNumber = claimNumber;
            DayIndex = dayIndex;
            Reward = reward;
        }

        public int ClaimNumber { get; }
        public int DayIndex { get; }
        public RewardBundle Reward { get; }
    }
}
