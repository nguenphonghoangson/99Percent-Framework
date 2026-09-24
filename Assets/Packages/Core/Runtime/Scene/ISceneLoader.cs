using System.Threading.Tasks;

namespace NinetyNine.Core
{
    /// <summary>
    ///     Switches the runtime context (Home, Gameplay). A scene only holds the GameObjects of one context and
    ///     composes features; it owns no business logic. Services and the UI root outlive every scene.
    /// </summary>
    public interface ISceneLoader
    {
        string ActiveScene { get; }

        bool IsLoading { get; }

        /// <summary>Arguments of the load that brought up <see cref="ActiveScene" />, read by that scene's entry.</summary>
        object Args { get; }

        /// <summary>
        ///     Replaces the active scene. A request made while a load runs is ignored and gets the running load
        ///     back, so a double tap on Play loads once.
        /// </summary>
        Task LoadAsync(string sceneName, object args = null);
    }
}
