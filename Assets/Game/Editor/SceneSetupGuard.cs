using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Before entering play mode, makes sure Intro / Home / Gameplay exist, lead the Build Settings list and use
    ///     their own UI catalogs — scene loads and screen lookups fail otherwise. Creates only what is missing and
    ///     never replaces the open scene, so forgetting NinetyNine/Setup is no longer a runtime error.
    /// </summary>
    [InitializeOnLoad]
    internal static class SceneSetupGuard
    {
        static SceneSetupGuard() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode || IsReady()) return;

            UiCatalogSplit.Apply();
            if (!FrameworkSetup.CreateScenes())
            {
                EditorApplication.isPlaying = false;
                Debug.LogError("[SceneSetupGuard] Save or close the untitled scene first; Intro/Home/Gameplay could not be created.");
                return;
            }

            FrameworkSetup.RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[SceneSetupGuard] Scenes, Build Settings (Intro → Home → Gameplay) and the Home/Gameplay UI catalogs are set up.");
        }

        private static bool IsReady()
        {
            var registered = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            return registered.Take(FrameworkSetup.ScenePaths.Length).SequenceEqual(FrameworkSetup.ScenePaths) &&
                   FrameworkSetup.ScenePaths.All(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p)) &&
                   UiCatalogSplit.IsApplied();
        }
    }
}
