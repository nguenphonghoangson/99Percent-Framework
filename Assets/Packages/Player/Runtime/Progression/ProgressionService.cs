using System;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Progression
{
    [Serializable]
    internal sealed class ProgressionState
    {
        public int currentLevelIndex;
    }

    internal sealed class ProgressionService : IProgressionService, IInitializable
    {
        private const string SaveKey = "module.progression";

        private readonly IEventBus _events;
        private readonly ISaveService _save;
        private ProgressionState _state = new();

        public ProgressionService(ISaveService save, IEventBus events)
        {
            _save = save;
            _events = events;
        }

        public void Initialize() => _state = _save.Load<ProgressionState>(SaveKey);

        public int CurrentLevelIndex => _state.currentLevelIndex;

        public int CurrentLevelNumber => _state.currentLevelIndex + 1;

        public bool CompleteLevel(int levelIndex)
        {
            if (levelIndex != _state.currentLevelIndex) return false;

            _state.currentLevelIndex++;
            _save.Save(SaveKey, _state);
            _events.Publish(new LevelProgressedEvent(levelIndex, _state.currentLevelIndex));
            return true;
        }
    }
}
