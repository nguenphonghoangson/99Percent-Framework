using System;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.Shop;
using NinetyNine.Game.Flow;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Iap;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Reward;
using NinetyNine.UI;
using NinetyNine.Persistence;
using UnityEngine;

namespace NinetyNine.Game
{
    /// <summary>
    ///     Composition root and the only thing in the Intro scene: lists which modules and features the game ships
    ///     with, builds them, then loads the first context scene. Services, this object and the UI root live for the
    ///     whole session; each context scene's <see cref="SceneEntry" /> opens its root screen through
    ///     <see cref="INavigator" />. Views read services from <see cref="ServiceLocator.Current" />.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Module config")]
        [SerializeField] private EconomyConfig economyConfig;
        [SerializeField] private LevelDatabase levelDatabase;

        [Header("Feature config")]
        [SerializeField] private PuzzleGameplayConfig puzzleGameplayConfig;
        [SerializeField] private ShopConfig shopConfig;
        [SerializeField] private ProfileFeatureConfig profileConfig;
        [SerializeField] private DailyRewardConfig dailyRewardConfig;

        [Header("UI")]
        [SerializeField] private UICatalog uiCatalog;

        [Header("Flow")]
        [Tooltip("Scene loaded once services are up.")]
        [SerializeField] private string firstScene = GameScenes.Home;

        private ModuleHost _host;

        public static IServiceResolver Services => ServiceLocator.Current;

        /// <summary>
        ///     Test seam: PlayMode tests boot the real scene on an in-memory save so they never read or write the
        ///     developer's PlayerPrefs. Null in normal runs.
        /// </summary>
        internal static ISaveProvider SaveProviderOverride { get; set; }

        private void Awake()
        {
            if (Services != null)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);

            _host = new ModuleHost()
                // Core / infrastructure
                .AddModule(new CoreModule(new SystemTimeService(), new EventBus(Debug.LogException), new FeatureToggles()))
                .AddModule(new SaveModule(SaveProviderOverride ?? new PlayerPrefsSaveProvider(), new JsonUtilitySerializer()))
                .AddModule(new UIModule(uiCatalog))
                .AddModule(new SceneModule())
                // Player
                .AddModule(new EconomyModule(economyConfig))
                .AddModule(new InventoryModule())
                .AddModule(new RewardModule())
                .AddModule(new ProgressionModule())
                .AddModule(new ProfileModule())
                // Gameplay
                .AddModule(new LevelModule(levelDatabase))
                .AddModule(new PuzzleModule());

            var iapProvider = CreateIapProvider();
            if (iapProvider != null) _host.AddModule(new IapModule(iapProvider));

            _host
                .AddFeature(new PuzzleGameplayFeature(puzzleGameplayConfig))
                .AddFeature(new ShopFeature(shopConfig))
                .AddFeature(new ProfileFeature(profileConfig))
                .AddFeature(new DailyRewardFeature(dailyRewardConfig));

            ServiceLocator.Current = _host.Build();
            LoadFirstScene(ServiceLocator.Current.Require<ISceneLoader>(), SceneEntry.TakeRedirect() ?? firstScene);
        }

        private async void LoadFirstScene(ISceneLoader scenes, string scene)
        {
            try
            {
                await scenes.LoadAsync(scene);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        private void OnApplicationQuit() => Flush();

        private void OnDestroy()
        {
            if (_host == null) return;

            Flush();
            _host.Dispose();
            _host = null;
            ServiceLocator.Current = null;
        }

        private static void Flush()
        {
            if (Services != null && Services.TryGet<ISaveService>(out var save)) save.Flush();
        }

        // Release builds plug the Unity IAP provider in here; without one, IAP shop products report Unavailable.
        private static IIapProvider CreateIapProvider()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new FakeIapProvider();
#else
            return null;
#endif
        }
    }
}
