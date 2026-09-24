using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Building blocks for the generated prefabs. Geometry helpers work in reference pixels
    ///     (<see cref="ReferenceWidth" /> x <see cref="ReferenceHeight" />, iPhone 12 Pro portrait); art always comes
    ///     from <see cref="UiSkin" /> roles, so the look can change without touching a builder.
    /// </summary>
    internal static class UiKit
    {
        public const int UiLayer = 5;
        public const float ReferenceWidth = 1170;
        public const float ReferenceHeight = 2532;

        /// <summary>Mock-up screenshots are 924 px wide; multiply their pixel positions by this.</summary>
        public const float Mock = ReferenceWidth / 924f;

        // Palette for the generated fallback skin and for plain overlays.
        public static readonly Color Navy = Hex(0x141A3A);
        public static readonly Color PanelFill = Hex(0x47B2F5);
        public static readonly Color InsetFill = Hex(0x5F6FE6);
        public static readonly Color TileFill = Hex(0x3B8EF0);
        public static readonly Color Green = Hex(0x4CD964);
        public static readonly Color Yellow = Hex(0xFFC93C);
        public static readonly Color Blue = Hex(0x3E8EF7);
        public static readonly Color Red = Hex(0xE53945);
        public static readonly Color Orange = Hex(0xFFA928);
        public static readonly Color Gold = Hex(0xFFC93C);
        public static readonly Color HudPill = new(0.05f, 0.08f, 0.2f, 0.45f);
        public static readonly Color NavBar = Hex(0x1B1F3B);
        public static readonly Color NavSelected = Hex(0x3B4BC8);
        public static readonly Color CardFrame = Hex(0xF5A623);
        public static readonly Color SectionFill = Hex(0x25277A);
        public static readonly Color Dim = new(0, 0, 0, 0.6f);

        // ---------------------------------------------------------------- geometry

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)go.transform;
            if (parent) rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0) =>
            Place(rect, 0, 0, 1, 1, left, bottom, right, top);

        public static RectTransform Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Fixed-size rect whose centre sits at an anchor point, nudged by <paramref name="offset" />.</summary>
        public static RectTransform At(RectTransform rect, float anchorX, float anchorY, float width, float height,
            Vector2 offset = default)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = offset;
            return rect;
        }

        /// <summary>Fixed-size rect positioned by its centre, measured from the parent's top-left corner.</summary>
        public static RectTransform FromTopLeft(RectTransform rect, float centreX, float centreY, float width, float height) =>
            At(rect, 0, 1, width, height, new Vector2(centreX, -centreY));

        /// <summary>Full-width strip <paramref name="top" /> px below the parent's top edge.</summary>
        public static RectTransform Top(RectTransform rect, float top, float height, float left = 0, float right = 0)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(left, -(top + height));
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Bottom(RectTransform rect, float bottom, float height, float left = 0, float right = 0)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
            return rect;
        }

        // ---------------------------------------------------------------- graphics

        /// <summary>
        ///     Draws a skin part. Sliced sprites keep their corners, scaled by <paramref name="cornerScale" />; plain
        ///     sprites stretch, or keep their aspect inside the rect when <paramref name="fit" /> (icons).
        /// </summary>
        public static Image Img(RectTransform rect, SkinPart part, bool raycast = false, float cornerScale = 1, bool fit = false)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = part.Tint;
            image.raycastTarget = raycast;
            if (!part.Sprite) return image;

            image.sprite = part.Sprite;
            if (part.Sprite.border != Vector4.zero && !fit)
            {
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1 / cornerScale;
            }
            else
            {
                image.preserveAspect = fit;
            }

            return image;
        }

        public static Image Icon(RectTransform rect, SkinPart part) => Img(rect, part, false, 1, true);

        public static Image Solid(RectTransform rect, Color color, bool raycast = false) =>
            Img(rect, new SkinPart(null, color), raycast);

        public static TextMeshProUGUI Label(RectTransform parent, string name, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool title = false)
        {
            var label = Stretch(Node(name, parent)).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = UiSkin.Font;
            label.fontSharedMaterial = title && UiSkin.TitleMaterial ? UiSkin.TitleMaterial : label.font.material;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>White outlined title text; shrinks to fit its box down to half size instead of wrapping.</summary>
        public static TextMeshProUGUI Title(RectTransform parent, string name, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = Label(parent, name, text, size, Color.white, alignment, true);
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = size * 0.5f;
            label.enableWordWrapping = false;
            return label;
        }

        // ---------------------------------------------------------------- widgets

        /// <summary>Skinned button with an outlined label sitting on the face (above the art's bottom bevel).</summary>
        public static Button SkinButton(RectTransform parent, string name, SkinPart skin, string text, float fontSize,
            out TextMeshProUGUI label, float cornerScale = 1)
        {
            var root = Node(name, parent);
            var face = Img(root, skin, true, cornerScale);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            label = Title(Place(Node("LabelBox", root), 0, 0.12f, 1, 1, 18, 0, 18, 0), "Label", text, fontSize);
            return button;
        }

        public static Button IconButton(RectTransform parent, string name, SkinPart background, SkinPart icon,
            float iconInset = 0.22f, float cornerScale = 1)
        {
            var root = Node(name, parent);
            var face = Img(root, background, true, cornerScale, !background.Sprite || background.Sprite.border == Vector4.zero);
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            if (icon.Sprite) Icon(Place(Node("Icon", root), iconInset, iconInset + 0.04f, 1 - iconInset, 1 - iconInset + 0.04f), icon);
            return button;
        }

        public static Button CloseButton(RectTransform parent) =>
            IconButton(parent, "CloseButton", UiSkin.CloseButton, UiSkin.CloseIcon, 0.26f);

        /// <summary>
        ///     Modal shell: dim full-screen backdrop (a button), header panel with an outlined title and a close
        ///     button, pop transition. The panel art is scaled so its header band is <paramref name="headerHeight" />
        ///     tall. Returns the panel rect, which content is laid out against.
        /// </summary>
        public static RectTransform PopupShell<T>(string name, Vector2 size, float headerHeight, SkinPart panelSkin,
            string title, out T view, out TextMeshProUGUI titleText, out Button close, bool closeOnBackdrop) where T : UIPopup
        {
            var root = Stretch(Node(name, null));
            var backdrop = root.gameObject.AddComponent<Button>();
            backdrop.targetGraphic = Solid(root, Dim, true);
            backdrop.transition = Selectable.Transition.None;

            // The reference panel's header band is 204 px of its 229 px sliced top border.
            var panel = At(Node("Panel", root), 0.5f, 0.5f, size.x, size.y);
            Img(panel, panelSkin, true, UiSkin.UsesReference ? headerHeight / 204f : 1);

            var header = Top(Node("Header", panel), 0, headerHeight);
            titleText = Title(Place(Node("TitleBox", header), 0, 0, 1, 1, 150, 6, 150, 0), "TitleText", title, headerHeight * 0.5f);
            close = CloseButton(header);
            At((RectTransform)close.transform, 1, 0.5f, 108, 108, new Vector2(-86, 8));

            view = root.gameObject.AddComponent<T>();
            UiSetup.Wire(view, "backdropButton", backdrop);
            UiSetup.SetBool(view, "closeOnBackdrop", closeOnBackdrop);
            var pop = root.gameObject.AddComponent<ScalePopTransition>();
            UiSetup.Wire(pop, "panel", panel);
            UiSetup.Wire(view, "transition", pop);
            return panel;
        }

        public static void Height(Component component, float height)
        {
            var element = component.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
        }

        public static void SetLayerRecursively(GameObject go)
        {
            go.layer = UiLayer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject);
        }

        public static Color Hex(int rgb) =>
            new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
