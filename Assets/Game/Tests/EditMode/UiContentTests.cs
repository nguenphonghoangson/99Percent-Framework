using System.Linq;
using System.Reflection;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Home;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.Shop;
using NinetyNine.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Tests
{
    /// <summary>
    ///     A restyled prefab that loses a reference only fails at runtime, on the first open. This catches it in
    ///     CI: every serialized UnityEngine.Object field on our view components must be assigned.
    /// </summary>
    public class UiContentTests
    {
        private static readonly string[] Prefabs =
        {
            "Assets/Game/Features/Shop/UI/ShopScreen.prefab",
            "Assets/Game/Features/Shop/UI/ShopItem.prefab",
            "Assets/Game/Features/DailyReward/UI/DailyRewardPopup.prefab",
            "Assets/Game/Features/DailyReward/UI/DayCell.prefab",
            "Assets/Game/Features/Home/UI/HomeScreen.prefab",
            "Assets/Game/Features/Gameplay/UI/GameplayScreen.prefab",
            "Assets/Game/Features/Result/UI/WinPopup.prefab",
            "Assets/Game/Features/Result/UI/LosePopup.prefab",
            "Assets/Game/Features/Home/UI/MainHud.prefab",
            "Assets/Game/Features/Profile/UI/ProfilePopup.prefab"
        };

        [TestCaseSource(nameof(Prefabs))]
        public void View_prefab_has_every_reference_assigned(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) Assert.Ignore($"{path} not created yet - run NinetyNine/Setup.");

            var views = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(c => c.GetType().Namespace?.StartsWith("NinetyNine") == true).ToArray();
            Assert.That(views, Is.Not.Empty);

            foreach (var view in views)
            foreach (var field in view.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (!typeof(Object).IsAssignableFrom(field.FieldType)) continue;
                if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null) continue;

                Assert.That((Object)field.GetValue(view), Is.Not.Null, $"{view.GetType().Name}.{field.Name} in {path}");
            }
        }

        private static readonly (string key, string catalog, System.Type kind)[] Keys =
        {
            (HomeScreenView.ScreenId, HomeCatalog, typeof(UIScreen)),
            (ShopFeature.Id, HomeCatalog, typeof(UIScreen)),
            (DailyRewardFeature.Id, HomeCatalog, typeof(UIPopup)),
            (ProfileFeature.Id, HomeCatalog, typeof(UIPopup)),
            (PuzzleGameplayFeature.Id, GameplayCatalog, typeof(UIScreen)),
            (LevelEndKeys.Win, GameplayCatalog, typeof(UIPopup)),
            (LevelEndKeys.Lose, GameplayCatalog, typeof(UIPopup))
        };

        private const string GlobalCatalog = "Assets/Game/Content/UI/UICatalog.asset";
        private const string HomeCatalog = "Assets/Game/Content/UI/HomeUICatalog.asset";
        private const string GameplayCatalog = "Assets/Game/Content/UI/GameplayUICatalog.asset";

        [Test]
        public void Each_feature_key_lives_only_in_its_scene_catalog_with_the_right_view_kind()
        {
            var global = AssetDatabase.LoadAssetAtPath<UICatalog>(GlobalCatalog);
            if (!global || !AssetDatabase.LoadAssetAtPath<UICatalog>(HomeCatalog))
                Assert.Ignore("UI catalogs not split yet - run NinetyNine/Setup or enter Play mode once.");

            foreach (var (key, path, kind) in Keys)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<UICatalog>(path);
                Assert.That(catalog.Find(key)?.prefab, Is.InstanceOf(kind), $"{key} in {path}");
                Assert.That(global.Find(key), Is.Null, $"{key} must not stay in the global catalog");
            }
        }

        [TestCase("Assets/Game/Scenes/Home.unity", HomeCatalog)]
        [TestCase("Assets/Game/Scenes/Gameplay.unity", GameplayCatalog)]
        public void Context_scene_uses_its_own_ui_catalog(string scene, string catalog)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)) Assert.Ignore($"{scene} not created yet.");

            Assert.That(AssetDatabase.GetDependencies(scene, false), Has.Member(catalog));
        }
    }
}
