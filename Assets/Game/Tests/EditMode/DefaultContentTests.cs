using NUnit.Framework;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.Shop;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Levels;
using NinetyNine.Modules.Puzzle.Rules;
using NinetyNine.Modules.Reward;
using NinetyNine.Persistence;
using UnityEditor;
using UnityEngine;

namespace NinetyNine.Tests
{
    /// <summary>
    ///     Guards the shipped config assets: they boot the full graph, every level is valid and playable, and
    ///     every payout is grantable. Catches a designer typo ("con" instead of "coin") before a build does.
    /// </summary>
    public class DefaultContentTests
    {
        private const string Content = "Assets/Game/Content/";

        private static T Load<T>(string file) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Content + file);
            if (!asset) Assert.Ignore($"{file} not created yet — run NinetyNine/Setup.");
            return asset;
        }

        private static ModuleHost Boot(out IServiceResolver services)
        {
            var host = new ModuleHost()
                .AddModule(new CoreModule(new ManualTimeService()))
                .AddModule(new SaveModule(new InMemorySaveProvider(), new JsonUtilitySerializer()))
                .AddModule(new EconomyModule(Load<EconomyConfig>("Economy/EconomyConfig.asset")))
                .AddModule(new InventoryModule())
                .AddModule(new RewardModule())
                .AddModule(new ProgressionModule())
                .AddModule(new ProfileModule())
                .AddModule(new LevelModule(Load<LevelDatabase>("Levels/LevelDatabase.asset")))
                .AddModule(new PuzzleModule())
                .AddFeature(new PuzzleGameplayFeature(Load<PuzzleGameplayConfig>("Gameplay/PuzzleGameplayConfig.asset")))
                .AddFeature(new ShopFeature(Load<ShopConfig>("Shop/ShopConfig.asset")))
                .AddFeature(new ProfileFeature(Load<ProfileFeatureConfig>("Profile/ProfileFeatureConfig.asset")))
                .AddFeature(new DailyRewardFeature(Load<DailyRewardConfig>("DailyReward/DailyRewardConfig.asset")));
            services = host.Build();
            return host;
        }

        [Test]
        public void Every_level_is_valid_and_starts_with_a_legal_move()
        {
            var database = Load<LevelDatabase>("Levels/LevelDatabase.asset");
            var rules = new TapClearRules();

            Assert.That(database.levels, Is.Not.Empty);
            foreach (var level in database.levels)
            {
                Assert.That(level.Validate(), Is.Null);
                Assert.That(rules.HasAnyValidMove(level.CreateBoard()), Is.True, level.id);
            }
        }

        [Test]
        public void Every_shop_and_daily_payout_is_grantable()
        {
            using var host = Boot(out var services);
            var rewards = services.Require<IRewardService>();
            var economy = services.Require<IEconomyService>();

            foreach (var product in Load<ShopConfig>("Shop/ShopConfig.asset").products)
            {
                Assert.That(rewards.Validate(product.rewards).Error, Is.Null, product.id);
                if (product.priceKind == PriceKind.Currency)
                    Assert.That(economy.IsKnown(product.cost.currencyId), Is.True, product.id);
            }

            foreach (var day in Load<DailyRewardConfig>("DailyReward/DailyRewardConfig.asset").days)
                Assert.That(rewards.Validate(day).Error, Is.Null);

            Assert.That(rewards.Validate(Load<PuzzleGameplayConfig>("Gameplay/PuzzleGameplayConfig.asset").firstClearReward).Error, Is.Null);
        }

        [Test]
        public void Default_configs_boot_every_feature_and_first_level_starts()
        {
            using var host = Boot(out var services);

            Assert.That(host.SkippedFeatures, Is.Empty);
            Assert.That(services.Require<IPuzzleGameplayService>().StartCurrentLevel().State, Is.EqualTo(PuzzleSessionState.Playing));
            Assert.That(services.Require<IProfileService>().AvatarId, Is.EqualTo("avatar_01"));
        }

        [Test]
        public void Intro_scene_has_every_config_assigned()
        {
            const string scenePath = "Assets/Game/Scenes/Intro.unity";
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)) Assert.Ignore("Intro scene not created yet.");

            var dependencies = AssetDatabase.GetDependencies(scenePath, false);
            foreach (var file in new[] { "Economy/EconomyConfig", "Levels/LevelDatabase", "Gameplay/PuzzleGameplayConfig",
                         "Shop/ShopConfig", "Profile/ProfileFeatureConfig", "DailyReward/DailyRewardConfig" })
                Assert.That(dependencies, Has.Member(Content + file + ".asset"));
        }
    }
}
