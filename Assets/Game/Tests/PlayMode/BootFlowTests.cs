using System;
using System.Collections;
using System.Linq;
using NinetyNine.Core;
using NinetyNine.Features.DailyReward.UI;
using NinetyNine.Features.Profile;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.Features.PuzzleGameplay.UI;
using NinetyNine.Features.Shop;
using NinetyNine.Features.Shop.UI;
using NinetyNine.Game;
using NinetyNine.Game.Flow;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Progression;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Moves;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Puzzle.Rules;
using NinetyNine.Persistence;
using NinetyNine.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NinetyNine.Tests
{
    /// <summary>
    ///     Real boot with real transitions, on an in-memory save (never the developer's PlayerPrefs):
    ///     Intro → services → UI root → Home scene, then every screen/popup through the navigator, and the Home ⇄
    ///     Gameplay scene round trip.
    /// </summary>
    public class BootFlowTests
    {
        private static IServiceResolver Services => ServiceLocator.Current;
        private static INavigator Navigator => Services?.Require<INavigator>();
        private static string ActiveScene => SceneManager.GetActiveScene().name;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            foreach (var scene in new[] { GameScenes.Intro, GameScenes.Home, GameScenes.Gameplay })
                if (!Application.CanStreamedLevelBeLoaded(scene)) Assert.Ignore($"{scene} is not in Build Settings - run NinetyNine/Setup.");

            GameBootstrap.SaveProviderOverride = new InMemorySaveProvider();
            SceneManager.LoadScene(GameScenes.Intro);
            yield return Until(() => Navigator?.CurrentScreen?.Key == "home" && ActiveScene == GameScenes.Home);

            // Home may auto-open the daily popup when a claim is available; start from a clean state.
            if (Navigator.PopupCount > 0) Navigator.ClosePopup();
            yield return Until(() => Navigator.PopupCount == 0);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameBootstrap.SaveProviderOverride = null;
            var bootstrap = Object.FindObjectOfType<GameBootstrap>();
            if (bootstrap) Object.Destroy(bootstrap.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Home_navigates_to_shop_and_daily_and_back()
        {
            var coins = Services.Require<IEconomyService>().GetBalance("coin");
            Assert.That(GameObject.Find("CoinText").GetComponent<TMP_Text>().text, Is.EqualTo(coins.ToString("N0")));
            Assert.That(GameObject.Find("LivesStatusText").GetComponent<TMP_Text>().text, Is.EqualTo("MAX"));

            Click("ShopTab");
            yield return Until(() => Navigator.CurrentScreen.Key == "shop");
            var shop = (ShopScreen)Navigator.CurrentScreen;
            var items = shop.GetComponentsInChildren<ShopProductItem>(true);
            Assert.That(items, Has.Length.EqualTo(Services.Require<IShopService>().Products.Count));
            Assert.That(items.SelectMany(i => i.GetComponentsInChildren<TMP_Text>(true)).Select(t => t.text), Has.Member("600 coin"));

            Assert.That(GameObject.Find("MainHud(Clone)"), Is.Not.Null, "HUD stays up on the Shop tab");
            Click("StartTab");
            yield return Until(() => Navigator.CurrentScreen.Key == "home");
            Assert.That(shop.gameObject.activeSelf, Is.False);

            Click("ShopTab");
            yield return Until(() => Navigator.CurrentScreen.Key == "shop");
            Assert.That(Navigator.HandleBack(), Is.True);
            yield return Until(() => Navigator.CurrentScreen.Key == "home");

            Click("DailyButton");
            yield return Until(() => Navigator.TopPopup?.Key == "daily_reward");
            var daily = (DailyRewardPopup)Navigator.TopPopup;
            Assert.That(daily.GetComponentsInChildren<DayCellView>(true), Has.Length.EqualTo(7));

            Assert.That(Navigator.HandleBack(), Is.True);
            yield return Until(() => Navigator.PopupCount == 0);
            Assert.That(Navigator.HandleBack(), Is.False, "Home is the root");
        }

        [UnityTest]
        public IEnumerator Level_is_played_to_the_end_through_real_taps_and_returns_home()
        {
            var gameplay = Services.Require<IPuzzleGameplayService>();
            var rules = Services.Require<IPuzzleSessionFactory>().Rules;
            var coinsBefore = Services.Require<IEconomyService>().GetBalance("coin");

            Click("PlayButton");
            yield return Until(() => Navigator.CurrentScreen.Key == PuzzleGameplayFeature.Id && ActiveScene == GameScenes.Gameplay);
            Assert.That(GameObject.Find("MainHud(Clone)"), Is.Null, "HUD hides during a level");
            var session = gameplay.ActiveSession;
            var board = Object.FindObjectOfType<BoardView>();
            Assert.That(board.GetComponentsInChildren<Button>(), Has.Length.EqualTo(session.Board.Width * session.Board.Height));

            // Tap the first legal cell until the level ends; decline revives so a lose reaches the final popup.
            for (var guard = 0; guard < 200; guard++)
            {
                var popup = Navigator.TopPopup;
                if (popup?.Key == LevelEndKeys.Win) break;
                if (popup?.Key == LevelEndKeys.Lose)
                {
                    if (session.State != PuzzleSessionState.OutOfMoves) break;
                    Click(popup.transform, "SecondaryButton");
                }
                else
                {
                    var target = session.Board.Positions().First(p => rules.Validate(session.Board, PuzzleMove.Tap(p)) == MoveValidation.Valid);
                    Click(board.transform, $"Cell_{target.X}_{target.Y}");
                }

                yield return Until(() => true);
            }

            var end = Navigator.TopPopup;
            Assert.That(end, Is.Not.Null, "a Win or Lose popup must be showing");
            Assert.That(session.State, Is.EqualTo(PuzzleSessionState.Ended));
            var won = session.Result?.Outcome == LevelOutcome.Win;
            Assert.That(end.Key, Is.EqualTo(won ? LevelEndKeys.Win : LevelEndKeys.Lose));

            Click(end.transform, won ? "HomeButton" : "SecondaryButton");
            yield return Until(() => Navigator.CurrentScreen.Key == "home" && Navigator.PopupCount == 0 && ActiveScene == GameScenes.Home);
            Assert.That(Navigator.ScreenCount, Is.EqualTo(1), "the Home scene resets the stack to its root screen");

            var progression = Services.Require<IProgressionService>();
            Assert.That(progression.CurrentLevelNumber, Is.EqualTo(won ? 2 : 1));
            Assert.That(Services.Require<IEconomyService>().GetBalance("coin"), Is.EqualTo(won ? coinsBefore + 20 : coinsBefore));
        }

        [UnityTest]
        public IEnumerator Edit_profile_saves_name_and_avatar()
        {
            Click("AvatarButton");
            yield return Until(() => Navigator.TopPopup?.Key == ProfileFeature.Id);
            var popup = Navigator.TopPopup.transform;

            popup.GetComponentInChildren<TMP_InputField>().text = "Tester";
            Click(popup, "Avatar_avatar_02");
            Click(popup, "SaveButton");
            yield return Until(() => Navigator.PopupCount == 0);

            var profile = Services.Require<IProfileService>();
            Assert.That(profile.DisplayName, Is.EqualTo("Tester"));
            Assert.That(profile.AvatarId, Is.EqualTo("avatar_02"));
        }

        private static void Click(string name) => GameObject.Find(name).GetComponent<Button>().onClick.Invoke();

        private static void Click(Transform root, string name) =>
            root.GetComponentsInChildren<Button>(true).First(b => b.name == name).onClick.Invoke();

        private static IEnumerator Until(Func<bool> condition, float timeout = 5)
        {
            yield return null;
            var end = Time.realtimeSinceStartup + timeout;
            while (!(condition() && Navigator is { IsBusy: false }))
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for navigation.");
                yield return null;
            }
        }
    }
}
