using UnityEngine;

namespace NinetyNine.UI
{
    /// <summary>Popup entrance: fades the whole view and scales the panel up from <c>startScale</c>.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScalePopTransition : UITransition
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float startScale = 0.85f;

        private CanvasGroup _group;

        protected override void Apply(float visibility)
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            _group.alpha = visibility;
            if (panel) panel.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1, visibility);
        }
    }
}
