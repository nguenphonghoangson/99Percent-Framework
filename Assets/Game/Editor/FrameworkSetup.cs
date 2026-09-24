using System;
using System.Collections.Generic;
using System.Linq;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Home;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.Shop;
using NinetyNine.Game;
using NinetyNine.Game.Flow;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Reward;
using NinetyNine.UI;
using NinetyNine.Modules.Unlock;
using Random = System.Random;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NinetyNine.Editor
{
    /// <summary>
    ///     Creates the default config assets and the Intro / Home / Gameplay scenes. Existing assets are never
    ///     overwritten, so it is safe to run again after designers have tuned the data — it only fills in what is
    ///     missing.
    /// </summary>
    public static class FrameworkSetup
    {
        public const string ContentFolder = "Assets/Game/Content";
        public const string ScenesFolder = "Assets/Game/Scenes";
        public const string IntroScenePath = ScenesFolder + "/" + GameScenes.Intro + ".unity";
        public const string HomeScenePath = ScenesFolder + "/" + GameScenes.Home + ".unity";
        public const string GameplayScenePath = ScenesFolder + "/" + GameScenes.Gameplay + ".unity";

        /// <summary>Build order: Intro must be index 0, it is the only scene that boots services.</summary>
        public static readonly string[] ScenePaths = { IntroScenePath, HomeScenePath, GameplayScenePath };

        public const string EconomyPath = ContentFolder + "/Economy/EconomyConfig.asset";
        public const string LevelsPath = ContentFolder + "/Levels/LevelDatabase.asset";
        public const string PuzzleGameplayPath = ContentFolder + "/Gameplay/PuzzleGameplayConfig.asset";
        public const string ShopPath = ContentFolder + "/Shop/ShopConfig.asset";
        public const string ProfilePath = ContentFolder + "/Profile/ProfileFeatureConfig.asset";
        public const string DailyRewardPath = ContentFolder + "/DailyReward/DailyRewardConfig.asset";

        private const string Coin = "coin";
        private const string Gem = "gem";
        private const string Hammer = "booster_hammer";

        /// <summary>
        ///     Configs → UI prefabs + catalog → Home/Gameplay UI catalog split → Intro / Home / Gameplay scenes → Build
        ///     Settings. Fills in only what is missing.
        /// </summary>
        [MenuItem("NinetyNine/Setup/Create All (Configs, UI, Scenes)")]
        public static void CreateAll()
        {
            CreateConfigs();
            if (!UiSetup.CreatePrefabsAndCatalog()) return;

            UiCatalogSplit.Apply();
            if (!CreateScenes()) return;

            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[FrameworkSetup] Configs, UI and scenes are ready.");
        }

        [MenuItem("NinetyNine/Setup/Register Scenes In Build Settings")]
        public static void RegisterScenes()
        {
            var ours = ScenePaths.Where(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p))
                .Select(p => new EditorBuildSettingsScene(p, true));
            var others = EditorBuildSettings.scenes
                .Where(s => !ScenePaths.Contains(s.path) && AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path));
            EditorBuildSettings.scenes = ours.Concat(others).ToArray();
        }

        /// <summary>
        ///     Creates the missing scenes additively, so the scene open in the editor is left alone. False when an
        ///     untitled scene is open (Unity refuses additive creation then) and the user declined to save it.
        /// </summary>
        public static bool CreateScenes()
        {
            if (ScenePaths.All(p => AssetDatabase.LoadAssetAtPath<SceneAsset>(p))) return true;

            var additive = !HasUntitledScene();
            if (!additive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;

            CreateScene(IntroScenePath, additive, AddBootstrap);
            CreateScene(HomeScenePath, additive, () => AddSceneEntry(HomeScreenView.ScreenId, UiCatalogSplit.HomeCatalog));
            CreateScene(GameplayScenePath, additive, () => AddSceneEntry(PuzzleGameplayFeature.Id, UiCatalogSplit.GameplayCatalog));
            return true;
        }

        public static void CreateConfigs()
        {
            EnsureLivesCurrency();
            GetOrCreate<EconomyConfig>(EconomyPath, FillEconomy);
            GetOrCreate<LevelDatabase>(LevelsPath, FillLevels);
            GetOrCreate<PuzzleGameplayConfig>(PuzzleGameplayPath, FillPuzzleGameplay);
            GetOrCreate<ShopConfig>(ShopPath, FillShop);
            GetOrCreate<ProfileFeatureConfig>(ProfilePath, FillProfile);
            GetOrCreate<DailyRewardConfig>(DailyRewardPath, FillDailyReward);
        }

        private static void AddBootstrap()
        {
            var bootstrap = new GameObject("[GameBootstrap]").AddComponent<GameBootstrap>();
            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("economyConfig").objectReferenceValue = Load<EconomyConfig>(EconomyPath);
            serialized.FindProperty("levelDatabase").objectReferenceValue = Load<LevelDatabase>(LevelsPath);
            serialized.FindProperty("puzzleGameplayConfig").objectReferenceValue = Load<PuzzleGameplayConfig>(PuzzleGameplayPath);
            serialized.FindProperty("shopConfig").objectReferenceValue = Load<ShopConfig>(ShopPath);
            serialized.FindProperty("profileConfig").objectReferenceValue = Load<ProfileFeatureConfig>(ProfilePath);
            serialized.FindProperty("dailyRewardConfig").objectReferenceValue = Load<DailyRewardConfig>(DailyRewardPath);
            serialized.FindProperty("uiCatalog").objectReferenceValue = Load<UICatalog>(UiSetup.CatalogPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // A context scene holds its camera and a SceneEntry naming its root screen and UI catalog; world objects are
        // added later. A null catalog (UI not generated yet) is filled in by UiCatalogSplit on the next run.
        private static void AddSceneEntry(string rootScreen, UICatalog uiCatalog)
        {
            var entry = new GameObject("[SceneEntry]").AddComponent<SceneEntry>();
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("rootScreen").stringValue = rootScreen;
            serialized.FindProperty("uiCatalog").objectReferenceValue = uiCatalog;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateScene(string path, bool additive, Action populate)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) return;

            EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = Color.black;
            camera.transform.position = new Vector3(0, 0, -10);
            populate();

            EditorSceneManager.SaveScene(scene, path);
            if (!additive) return;

            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static bool HasUntitledScene()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) return true;
            return false;
        }

        // ---------------------------------------------------------------- default data

        // Lives are a capped currency until a Lives module adds refill timers; the HUD shows "MAX" at the cap.
        private static void EnsureLivesCurrency()
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfig>(EconomyPath);
            if (!economy || economy.Find("life") != null) return;

            economy.currencies.Add(new CurrencyDefinition { id = "life", initialBalance = 5, maxBalance = 5 });
            EditorUtility.SetDirty(economy);
        }

        private static void FillEconomy(EconomyConfig config)
        {
            config.currencies.Add(new CurrencyDefinition { id = "life", initialBalance = 5, maxBalance = 5 });
            config.currencies.Add(new CurrencyDefinition { id = Coin, initialBalance = 500 });
            config.currencies.Add(new CurrencyDefinition { id = Gem, initialBalance = 20 });
        }

        private static void FillLevels(LevelDatabase database)
        {
            // Sample content for the TapClearRules reference mechanic; replace with the game's real levels.
            database.levels.Add(RandomLevel("L001", 6, 6, 3, 20, 1, new[] { 300, 800, 1400 },
                ObjectiveDefinition.Collect(1, 12)));
            database.levels.Add(RandomLevel("L002", 6, 7, 3, 20, 2, new[] { 400, 1000, 1800 },
                ObjectiveDefinition.Collect(2, 15), ObjectiveDefinition.Score(600)));
            database.levels.Add(RandomLevel("L003", 6, 8, 4, 22, 3, new[] { 1200, 1800, 2600 },
                ObjectiveDefinition.Score(1200)));
            database.levels.Add(RandomLevel("L004", 7, 8, 4, 25, 4, new[] { 600, 1400, 2400 },
                ObjectiveDefinition.Collect(3, 18), ObjectiveDefinition.Collect(4, 18)));
            database.levels.Add(WithWalls(RandomLevel("L005", 7, 9, 4, 25, 5, new[] { 1500, 2200, 3200 },
                ObjectiveDefinition.Score(1500)), new[] { 1, 3, 5 }, 4));
            database.levels.Add(RandomLevel("L006", 8, 9, 5, 30, 6, new[] { 800, 1600, 2600 },
                ObjectiveDefinition.Collect(1, 22), ObjectiveDefinition.Collect(5, 14)));
            database.loopStartIndex = 2;
        }

        private static void FillPuzzleGameplay(PuzzleGameplayConfig config)
        {
            config.firstClearReward = RewardBundle.Of(RewardItem.Currency(Coin, 20));
            config.reviveMoves = 5;
            config.reviveCost = new Cost(Coin, 300);
            config.maxRevivesPerLevel = 2;
        }

        private static void FillShop(ShopConfig config)
        {
            config.featureUnlock = UnlockCondition.AtLevel(3);
            config.products.Add(new ShopProductDefinition
            {
                id = "free_gift", section = "daily", priceKind = PriceKind.Free, purchaseLimit = 1,
                rewards = RewardBundle.Of(RewardItem.Currency(Coin, 100))
            });
            config.products.Add(new ShopProductDefinition
            {
                id = "hammer_x3", section = "boosters", priceKind = PriceKind.Currency, cost = new Cost(Coin, 600),
                rewards = RewardBundle.Of(RewardItem.Item(Hammer, 3))
            });
            config.products.Add(new ShopProductDefinition
            {
                id = "coins_500", section = "coins", priceKind = PriceKind.Currency, cost = new Cost(Gem, 20),
                rewards = RewardBundle.Of(RewardItem.Currency(Coin, 500))
            });
            config.products.Add(new ShopProductDefinition
            {
                id = "gems_small", section = "gems", priceKind = PriceKind.Iap, iapProductId = "gems_small",
                rewards = RewardBundle.Of(RewardItem.Currency(Gem, 100))
            });
            config.products.Add(new ShopProductDefinition
            {
                id = "starter_pack", section = "offers", priceKind = PriceKind.Iap, iapProductId = "starter_pack",
                purchaseLimit = 1, unlock = UnlockCondition.AtLevel(5),
                rewards = RewardBundle.Of(RewardItem.Currency(Coin, 2000), RewardItem.Currency(Gem, 50),
                    RewardItem.Item(Hammer, 3), RewardItem.Item("avatar_crown", 1))
            });
        }

        private static void FillProfile(ProfileFeatureConfig config)
        {
            config.avatars.Add(new ProfileOption { id = "avatar_01" });
            config.avatars.Add(new ProfileOption { id = "avatar_02" });
            config.avatars.Add(new ProfileOption { id = "avatar_03" });
            config.avatars.Add(new ProfileOption { id = "avatar_04", unlock = UnlockCondition.AtLevel(10) });
            config.avatars.Add(new ProfileOption { id = "avatar_crown", unlock = UnlockCondition.WithItem("avatar_crown") });
            config.titles.Add(new ProfileOption { id = "title_rookie" });
            config.titles.Add(new ProfileOption { id = "title_solver", unlock = UnlockCondition.AtLevel(20) });
            config.titles.Add(new ProfileOption { id = "title_master", unlock = UnlockCondition.AtLevel(50) });
            config.freeRenames = 1;
            config.renameCost = new Cost(Gem, 50);
        }

        private static void FillDailyReward(DailyRewardConfig config)
        {
            config.days.Add(RewardBundle.Of(RewardItem.Currency(Coin, 50)));
            config.days.Add(RewardBundle.Of(RewardItem.Currency(Coin, 100)));
            config.days.Add(RewardBundle.Of(RewardItem.Item(Hammer, 1)));
            config.days.Add(RewardBundle.Of(RewardItem.Currency(Coin, 200)));
            config.days.Add(RewardBundle.Of(RewardItem.Currency(Gem, 5)));
            config.days.Add(RewardBundle.Of(RewardItem.Currency(Coin, 300)));
            config.days.Add(RewardBundle.Of(RewardItem.Currency(Coin, 500), RewardItem.Item(Hammer, 2)));
            config.resetStreakOnMiss = true;
            config.unlock = UnlockCondition.AtLevel(2);
        }

        // ---------------------------------------------------------------- helpers

        private static LevelDefinition RandomLevel(string id, int width, int height, int colours, int moves, int seed,
            int[] stars, params ObjectiveDefinition[] objectives)
        {
            var random = new Random(seed);
            var tiles = new int[width * height];
            for (var i = 0; i < tiles.Length; i++) tiles[i] = random.Next(1, colours + 1);

            return new LevelDefinition
            {
                id = id, width = width, height = height, tiles = tiles, moveLimit = moves, starScores = stars,
                objectives = new List<ObjectiveDefinition>(objectives)
            };
        }

        private static LevelDefinition WithWalls(LevelDefinition level, int[] columns, int row)
        {
            foreach (var x in columns) level.tiles[row * level.width + x] = Tile.Blocked;
            return level;
        }

        private static void GetOrCreate<T>(string path, Action<T> fill) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path)) return;

            EnsureFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static T Load<T>(string path) where T : ScriptableObject =>
            AssetDatabase.LoadAssetAtPath<T>(path) ??
            throw new InvalidOperationException($"{path} is missing — run CreateConfigs first.");

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
