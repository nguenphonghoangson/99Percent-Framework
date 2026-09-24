using System;
using NinetyNine.Core;

namespace NinetyNine.Modules.Puzzle.Levels
{
    /// <summary>Resolves a progression index to level content, looping once the content runs out.</summary>
    public interface ILevelService
    {
        int ContentLevelCount { get; }

        LevelDefinition GetByProgressIndex(int progressIndex);
    }

    public static class LevelIndexRules
    {
        /// <summary>
        ///     Progress index → content index. Past the end, cycles through [loopStart, count). A loop start
        ///     outside the list falls back to 0.
        /// </summary>
        public static int ResolveContentIndex(int progressIndex, int levelCount, int loopStart)
        {
            if (levelCount <= 0) throw new InvalidOperationException("No levels configured.");
            if (progressIndex < 0) throw new ArgumentOutOfRangeException(nameof(progressIndex));
            if (progressIndex < levelCount) return progressIndex;

            if (loopStart < 0 || loopStart >= levelCount) loopStart = 0;
            var loopLength = levelCount - loopStart;
            return loopStart + (progressIndex - levelCount) % loopLength;
        }
    }

    internal sealed class LevelService : ILevelService
    {
        private readonly LevelDatabase _database;

        public LevelService(LevelDatabase database) => _database = database;

        public int ContentLevelCount => _database.levels.Count;

        public LevelDefinition GetByProgressIndex(int progressIndex) =>
            _database.levels[LevelIndexRules.ResolveContentIndex(progressIndex, _database.levels.Count,
                _database.loopStartIndex)];
    }

    /// <summary>Provides <see cref="ILevelService" />. No dependencies.</summary>
    public sealed class LevelModule : IModule
    {
        private readonly LevelDatabase _database;

        public LevelModule(LevelDatabase database) =>
            _database = database ? database : throw new ArgumentNullException(nameof(database));

        public string Name => "Puzzle.Level";

        public void Install(IServiceRegistry registry) => registry.Register<ILevelService>(new LevelService(_database));
    }
}
