using System;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.UI
{
    /// <summary>Canvas layers, bottom to top. Views never reorder these; they live inside them.</summary>
    public interface IUILayers
    {
        RectTransform ScreenLayer { get; }

        /// <summary>Persistent chrome (top bar, bottom nav) above screens but below popups.</summary>
        RectTransform HudLayer { get; }

        RectTransform PopupLayer { get; }

        RectTransform OverlayLayer { get; }
    }

    /// <summary>
    ///     The persistent canvas: Screens, Hud, Popups and Overlay layers (bottom to top), the input blocker and
    ///     the back button. Built in code from <see cref="UICatalog" /> settings so no scene has to carry it, and
    ///     kept across scene loads.
    /// </summary>
    public sealed class UIRoot : MonoBehaviour, IUILayers
    {
        private GameObject _blocker;

        public RectTransform ScreenLayer { get; private set; }
        public RectTransform HudLayer { get; private set; }
        public RectTransform PopupLayer { get; private set; }
        public RectTransform OverlayLayer { get; private set; }

        /// <summary>Inactive holder for freshly created views.</summary>
        public Transform Staging { get; private set; }

        public event Action BackPressed;

        public static UIRoot Create(UICatalog settings)
        {
            var go = new GameObject("[UIRoot]", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster)) { layer = 5 };
            if (Application.isPlaying) DontDestroyOnLoad(go);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = settings.sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = settings.referenceResolution;
            scaler.screenMatchMode = settings.screenMatchMode;
            scaler.matchWidthOrHeight = settings.matchWidthOrHeight;

            var root = go.AddComponent<UIRoot>();
            var rect = (RectTransform)go.transform;
            root.ScreenLayer = Layer("Screens", rect);
            root.HudLayer = Layer("Hud", rect);
            root.PopupLayer = Layer("Popups", rect);
            root.OverlayLayer = Layer("Overlay", rect);
            root._blocker = InputBlocker.Create(rect);

            var staging = new GameObject("Staging");
            staging.transform.SetParent(go.transform, false);
            staging.SetActive(false);
            root.Staging = staging.transform;

            go.AddComponent<BackButtonListener>().BackPressed += () => root.BackPressed?.Invoke();
            InputBlocker.EnsureEventSystem(go.transform);
            return root;
        }

        // The blocker is created after the layers, so it already sits above every view; views live inside the
        // layers and never reorder the root's children.
        public void SetInputBlocked(bool blocked)
        {
            if (_blocker && _blocker.activeSelf != blocked) _blocker.SetActive(blocked);
        }

        private static RectTransform Layer(string name, RectTransform parent)
        {
            var layer = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rect = (RectTransform)layer.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
