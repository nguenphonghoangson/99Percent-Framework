namespace NinetyNine.Features.DailyReward.Domain
{
    public enum ClaimDenial
    {
        None,
        AlreadyClaimedToday,

        /// <summary>Device clock is before the last claim — rolled back, or a bad sync.</summary>
        ClockBehind
    }

    public readonly struct StreakState
    {
        public const long NeverClaimed = -1;

        public StreakState(int streak, long lastClaimDay)
        {
            Streak = streak;
            LastClaimDay = lastClaimDay;
        }

        public int Streak { get; }
        public long LastClaimDay { get; }
    }

    /// <summary>
    ///     Login-streak rules on whole UTC days, pure C# (same rules as percas.module.dailyreward). Integer day
    ///     indices mean time zones and DST cannot shift a boundary.
    /// </summary>
    public static class StreakRules
    {
        public static ClaimDenial Evaluate(StreakState current, long today)
        {
            if (current.LastClaimDay == StreakState.NeverClaimed) return ClaimDenial.None;
            if (today < current.LastClaimDay) return ClaimDenial.ClockBehind;
            if (today == current.LastClaimDay) return ClaimDenial.AlreadyClaimedToday;
            return ClaimDenial.None;
        }

        /// <summary>
        ///     State after claiming on <paramref name="today" />; unchanged when the claim is refused. A one-day
        ///     gap continues the streak; a longer gap resets it or carries on per <paramref name="resetOnMiss" />.
        /// </summary>
        public static StreakState Claim(StreakState current, long today, bool resetOnMiss)
        {
            if (Evaluate(current, today) != ClaimDenial.None) return current;

            var claimedBefore = current.LastClaimDay != StreakState.NeverClaimed;
            var continued = claimedBefore && today - current.LastClaimDay == 1;
            var missed = claimedBefore && today - current.LastClaimDay > 1;

            var streak = continued || (missed && !resetOnMiss) ? current.Streak + 1 : 1;
            return new StreakState(streak, today);
        }

        /// <summary>0-based cycle index for claim number <paramref name="streak" />. Wraps: day 8 of a 7-day cycle pays day 1.</summary>
        public static int RewardIndex(int streak, int cycleLength)
        {
            if (cycleLength <= 0) return 0;
            var claimNumber = streak < 1 ? 1 : streak;
            return (claimNumber - 1) % cycleLength;
        }

        public static bool IsStreakBroken(StreakState current, long today) =>
            current.LastClaimDay != StreakState.NeverClaimed && today - current.LastClaimDay > 1;
    }
}
