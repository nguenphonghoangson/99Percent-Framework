using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace NinetyNine.UI
{
    /// <summary>
    ///     Show/hide animation of a view, on unscaled time so it runs while the game is paused. Outside play mode,
    ///     on an inactive object or with a zero duration it snaps to the end state and completes immediately.
    /// </summary>
    public abstract class UITransition : MonoBehaviour
    {
        [SerializeField] private float duration = 0.2f;

        private TaskCompletionSource<bool> _pending;

        public Task Play(bool show)
        {
            Finish();
            if (!Application.isPlaying || !isActiveAndEnabled || duration <= 0)
            {
                Apply(show ? 1 : 0);
                return Task.CompletedTask;
            }

            _pending = new TaskCompletionSource<bool>();
            StartCoroutine(Run(show, _pending));
            return _pending.Task;
        }

        /// <param name="visibility">0 = fully hidden, 1 = fully shown.</param>
        protected abstract void Apply(float visibility);

        // A view deactivated or destroyed mid-animation must not leave the navigator waiting forever.
        private void OnDisable() => Finish();

        private IEnumerator Run(bool show, TaskCompletionSource<bool> completion)
        {
            float from = show ? 0 : 1, to = show ? 1 : 0;
            for (var t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                var x = t / duration;
                Apply(Mathf.Lerp(from, to, 1 - (1 - x) * (1 - x)));
                yield return null;
            }

            Apply(to);
            if (_pending == completion) _pending = null;
            completion.TrySetResult(true);
        }

        private void Finish()
        {
            StopAllCoroutines();
            _pending?.TrySetResult(true);
            _pending = null;
        }
    }
}
