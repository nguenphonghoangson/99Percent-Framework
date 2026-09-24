using System.Collections.Generic;
using System.Linq;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Home;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.Shop;
using NinetyNine.Game.Flow;
using NinetyNine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Splits the UI into one catalog per context scene: Home UI and Gameplay UI, each wired into its scene's
    ///     <see cref="SceneEntry" />. The global catalog keeps the canvas settings and views shared by both scenes.
    ///     <para>
    ///         Runs after <see cref="UiSetup" />, which still writes every generated key into the global catalog. The
    ///         order keeps the game working at every step: copy keys into the scene catalogs → wire the scenes →
    ///         only then remove the keys from the global catalog. A scene that could not be wired leaves the global
    ///         entries in place, and lookups fall back to them.
    ///     </para>
    /// </summary>
    public static class UiCatalogSplit
    {
        public const string HomeCatalogPath = FrameworkSetup.ContentFolder + "/UI/HomeUICatalog.asset";
        public const string GameplayCatalogPath = FrameworkSetup.ContentFolder + "/UI/GameplayUICatalog.asset";

        public static readonly string[] HomeKeys =
            { HomeScreenView.ScreenId, ShopFeature.Id, DailyRewardFeature.Id, ProfileFeature.Id };

        public static readonly string[] GameplayKeys = { PuzzleGameplayFeature.Id, LevelEndKeys.Win, LevelEndKeys.Lose };

        private static (string scenePath, string catalogPath, string[] keys)[] Contexts => new[]
        {
            (FrameworkSetup.HomeScenePath, HomeCatalogPath, HomeKeys),
            (FrameworkSetup.GameplayScenePath, GameplayCatalogPath, GameplayKeys)
        };

        public static UICatalog HomeCatalog => AssetDatabase.LoadAssetAtPath<UICatalog>(HomeCatalogPath);
        public static UICatalog GameplayCatalog => AssetDatabase.LoadAssetAtPath<UICatalog>(GameplayCatalogPath);

        /// <summary>Full split. False when a scene could not be wired; the global catalog then keeps its keys.</summary>
        public static bool Apply()
        {
            var global = AssetDatabase.LoadAssetAtPath<UICatalog>(UiSetup.CatalogPath);
            if (!global) return false;

            foreach (var (_, catalogPath, keys) in Contexts) CopyInto(GetOrCreate(catalogPath), global, keys);

            var wired = Contexts.All(c => WireScene(c.scenePath, c.catalogPath));
            if (wired) RemoveFromGlobal(global, Contexts.SelectMany(c => c.keys));

            AssetDatabase.SaveAssets();
            return wired;
        }

        /// <summary>True when every existing context scene already references its catalog (cheap: no scene is opened).</summary>
        public static bool IsApplied()
        {
            var global = AssetDatabase.LoadAssetAtPath<UICatalog>(UiSetup.CatalogPath);
            if (global && Contexts.SelectMany(c => c.keys).Any(k => global.Find(k) != null)) return false;

            return Contexts.All(c => !AssetDatabase.LoadAssetAtPath<SceneAsset>(c.scenePath) ||
                                     AssetDatabase.GetDependencies(c.scenePath, false).Contains(c.catalogPath));
        }

        private static UICatalog GetOrCreate(string path)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<UICatalog>(path);
            if (catalog) return catalog;

            FrameworkSetup.EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            catalog = ScriptableObject.CreateInstance<UICatalog>();
            AssetDatabase.CreateAsset(catalog, path);
            return catalog;
        }

        // Keeps entries a designer already tuned in the scene catalog; fills missing or broken ones from the global.
        private static void CopyInto(UICatalog target, UICatalog global, IEnumerable<string> keys)
        {
            var changed = false;
            foreach (var key in keys)
            {
                var source = global.Find(key);
                if (source == null || !source.prefab) continue;

                var existing = target.Find(key);
                if (existing != null && existing.prefab) continue;

                if (existing == null) target.entries.Add(new UICatalogEntry { key = key, prefab = source.prefab, keepInstance = source.keepInstance });
                else existing.prefab = source.prefab;
                changed = true;
            }

            if (changed) EditorUtility.SetDirty(target);
        }

        private static void RemoveFromGlobal(UICatalog global, IEnumerable<string> keys)
        {
            var set = new HashSet<string>(keys);
            if (global.entries.RemoveAll(e => set.Contains(e.key)) > 0) EditorUtility.SetDirty(global);
        }

        /// <summary>
        ///     Assigns the catalog to the scene's SceneEntry when it has none. Works on the loaded scene when it is
        ///     open (asking first if it has unsaved changes), otherwise opens it additively and closes it again.
        /// </summary>
        private static bool WireScene(string scenePath, string catalogPath)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)) return true; // created later, already wired
            if (AssetDatabase.GetDependencies(scenePath, false).Contains(catalogPath)) return true;

            var scene = SceneManager.GetSceneByPath(scenePath);
            var openedHere = !scene.isLoaded;
            if (openedHere) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            else if (scene.isDirty && !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene })) return false;

            var catalog = AssetDatabase.LoadAssetAtPath<UICatalog>(catalogPath);
            bool wired = false, changed = false;
            foreach (var entry in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SceneEntry>(true)))
            {
                wired = true;
                var serialized = new SerializedObject(entry);
                var property = serialized.FindProperty("uiCatalog");
                if (property.objectReferenceValue) continue; // a designer's choice wins

                property.objectReferenceValue = catalog;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (changed) EditorSceneManager.SaveScene(scene);
            if (!wired) Debug.LogWarning($"[UiCatalogSplit] {scenePath} has no SceneEntry; its UI stays in the global catalog.");

            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            return wired;
        }
    }
}
