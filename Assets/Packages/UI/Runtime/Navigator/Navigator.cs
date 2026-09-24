using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinetyNine.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NinetyNine.UI
{
    internal sealed class Navigator : INavigator, IDisposable
    {
        private readonly IEventBus _events;
        private readonly IUIFactory _factory;
        private readonly List<UIPopup> _popups = new();
        private readonly UIRoot _root;
        private readonly List<UIScreen> _screens = new();
        private bool _disposed;
        private int _pending;
        private Task _tail = Task.CompletedTask;

        public Navigator(IUIFactory factory, UIRoot root, IEventBus events)
        {
            _factory = factory;
            _root = root;
            _events = events;
            _root.BackPressed += OnBackPressed;
        }

        public UIScreen CurrentScreen => _screens.Count > 0 ? _screens[_screens.Count - 1] : null;

        public UIPopup TopPopup => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        public int ScreenCount => _screens.Count;

        public int PopupCount => _popups.Count;

        /// <summary>True while any operation is queued or running — not just the one currently animating.</summary>
        public bool IsBusy => _pending > 0;

        public Task<UIScreen> PushScreen(string key, object args = null) => Enqueue(async () =>
        {
            await CloseAllPopups();
            var previous = CurrentScreen;
            var screen = Create<UIScreen>(key, _root.ScreenLayer);
            _screens.Add(screen);
            await Open(screen, args, UIViewKind.Screen);

            if (previous)
            {
                previous.gameObject.SetActive(false);
                previous.OnCovered();
            }

            return screen;
        });

        public Task<UIScreen> ReplaceScreen(string key, object args = null) => Enqueue(async () =>
        {
            await CloseAllPopups();
            var previous = CurrentScreen;
            var screen = Create<UIScreen>(key, _root.ScreenLayer);
            _screens.Add(screen);
            await Open(screen, args, UIViewKind.Screen);

            if (previous)
            {
                _screens.Remove(previous);
                await Close(previous, UIViewKind.Screen, false);
            }

            return screen;
        });

        public Task<bool> PopScreen() => Enqueue(async () =>
        {
            if (_screens.Count <= 1) return false;

            await CloseAllPopups();
            await PopTop();
            return true;
        });

        public Task PopToRoot() => Enqueue(async () =>
        {
            if (_screens.Count <= 1) return true;

            await CloseAllPopups();
            // Screens between the root and the top are inactive: close them without transitions.
            while (_screens.Count > 2)
            {
                var hidden = _screens[_screens.Count - 2];
                _screens.RemoveAt(_screens.Count - 2);
                await Close(hidden, UIViewKind.Screen, false);
            }

            await PopTop();
            return true;
        });

        public Task<UIPopup> ShowPopup(string key, object args = null) => Enqueue(async () =>
        {
            var popup = Create<UIPopup>(key, _root.PopupLayer);
            popup.BeginOpening();
            _popups.Add(popup);
            await Open(popup, args, UIViewKind.Popup);
            return popup;
        });

        public async Task<object> ShowPopupAndWait(string key, object args = null)
        {
            var popup = await ShowPopup(key, args);
            return await popup.Closed;
        }

        public Task ClosePopup(UIPopup popup = null, object result = null) => Enqueue(async () =>
        {
            var target = popup ? popup : TopPopup;
            if (!target || !_popups.Remove(target)) return false;

            target.CompleteClosing(result);
            await Close(target, UIViewKind.Popup, true);
            return true;
        });

        public bool HandleBack()
        {
            if (IsBusy) return true;

            var popup = TopPopup;
            if (popup)
            {
                if (popup.CloseOnBack) Forget(ClosePopup(popup));
                return true;
            }

            if (_screens.Count > 1 && CurrentScreen.AllowBack)
            {
                Forget(PopScreen());
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Anything still queued or mid-transition is cancelled and never touches a view again: destroying
            // the root deactivates it, and Unity forbids changing children while that happens.
            _disposed = true;
            _root.BackPressed -= OnBackPressed;
            foreach (var popup in _popups) popup.CompleteClosing(null);
            _popups.Clear();
            _screens.Clear();
            _factory.Dispose();
            if (!_root) return;
            if (Application.isPlaying) Object.Destroy(_root.gameObject);
            else Object.DestroyImmediate(_root.gameObject);
        }

        private async Task PopTop()
        {
            var top = CurrentScreen;
            _screens.RemoveAt(_screens.Count - 1);

            var revealed = CurrentScreen;
            revealed.gameObject.SetActive(true);
            revealed.OnRevealed();

            await Close(top, UIViewKind.Screen, true);
        }

        private async Task CloseAllPopups()
        {
            while (_popups.Count > 0)
            {
                var popup = TopPopup;
                _popups.RemoveAt(_popups.Count - 1);
                popup.CompleteClosing(null);
                await Close(popup, UIViewKind.Popup, true);
            }
        }

        private T Create<T>(string key, RectTransform layer) where T : UIView
        {
            var view = _factory.Create(key);
            if (view is not T typed)
            {
                _factory.Release(view);
                throw new ArgumentException($"UI '{key}' is a {view.GetType().Name}, not a {typeof(T).Name}.", nameof(key));
            }

            typed.Attach(key, this);
            typed.transform.SetParent(layer, false);
            return typed;
        }

        private async Task Open(UIView view, object args, UIViewKind kind)
        {
            view.transform.SetAsLastSibling();
            view.gameObject.SetActive(true);
            view.IsOpen = true;
            view.OnOpening(args);
            await view.PlayTransition(true);
            if (_disposed) return;
            view.OnOpened();
            _events.Publish(new UIViewOpenedEvent(view.Key, kind));
        }

        private async Task Close(UIView view, UIViewKind kind, bool animate)
        {
            view.OnClosing();
            if (animate) await view.PlayTransition(false);
            if (_disposed || !view) return;
            view.gameObject.SetActive(false);
            view.IsOpen = false;
            view.OnClosed();
            _events.Publish(new UIViewClosedEvent(view.Key, kind));
            _factory.Release(view);
        }

        // Serialises operations. With nothing pending (and no transition) an operation completes synchronously,
        // which is what lets edit-mode tests drive the navigator without frames.
        private Task<T> Enqueue<T>(Func<Task<T>> operation)
        {
            var completion = new TaskCompletionSource<T>();
            if (_disposed)
            {
                completion.SetCanceled();
                return completion.Task;
            }

            if (_pending++ == 0) _root.SetInputBlocked(true);
            var previous = _tail;
            _tail = completion.Task;
            Run(previous, operation, completion);
            return completion.Task;
        }

        private async void Run<T>(Task previous, Func<Task<T>> operation, TaskCompletionSource<T> completion)
        {
            try
            {
                await previous;
            }
            catch
            {
                // A failed earlier operation already reported through its own task; the queue moves on.
            }

            try
            {
                if (_disposed) completion.TrySetCanceled();
                else completion.TrySetResult(await operation());
            }
            catch (Exception e)
            {
                completion.TrySetException(e);
            }
            finally
            {
                if (--_pending == 0 && !_disposed) _root.SetInputBlocked(false);
            }
        }

        private void OnBackPressed() => HandleBack();

        private static async void Forget(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
