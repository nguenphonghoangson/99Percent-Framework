using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Imports every PNG under the reference art's Sprites folder as a UI sprite and restores its 9-slice
    ///     border from <c>sprites.json</c> (extracted from the source atlases), so a sliced panel keeps its corners.
    /// </summary>
    internal sealed class ReferenceArtImport : AssetPostprocessor
    {
        public const string SpritesFolder = UiSkin.ReferenceRoot + "/Sprites";

        private static Dictionary<string, Vector4> _borders;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpritesFolder + "/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (Borders().TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out var border))
                importer.spriteBorder = border;
        }

        private static Dictionary<string, Vector4> Borders()
        {
            if (_borders != null) return _borders;

            _borders = new Dictionary<string, Vector4>();
            var manifest = SpritesFolder + "/sprites.json";
            if (!File.Exists(manifest)) return _borders;

            // { "Name": { "border": [left, bottom, right, top], ... } } — same order as TextureImporter.spriteBorder.
            foreach (Match match in Regex.Matches(File.ReadAllText(manifest),
                         "\"([^\"]+)\":\\s*\\{\\s*\"border\":\\s*\\[\\s*([\\d.]+),\\s*([\\d.]+),\\s*([\\d.]+),\\s*([\\d.]+)"))
                _borders[match.Groups[1].Value] = new Vector4(float.Parse(match.Groups[2].Value),
                    float.Parse(match.Groups[3].Value), float.Parse(match.Groups[4].Value), float.Parse(match.Groups[5].Value));
            return _borders;
        }
    }
}
