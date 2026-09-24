namespace NinetyNine.Modules.Puzzle.Results
{
    public enum LevelOutcome
    {
        Win,
        Lose,
        Quit
    }

    /// <summary>Final record of a finished session — what progression, rewards and analytics consume.</summary>
    public readonly struct LevelResult
    {
        public LevelResult(string levelId, int levelIndex, LevelOutcome outcome, int score, int stars, int movesUsed,
            int reviveCount)
        {
            LevelId = levelId;
            LevelIndex = levelIndex;
            Outcome = outcome;
            Score = score;
            Stars = stars;
            MovesUsed = movesUsed;
            ReviveCount = reviveCount;
        }

        public string LevelId { get; }
        public int LevelIndex { get; }
        public LevelOutcome Outcome { get; }
        public int Score { get; }
        public int Stars { get; }
        public int MovesUsed { get; }
        public int ReviveCount { get; }
    }
}
