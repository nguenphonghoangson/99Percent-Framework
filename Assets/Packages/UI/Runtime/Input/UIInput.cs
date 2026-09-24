using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NinetyNine.UI
{
    /// <summary>Raises <see cref="BackPressed" /> for Android back / Escape.</summary>
    public sealed class BackButtonListener : MonoBehaviour
    {
        public event Action BackPressed;

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape)) BackPressed?.Invoke();
#endif
        }
    }

    /// <summary>Invisible full-screen raycast target that swallows taps while a navigation runs.</summary>
    public static class InputBlocker
    {
        public static GameObject Create(RectTransform parent)
        {
            var blocker = new GameObject("InputBlocker", typeof(RectTransform), typeof(Image)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)blocker.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = Color.clear;
            blocker.SetActive(false);
            return blocker;
        }

        /// <summary>Adds an EventSystem under <paramref name="owner" /> when the scene has none.</summary>
        public static void EnsureEventSystem(Transform owner)
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>()) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(owner, false);
        }
    }
}
