using UnityEngine;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Full-page view on the screen stack. Only the top screen is active; the ones below are deactivated
    ///     (and get <see cref="OnCovered" />/<see cref="OnRevealed" />) but keep their place in the stack.
    /// </summary>
    public abstract class UIScreen : UIView
    {
        [Tooltip("Back button / Escape pops this screen. Off for screens that must be left through their own UI.")]
        [SerializeField] private bool allowBack = true;

        public bool AllowBack => allowBack;

        protected internal virtual void OnCovered() { }

        protected internal virtual void OnRevealed() { }

        /// <summary>Pops this screen when it is on top. Wire to a close/back button.</summary>
        public void Back() => Forget(Navigator.PopScreen());
    }
}
