using System;
using System.Collections.Generic;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Objectives;

namespace NinetyNine.Modules.Puzzle.Levels
{
    /// <summary>One level as design data. Tiles are row-major from the bottom row (see <see cref="Board" />).</summary>
    [Serializable]
    public sealed class LevelDefinition
    {
        public string id;
        public int width = 6;
        public int height = 8;
        public int[] tiles = Array.Empty<int>();

        /// <summary>0 = unlimited.</summary>
        public int moveLimit = 20;

        public List<ObjectiveDefinition> objectives = new();

        /// <summary>Ascending score needed for each star.</summary>
        public int[] starScores = Array.Empty<int>();

        public Board CreateBoard() => new(width, height, tiles);

        /// <summary>Null when valid; otherwise the first problem, for editor validation and CI checks.</summary>
        public string Validate()
        {
            if (string.IsNullOrEmpty(id)) return "Missing id.";
            if (width <= 0 || height <= 0) return $"{id}: size must be positive.";
            if (tiles == null || tiles.Length != width * height) return $"{id}: expected {width * height} tiles.";
            if (objectives == null || objectives.Count == 0) return $"{id}: needs at least one objective.";
            if (moveLimit < 0) return $"{id}: move limit cannot be negative.";
            return null;
        }
    }
}
