using System.Collections.Generic;
using UnityEngine;

namespace NinetyNine.Modules.Puzzle.Levels
{
    /// <summary>
    ///     Ordered level content. The order here is the funnel order — a funnel reorder edits this list, not
    ///     the levels themselves.
    /// </summary>
    [CreateAssetMenu(menuName = "NinetyNine/Modules/Level Database", fileName = "LevelDatabase")]
    public sealed class LevelDatabase : ScriptableObject
    {
        public List<LevelDefinition> levels = new();

        [Tooltip("Once the player passes the last level, play continues looping from this index.")]
        public int loopStartIndex;

        private void OnValidate()
        {
            var ids = new HashSet<string>();
            foreach (var level in levels)
            {
                var error = level.Validate();
                if (error != null) Debug.LogWarning($"[LevelDatabase] {error}", this);
                else if (!ids.Add(level.id)) Debug.LogWarning($"[LevelDatabase] Duplicate level id {level.id}.", this);
            }
        }
    }
}
