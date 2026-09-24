using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Placeholder UI art drawn in code: tintable white shapes (panels, pills, hexagon, icons) plus a few
    ///     coloured icons (coin, gem, heart, hammer) and the title font material. Every file is written once and
    ///     never overwritten, so the art team replaces a PNG in place (same name, same GUID) and every prefab
    ///     picks it up.
    /// </summary>
    public static class UiArt
    {
        public const string Folder = "Assets/Game/Art/UI/Generated";
        public const string TitleMaterialPath = "Assets/Game/Art/UI/Fonts/Title Outline.mat";

        public const string Rounded16 = "ui_rounded_16";
        public const string Rounded32 = "ui_rounded_32";
        public const string Pill = "ui_pill";
        public const string Gloss = "ui_gloss";
        public const string Hexagon = "ui_hexagon";
        public const string Star = "ui_star";
        public const string Plus = "ui_icon_plus";
        public const string Cross = "ui_icon_cross";
        public const string Gear = "ui_icon_gear";
        public const string Pencil = "ui_icon_pencil";
        public const string Lock = "ui_icon_lock";
        public const string ShopIcon = "ui_icon_shop";
        public const string TrophyIcon = "ui_icon_trophy";
        public const string StartIcon = "ui_icon_start";
        public const string CalendarIcon = "ui_icon_calendar";
        public const string BackgroundHome = "ui_bg_home";
        public const string BackgroundShop = "ui_bg_shop";
        public const string CoinIcon = "icon_coin";
        public const string GemIcon = "icon_gem";
        public const string HeartIcon = "icon_heart";
        public const string HammerIcon = "icon_hammer";

        private static readonly Color Gold = Hex(0xFFD23F), GoldDark = Hex(0xE89A1C);

        public static Sprite Get(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png") ??
            throw new InvalidOperationException($"UI sprite {name} missing - run UiArt.EnsureAll first.");

        public static Material TitleMaterial => AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);

        public static void EnsureAll()
        {
            FrameworkSetup.EnsureFolder(Folder);

            Shape(Rounded16, 64, 16, Layer(RoundRect(0, 0, 1, 1, 16 / 64f)));
            Shape(Rounded32, 128, 32, Layer(RoundRect(0, 0, 1, 1, 32 / 128f)));
            Shape(Pill, 128, 63, Layer(RoundRect(0, 0, 1, 1, 0.5f)));
            Shape(Gloss, 128, 32, new ShapeLayer(RoundRect(0, 0, 1, 1, 32 / 128f),
                p => new Color(1, 1, 1, Mathf.Clamp01((p.y - 0.45f) / 0.55f) * 0.45f)));
            Shape(Hexagon, 256, 0, Layer(Polygon(Regular(6, 0.5f, 0.5f, 0.48f, 90))));
            Shape(Star, 256, 0, Layer(Polygon(StarPoints(0.5f, 0.52f, 0.48f, 0.22f))));
            Shape(Plus, 128, 0, Layer(Union(RoundRect(0.4f, 0.14f, 0.6f, 0.86f, 0.08f), RoundRect(0.14f, 0.4f, 0.86f, 0.6f, 0.08f))));
            Shape(Cross, 128, 0, Layer(Union(Capsule(0.22f, 0.22f, 0.78f, 0.78f, 0.1f), Capsule(0.22f, 0.78f, 0.78f, 0.22f, 0.1f))));
            Shape(Gear, 256, 0, Layer(GearShape()), Erase(Circle(0.5f, 0.5f, 0.14f)));
            Shape(Pencil, 128, 0, Layer(Union(Capsule(0.32f, 0.32f, 0.82f, 0.82f, 0.1f), Polygon(new[] { V(0.14f, 0.14f), V(0.36f, 0.22f), V(0.22f, 0.36f) }))));
            Shape(Lock, 128, 0, Layer(Union(RoundRect(0.2f, 0.08f, 0.8f, 0.56f, 0.1f),
                    (x, y) => Annulus(0.5f, 0.58f, 0.15f, 0.24f)(x, y) && y > 0.52f)),
                Erase(Union(Circle(0.5f, 0.35f, 0.07f), RoundRect(0.47f, 0.16f, 0.53f, 0.34f, 0.02f))));
            Shape(ShopIcon, 256, 0, Layer(Union(RoundRect(0.16f, 0.1f, 0.84f, 0.56f, 0.06f), RoundRect(0.08f, 0.56f, 0.92f, 0.84f, 0.08f),
                    Circle(0.19f, 0.56f, 0.11f), Circle(0.4f, 0.56f, 0.11f), Circle(0.6f, 0.56f, 0.11f), Circle(0.81f, 0.56f, 0.11f))),
                Erase(RoundRect(0.4f, 0.1f, 0.6f, 0.38f, 0.04f)));
            Shape(TrophyIcon, 256, 0, Layer(Union(Polygon(new[] { V(0.22f, 0.9f), V(0.78f, 0.9f), V(0.64f, 0.46f), V(0.36f, 0.46f) }),
                (x, y) => (Annulus(0.24f, 0.72f, 0.08f, 0.15f)(x, y) || Annulus(0.76f, 0.72f, 0.08f, 0.15f)(x, y)) && y > 0.58f,
                RoundRect(0.45f, 0.26f, 0.55f, 0.5f, 0.02f), RoundRect(0.28f, 0.08f, 0.72f, 0.28f, 0.06f))));
            Shape(StartIcon, 256, 0, Layer(RoundRect(0.12f, 0.12f, 0.88f, 0.88f, 0.22f)),
                Erase(Union(Circle(0.36f, 0.58f, 0.07f), Circle(0.64f, 0.58f, 0.07f), RoundRect(0.34f, 0.26f, 0.66f, 0.42f, 0.08f))));
            Shape(CalendarIcon, 256, 0, Layer(RoundRect(0.12f, 0.1f, 0.88f, 0.8f, 0.1f)),
                Erase(RoundRect(0.2f, 0.18f, 0.8f, 0.6f, 0.04f)), Layer(Union(RoundRect(0.3f, 0.7f, 0.38f, 0.92f, 0.04f), RoundRect(0.62f, 0.7f, 0.7f, 0.92f, 0.04f))),
                Layer(RoundRect(0.38f, 0.28f, 0.62f, 0.5f, 0.04f)));

            Gradient(BackgroundHome, Hex(0x1E47CC), Hex(0x19B8F2));
            Gradient(BackgroundShop, Hex(0x1B3F8F), Hex(0x224FA8));

            Shape(CoinIcon, 256, 0, new ShapeLayer(Circle(0.5f, 0.5f, 0.46f), _ => GoldDark),
                new ShapeLayer(Circle(0.5f, 0.52f, 0.38f), _ => Gold),
                new ShapeLayer(Circle(0.42f, 0.62f, 0.12f), _ => new Color(1, 1, 1, 0.35f)));
            Shape(GemIcon, 256, 0,
                new ShapeLayer(Polygon(new[] { V(0.5f, 0.06f), V(0.94f, 0.58f), V(0.76f, 0.86f), V(0.24f, 0.86f), V(0.06f, 0.58f) }), _ => Hex(0x2E9BE8)),
                new ShapeLayer(Polygon(new[] { V(0.5f, 0.06f), V(0.06f, 0.58f), V(0.24f, 0.86f), V(0.5f, 0.86f) }), _ => Hex(0x7DD3FF)));
            Shape(HeartIcon, 256, 0, new ShapeLayer(HeartShape(0.5f, 0.47f, 0.4f), _ => Hex(0xE8303F)),
                new ShapeLayer(Circle(0.35f, 0.63f, 0.09f), _ => new Color(1, 1, 1, 0.4f)));
            Shape(HammerIcon, 256, 0, new ShapeLayer(Capsule(0.3f, 0.2f, 0.62f, 0.6f, 0.06f), _ => Hex(0xA0662E)),
                new ShapeLayer(Rotated(RoundRect(0.34f, 0.58f, 0.9f, 0.82f, 0.06f), 0.62f, 0.7f, 38), _ => Hex(0x9AA6BF)));

            EnsureTitleMaterial();
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- TMP title style

        private static void EnsureTitleMaterial()
        {
            if (TitleMaterial) return;

            var font = TMP_Settings.defaultFontAsset;
            if (!font) return;

            FrameworkSetup.EnsureFolder(Path.GetDirectoryName(TitleMaterialPath)?.Replace('\\', '/'));
            var material = new Material(font.material) { name = "Title Outline" };
            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Hex(0x141A3A));
            material.SetColor(ShaderUtilities.ID_UnderlayColor, Hex(0x141A3A));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
            AssetDatabase.CreateAsset(material, TitleMaterialPath);
        }

        // ---------------------------------------------------------------- rasteriser

        private delegate bool Inside(float x, float y);

        private readonly struct ShapeLayer
        {
            public ShapeLayer(Inside inside, Func<Vector2, Color> color, bool erase = false)
            {
                InsideTest = inside;
                ColorAt = color;
                IsErase = erase;
            }

            public Inside InsideTest { get; }
            public Func<Vector2, Color> ColorAt { get; }
            public bool IsErase { get; }
        }

        private static ShapeLayer Layer(Inside inside) => new(inside, _ => Color.white);

        private static ShapeLayer Erase(Inside inside) => new(inside, _ => Color.clear, true);

        /// <summary>Renders layers bottom to top with 4x4 supersampling and imports the PNG as a (9-sliced) sprite.</summary>
        private static void Shape(string name, int size, int border, params ShapeLayer[] layers)
        {
            var path = $"{Folder}/{name}.png";
            if (File.Exists(path)) return;

            const int samples = 4;
            var pixels = new Color[size * size];
            for (var py = 0; py < size; py++)
            for (var px = 0; px < size; px++)
            {
                var result = Color.clear;
                foreach (var layer in layers)
                {
                    var hits = 0;
                    for (var sy = 0; sy < samples; sy++)
                    for (var sx = 0; sx < samples; sx++)
                        if (layer.InsideTest((px + (sx + 0.5f) / samples) / size, (py + (sy + 0.5f) / samples) / size))
                            hits++;
                    if (hits == 0) continue;

                    var coverage = hits / (float)(samples * samples);
                    var uv = new Vector2((px + 0.5f) / size, (py + 0.5f) / size);
                    if (layer.IsErase)
                    {
                        result.a *= 1 - coverage;
                        continue;
                    }

                    var c = layer.ColorAt(uv);
                    var a = c.a * coverage;
                    var outA = a + result.a * (1 - a);
                    result = outA <= 0
                        ? Color.clear
                        : new Color((c.r * a + result.r * result.a * (1 - a)) / outA,
                            (c.g * a + result.g * result.a * (1 - a)) / outA,
                            (c.b * a + result.b * result.a * (1 - a)) / outA, outA);
                }

                pixels[py * size + px] = result;
            }

            Write(path, size, size, pixels, border);
        }

        private static void Gradient(string name, Color top, Color bottom)
        {
            var path = $"{Folder}/{name}.png";
            if (File.Exists(path)) return;

            const int height = 256;
            var pixels = new Color[4 * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < 4; x++)
                pixels[y * 4 + x] = Color.Lerp(bottom, top, y / (height - 1f));
            Write(path, 4, height, pixels, 0);
        }

        private static void Write(string path, int width, int height, Color[] pixels, int border)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // ---------------------------------------------------------------- shapes (unit square, y up)

        private static Vector2 V(float x, float y) => new(x, y);

        private static Inside Union(params Inside[] shapes) => (x, y) =>
        {
            foreach (var shape in shapes)
                if (shape(x, y))
                    return true;
            return false;
        };

        private static Inside Circle(float cx, float cy, float r) => (x, y) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

        private static Inside Annulus(float cx, float cy, float inner, float outer) => (x, y) =>
        {
            var d = (x - cx) * (x - cx) + (y - cy) * (y - cy);
            return d <= outer * outer && d >= inner * inner;
        };

        private static Inside RoundRect(float x0, float y0, float x1, float y1, float r) => (x, y) =>
        {
            float cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, hx = (x1 - x0) / 2 - r, hy = (y1 - y0) / 2 - r;
            float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
            var outside = new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0) <= r;
        };

        private static Inside Capsule(float ax, float ay, float bx, float by, float r) => (x, y) =>
        {
            var pa = new Vector2(x - ax, y - ay);
            var ba = new Vector2(bx - ax, by - ay);
            var h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude <= r;
        };

        private static Inside Rotated(Inside shape, float cx, float cy, float degrees)
        {
            var rad = -degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return (x, y) => shape(cx + (x - cx) * cos - (y - cy) * sin, cy + (x - cx) * sin + (y - cy) * cos);
        }

        private static Inside Polygon(IReadOnlyList<Vector2> points) => (x, y) =>
        {
            var inside = false;
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
                if ((points[i].y > y) != (points[j].y > y) &&
                    x < (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    inside = !inside;
            return inside;
        };

        private static Vector2[] Regular(int sides, float cx, float cy, float r, float startDegrees)
        {
            var points = new Vector2[sides];
            for (var i = 0; i < sides; i++)
            {
                var a = (startDegrees + i * 360f / sides) * Mathf.Deg2Rad;
                points[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }

            return points;
        }

        private static Vector2[] StarPoints(float cx, float cy, float outer, float inner)
        {
            var points = new Vector2[10];
            for (var i = 0; i < 10; i++)
            {
                var a = (90 + i * 36) * Mathf.Deg2Rad;
                var r = i % 2 == 0 ? outer : inner;
                points[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }

            return points;
        }

        private static Inside HeartShape(float cx, float cy, float scale) => (x, y) =>
        {
            float u = (x - cx) / scale * 1.2f, v = (y - cy) / scale * 1.2f + 0.15f;
            var a = u * u + v * v - 1;
            return a * a * a - u * u * v * v * v <= 0;
        };

        private static Inside GearShape() => (x, y) =>
        {
            float dx = x - 0.5f, dy = y - 0.5f, r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r <= 0.33f) return true;
            if (r > 0.46f) return false;
            var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg + 360;
            return angle % 45 < 22;
        };

        private static Color Hex(int rgb) =>
            new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
