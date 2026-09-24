using UnityEngine;

namespace NinetyNine.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FadeTransition : UITransition
    {
        private CanvasGroup _group;

        protected override void Apply(float visibility)
        {
            if (!_group) _group = GetComponent<CanvasGroup>();
            _group.alpha = visibility;
        }
    }
}
