using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Modal view above the screens. Popups stack; a popup can hand a result back to whoever opened it with
    ///     <see cref="INavigator.ShowPopupAndWait" /> (confirm dialogs, choice pickers).
    /// </summary>
    public abstract class UIPopup : UIView
    {
        [SerializeField] private bool closeOnBack = true;
        [SerializeField] private bool closeOnBackdrop = true;

        [Tooltip("Full-screen button behind the panel. Optional.")]
        [SerializeField] private Button backdropButton;

        private TaskCompletionSource<object> _closed = new();

        public bool CloseOnBack => closeOnBack;

        /// <summary>Completes with the result passed to <see cref="Close" /> when this opening ends.</summary>
        public Task<object> Closed => _closed.Task;

        protected override void OnCreated()
        {
            if (backdropButton) backdropButton.onClick.AddListener(OnBackdrop);
        }

        public void Close(object result = null) => Forget(Navigator.ClosePopup(this, result));

        internal void BeginOpening()
        {
            if (_closed.Task.IsCompleted) _closed = new TaskCompletionSource<object>();
        }

        internal void CompleteClosing(object result) => _closed.TrySetResult(result);

        private void OnBackdrop()
        {
            if (closeOnBackdrop) Close();
        }
    }
}
