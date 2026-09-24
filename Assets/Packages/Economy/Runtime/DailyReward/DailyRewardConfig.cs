using System.Collections.Generic;
using NinetyNine.Modules.Reward;
using NinetyNine.Modules.Unlock;
using UnityEngine;

namespace NinetyNine.Features.DailyReward
{
    [CreateAssetMenu(menuName = "NinetyNine/Features/Daily Reward Config", fileName = "DailyRewardConfig")]
    public sealed class DailyRewardConfig : ScriptableObject
    {
        [Tooltip("One entry per day of the cycle. The cycle repeats after the last day.")]
        public List<RewardBundle> days = new();

        [Tooltip("Missing a day restarts at day 1. Off = the streak just continues.")]
        public bool resetStreakOnMiss = true;

        public UnlockCondition unlock;
    }
}
