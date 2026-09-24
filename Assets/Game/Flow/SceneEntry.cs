using System;
using NinetyNine.Core;
using NinetyNine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinetyNine.Game.Flow
{
    /// <summary>
    ///     Entry of a context scene (Home, Gameplay). Once services are up it switches the UI to this scene's catalog
    ///     (<see cref="uiCatalog" />: Home UI or Gameplay UI), makes <see cref="rootScreen" /> the only screen in the
    ///     navigator stack and hands it the args the scene was loaded with. Composition only: the logic lives in the
    ///     features behind the screen. Subclass and override <see cref="Enter" /> when a scene has world objects to set
    ///     up (board, camera rig).
    /// </summary>
    public class SceneEntry : MonoBehaviour
    {
        [Tooltip("Catalog key of the screen this scene shows as the root of the navigator stack.")]
        [SerializeField] private string rootScreen;

        [Tooltip("Screens and popups only this scene opens. Empty = everything comes from the global catalog.")]
        [SerializeField] private UICatalog uiCatalog;

        private static string _redirectedFrom;

        /// <summary>
        ///     The scene played directly in the editor before Intro existed in memory; Intro loads it instead of Home so
        ///     pressing Play in any context scene still lands there. Consumed once.
        /// </summary>
        public static string TakeRedirect()
        {
            var scene = _redirectedFrom;
            _redirectedFrom = null;
            return scene;
        }

        private void Start()
        {
            var services = ServiceLocator.Current;
            if (services == null)
            {
                _redirectedFrom = gameObject.scene.name;
                SceneManager.LoadScene(GameScenes.Intro);
                return;
            }

            Enter(services);
        }

        protected virtual void Enter(IServiceResolver services) =>
            ShowRootScreen(services.Require<INavigator>(), services.Require<IUIFactory>(), services.Require<ISceneLoader>().Args);

        private async void ShowRootScreen(INavigator navigator, IUIFactory factory, object args)
        {
            try
            {
                // Screens of the previous context stay on the persistent UI root until replaced here; closing them
                // runs their normal lifecycle (Gameplay quits an unfinished level). The context switch sits between
                // the two steps: the popped screens are still cached under the old context and get dropped with it,
                // and the replaced root is destroyed on release instead of kept.
                await navigator.PopToRoot();
                factory.SetContext(uiCatalog);
                await navigator.ReplaceScreen(rootScreen, args);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }
}
