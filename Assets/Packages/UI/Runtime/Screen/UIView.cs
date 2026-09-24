using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Base of everything the <see cref="INavigator" /> opens. Lifecycle, in order:
    ///     <see cref="OnCreated" /> once per instance → per open: <see cref="OnOpening" /> (bind args, still
    ///     invisible) → show transition → <see cref="OnOpened" /> … <see cref="OnClosing" /> → hide transition →
    ///     <see cref="OnClosed" />. Views may also use OnEnable/OnDisable: the object is active exactly while open.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIView : MonoBehaviour
    {
        [SerializeField] private UITransition transition;

        /// <summary>Catalog key this instance was created from.</summary>
        public string Key { get; private set; }

        public bool IsOpen { get; internal set; }

        protected INavigator Navigator { get; private set; }

        internal void Attach(string key, INavigator navigator)
        {
            if (Navigator != null) return;

            Key = key;
            Navigator = navigator;
            OnCreated();
        }

        internal Task PlayTransition(bool show) => transition ? transition.Play(show) : Task.CompletedTask;

        protected virtual void OnCreated() { }

        protected internal virtual void OnOpening(object args) { }

        protected internal virtual void OnOpened() { }

        protected internal virtual void OnClosing() { }

        protected internal virtual void OnClosed() { }

        /// <summary>Fire-and-forget for button handlers: navigation errors are logged, never swallowed.</summary>
        protected void Forget(Task task) => ForgetAsync(task, this);

        private static async void ForgetAsync(Task task, UnityEngine.Object context)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Debug.LogException(e, context);
            }
        }
    }
}
