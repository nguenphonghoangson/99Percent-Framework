using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Reward;
using UnityEngine;

namespace NinetyNine.Features.PuzzleGameplay
{
    [CreateAssetMenu(menuName = "NinetyNine/Features/Puzzle Gameplay Config", fileName = "PuzzleGameplayConfig")]
    public sealed class PuzzleGameplayConfig : ScriptableObject
    {
        [Tooltip("Paid on the first clear of a level. Replays pay nothing.")]
        public RewardBundle firstClearReward = new();

        public int reviveMoves = 5;
        public Cost reviveCost = new("coin", 900);
        public int maxRevivesPerLevel = 1;
    }
}
