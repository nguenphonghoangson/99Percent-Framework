namespace NinetyNine.Modules.Progression
{
    /// <summary>Where the player is in the level funnel. Knows indices only, not level content.</summary>
    public interface IProgressionService
    {
        /// <summary>0-based index of the next level to play; also the number of levels cleared.</summary>
        int CurrentLevelIndex { get; }

        /// <summary>1-based, for display and for "unlocks at level N" rules.</summary>
        int CurrentLevelNumber { get; }

        /// <summary>
        ///     Advances when <paramref name="levelIndex" /> is the current level. Replaying an earlier level
        ///     is not an advance and returns false.
        /// </summary>
        bool CompleteLevel(int levelIndex);
    }

    public readonly struct LevelProgressedEvent
    {
        public LevelProgressedEvent(int completedIndex, int currentIndex)
        {
            CompletedIndex = completedIndex;
            CurrentIndex = currentIndex;
        }

        public int CompletedIndex { get; }
        public int CurrentIndex { get; }
    }
}
