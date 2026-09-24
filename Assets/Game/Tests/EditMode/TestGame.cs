using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.Shop;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Iap;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Reward;
using NinetyNine.Persistence;
using Object = UnityEngine.Object;
using UnityEngine;

namespace NinetyNine.Tests
{
    /// <summary>
    ///     The real module/feature graph on an in-memory save and a manual clock. Configure the public configs,
    ///     then <see cref="Build" />. Pass the same <see cref="Save" /> to a new instance to simulate a relaunch.
    /// </summary>
    internal sealed class TestGame : IDisposable
    {
        public const long Day0 = 20000 * TimeUnits.SecondsPerDay;

        private readonly List<Object> _assets = new();

        public TestGame(InMemorySaveProvider save = null, ManualTimeService time = null)
        {
            Save = save ?? new InMemorySaveProvider();
            Time = time ?? new ManualTimeService(Day0);

            Economy = Create<EconomyConfig>();
            Economy.currencies.Add(new CurrencyDefinition { id = "coin", initialBalance = 1000 });
            Economy.currencies.Add(new CurrencyDefinition { id = "gem", initialBalance = 100 });

            Levels = Create<LevelDatabase>();
            Levels.levels.Add(Level("L1", new[] { 1, 1, 2, 2 }, 4, 1, ObjectiveDefinition.ClearBoard()));
            Levels.levels.Add(Level("L2", new[] { 1, 1, 2, 2 }, 4, 1, ObjectiveDefinition.ClearBoard()));

            Puzzle = Create<PuzzleGameplayConfig>();
            Puzzle.firstClearReward = RewardBundle.Of(RewardItem.Currency("coin", 50));
            Puzzle.reviveCost = new Cost("coin", 100);
            Puzzle.reviveMoves = 3;
            Puzzle.maxRevivesPerLevel = 1;

            Shop = Create<ShopConfig>();
            Profile = Create<ProfileFeatureConfig>();
            DailyReward = Create<DailyRewardConfig>();
        }

        public InMemorySaveProvider Save { get; }
        public ManualTimeService Time { get; }
        public FakeIapProvider Iap { get; } = new();
        public FeatureToggles Toggles { get; } = new();

        public EconomyConfig Economy { get; }
        public LevelDatabase Levels { get; }
        public PuzzleGameplayConfig Puzzle { get; }
        public ShopConfig Shop { get; }
        public ProfileFeatureConfig Profile { get; }
        public DailyRewardConfig DailyReward { get; }

        public ModuleHost Host { get; private set; }
        public IServiceResolver Services { get; private set; }

        public IEconomyService Wallet => Get<IEconomyService>();
        public IInventoryService Inventory => Get<IInventoryService>();
        public IProgressionService Progression => Get<IProgressionService>();

        public TestGame Build(bool withIap = true)
        {
            Host = new ModuleHost()
                .AddModule(new CoreModule(Time, new EventBus(), Toggles))
                .AddModule(new SaveModule(Save, new JsonUtilitySerializer()))
                .AddModule(new EconomyModule(Economy))
                .AddModule(new InventoryModule())
                .AddModule(new RewardModule())
                .AddModule(new ProgressionModule())
                .AddModule(new ProfileModule(() => "Player1234"))
                .AddModule(new LevelModule(Levels))
                .AddModule(new PuzzleModule());
            if (withIap) Host.AddModule(new IapModule(Iap));

            Host.AddFeature(new PuzzleGameplayFeature(Puzzle))
                .AddFeature(new ShopFeature(Shop))
                .AddFeature(new ProfileFeature(Profile))
                .AddFeature(new DailyRewardFeature(DailyReward));

            Services = Host.Build();
            return this;
        }

        public T Get<T>() where T : class => Services.Require<T>();

        public static LevelDefinition Level(string id, int[] tiles, int width, int height, params ObjectiveDefinition[] objectives) =>
            new()
            {
                id = id, width = width, height = height, tiles = tiles, moveLimit = 10,
                objectives = new List<ObjectiveDefinition>(objectives)
            };

        public void Dispose()
        {
            Host?.Dispose();
            foreach (var asset in _assets) Object.DestroyImmediate(asset);
        }

        private T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
