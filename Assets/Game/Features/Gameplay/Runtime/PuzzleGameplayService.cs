using System;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Reward;

namespace NinetyNine.Features.PuzzleGameplay
{
    internal sealed class PuzzleGameplayService : IPuzzleGameplayService, IDisposable
    {
        private const string WinSource = "level_first_clear";
        private const string ReviveSource = "level_revive";

        private readonly PuzzleGameplayConfig _config;
        private readonly IEconomyService _economy;
        private readonly IEventBus _events;
        private readonly ILevelService _levels;
        private readonly IProgressionService _progression;
        private readonly IRewardService _rewards;
        private readonly IPuzzleSessionFactory _sessions;

        public PuzzleGameplayService(PuzzleGameplayConfig config, ILevelService levels, IPuzzleSessionFactory sessions,
            IProgressionService progression, IEconomyService economy, IRewardService rewards, IEventBus events)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _levels = levels;
            _sessions = sessions;
            _progression = progression;
            _economy = economy;
            _rewards = rewards;
            _events = events;
        }

        public PuzzleSession ActiveSession { get; private set; }

        public int CurrentLevelNumber => _progression.CurrentLevelNumber;

        public Cost ReviveCost => _config.reviveCost;

        public int ReviveMoves => _config.reviveMoves;

        public RewardBundle FirstClearReward => _config.firstClearReward;

        public bool ReviveAvailable => ActiveSession is { State: PuzzleSessionState.OutOfMoves } &&
                                       ActiveSession.ReviveCount < _config.maxRevivesPerLevel;

        public bool CanRevive => ReviveAvailable && _economy.CanAfford(_config.reviveCost);

        public PuzzleSession StartCurrentLevel() => StartLevel(_progression.CurrentLevelIndex);

        public PuzzleSession StartLevel(int progressIndex)
        {
            if (progressIndex < 0 || progressIndex > _progression.CurrentLevelIndex)
                throw new ArgumentOutOfRangeException(nameof(progressIndex), "Level not reached yet.");

            ActiveSession?.Quit();

            var level = _levels.GetByProgressIndex(progressIndex);
            ActiveSession = _sessions.Create(level, progressIndex);
            ActiveSession.Ended += OnSessionEnded;
            _events.Publish(new PuzzleLevelStartedEvent(progressIndex, level.id));
            return ActiveSession;
        }

        public Result TryRevive()
        {
            if (ActiveSession == null) return Result.Fail(PuzzleGameplayErrors.NoActiveSession);
            if (ActiveSession.State != PuzzleSessionState.OutOfMoves) return Result.Fail(PuzzleGameplayErrors.NotOutOfMoves);
            if (ActiveSession.ReviveCount >= _config.maxRevivesPerLevel)
                return Result.Fail(PuzzleGameplayErrors.ReviveLimitReached);

            var spend = _economy.TrySpend(_config.reviveCost, ReviveSource);
            if (!spend.IsSuccess) return spend;

            ActiveSession.Revive(_config.reviveMoves);
            return Result.Success;
        }

        public void Dispose()
        {
            if (ActiveSession != null) ActiveSession.Ended -= OnSessionEnded;
        }

        private void OnSessionEnded(LevelResult result)
        {
            ActiveSession.Ended -= OnSessionEnded;

            var firstClear = result.Outcome == LevelOutcome.Win && _progression.CompleteLevel(result.LevelIndex);
            if (firstClear && !_config.firstClearReward.IsEmpty) _rewards.Grant(_config.firstClearReward, WinSource);

            _events.Publish(new PuzzleLevelFinishedEvent(result, firstClear));
        }
    }
}
