using NinetyNine.Modules.Puzzle.Boards;
using UnityEngine;

namespace NinetyNine.Features.PuzzleGameplay.UI
{
    /// <summary>Placeholder look for tile ids until the game has real tile art.</summary>
    public static class TilePalette
    {
        public static readonly Color Blocked = new(0.12f, 0.13f, 0.17f);

        private static readonly Color[] Colors =
        {
            new(0.90f, 0.28f, 0.30f), new(0.24f, 0.56f, 0.97f), new(0.25f, 0.75f, 0.50f),
            new(0.95f, 0.76f, 0.31f), new(0.64f, 0.42f, 0.95f), new(0.95f, 0.53f, 0.31f)
        };

        private static readonly string[] Names = { "Red", "Blue", "Green", "Yellow", "Purple", "Orange" };

        public static Color ColorOf(int tileId) =>
            tileId == Tile.Blocked ? Blocked : Tile.IsPlayable(tileId) ? Colors[(tileId - 1) % Colors.Length] : Color.clear;

        public static string NameOf(int tileId) =>
            Tile.IsPlayable(tileId) ? Names[(tileId - 1) % Names.Length] : "Tile";

        /// <summary>Tile name in its own colour, as TMP rich text.</summary>
        public static string Tag(int tileId) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(ColorOf(tileId))}>{NameOf(tileId)}</color>";
    }
}
