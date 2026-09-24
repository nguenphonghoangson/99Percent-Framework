using System.Threading.Tasks;

namespace NinetyNine.UI
{
    public enum UIViewKind
    {
        Screen,
        Popup
    }

    /// <summary>
    ///     Screen stack + popup stack, addressed by catalog key (by convention the feature id: "shop",
    ///     "daily_reward"). Callers never reference another feature's view type, so opening a screen does not
    ///     couple assemblies.
    ///     <para>
    ///         Every operation is queued and runs to completion — transitions included — before the next starts,
    ///         so the stacks are never observed half-changed. Input is blocked while an operation runs. A view hook
    ///         that navigates (e.g. Home opening the daily popup in OnOpened) is simply queued behind the current
    ///         operation; never await a navigation call inside a hook.
    ///     </para>
    /// </summary>
    public interface INavigator
    {
        UIScreen CurrentScreen { get; }

        UIPopup TopPopup { get; }

        int ScreenCount { get; }

        int PopupCount { get; }

        bool IsBusy { get; }

        /// <summary>Closes open popups, shows the new screen on top, then deactivates the one below.</summary>
        Task<UIScreen> PushScreen(string key, object args = null);

        /// <summary>Swaps the top screen without growing the stack (Home → Gameplay → Home loops).</summary>
        Task<UIScreen> ReplaceScreen(string key, object args = null);

        /// <summary>False when only the root screen is left.</summary>
        Task<bool> PopScreen();

        Task PopToRoot();

        Task<UIPopup> ShowPopup(string key, object args = null);

        /// <summary>Opens a popup and completes with the result it closes with (null when dismissed).</summary>
        Task<object> ShowPopupAndWait(string key, object args = null);

        /// <summary>Closes <paramref name="popup" />, or the top popup when null.</summary>
        Task ClosePopup(UIPopup popup = null, object result = null);

        /// <summary>
        ///     Android back / Escape: closes the top popup (a popup that forbids it still swallows the press), else
        ///     pops the screen. False at the root, where the game decides (quit dialog, move task to back).
        /// </summary>
        bool HandleBack();
    }

    public readonly struct UIViewOpenedEvent
    {
        public UIViewOpenedEvent(string key, UIViewKind kind)
        {
            Key = key;
            Kind = kind;
        }

        public string Key { get; }
        public UIViewKind Kind { get; }
    }

    public readonly struct UIViewClosedEvent
    {
        public UIViewClosedEvent(string key, UIViewKind kind)
        {
            Key = key;
            Kind = kind;
        }

        public string Key { get; }
        public UIViewKind Kind { get; }
    }
}
