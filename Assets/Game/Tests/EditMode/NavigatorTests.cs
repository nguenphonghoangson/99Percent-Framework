using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NinetyNine.Core;
using NinetyNine.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NinetyNine.Tests
{
    /// <summary>
    ///     Navigator logic on real GameObjects. Views have no transition, so every operation completes
    ///     synchronously — no frames needed.
    /// </summary>
    public class NavigatorTests
    {
        private readonly List<Object> _objects = new();
        private readonly List<UIViewOpenedEvent> _opened = new();
        private ModuleHost _host;
        private INavigator _nav;
        private IUIFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _opened.Clear();
            var catalog = ScriptableObject.CreateInstance<UICatalog>();
            _objects.Add(catalog);
            catalog.entries.Add(Entry<TestScreen>("home", true));
            catalog.entries.Add(Entry<TestScreen>("shop", true));
            catalog.entries.Add(Entry<TestScreen>("level", false));
            catalog.entries.Add(Entry<TestPopup>("popup", true));
            catalog.entries.Add(Entry<TestPopup>("sticky", true, closeOnBack: false));

            var events = new EventBus();
            events.Subscribe<UIViewOpenedEvent>(_opened.Add);
            _host = new ModuleHost().AddModule(new CoreModule(new ManualTimeService(), events)).AddModule(new UIModule(catalog));
            var services = _host.Build();
            _nav = services.Require<INavigator>();
            _factory = services.Require<IUIFactory>();
        }

        [TearDown]
        public void TearDown()
        {
            _host.Dispose();
            foreach (var o in _objects) Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private UICatalogEntry Entry<T>(string key, bool keep, bool closeOnBack = true) where T : UIView
        {
            var template = new GameObject(key + "_template", typeof(RectTransform));
            template.SetActive(false);
            _objects.Add(template);
            var view = template.AddComponent<T>();
            if (!closeOnBack)
            {
                var serialized = new SerializedObject(view);
                serialized.FindProperty("closeOnBack").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return new UICatalogEntry { key = key, prefab = view, keepInstance = keep };
        }

        private UICatalog Catalog(params UICatalogEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<UICatalog>();
            _objects.Add(catalog);
            catalog.entries.AddRange(entries);
            return catalog;
        }

        private static void Done(Task task)
        {
            Assert.That(task.IsCompleted, Is.True, "operation should complete synchronously without transitions");
            task.GetAwaiter().GetResult();
        }

        private static T Done<T>(Task<T> task)
        {
            Assert.That(task.IsCompleted, Is.True, "operation should complete synchronously without transitions");
            return task.GetAwaiter().GetResult();
        }

        [Test]
        public void Push_covers_previous_and_pop_reveals_it()
        {
            var home = (TestScreen)Done(_nav.PushScreen("home"));
            var shop = (TestScreen)Done(_nav.PushScreen("shop", 42));

            Assert.That(_nav.CurrentScreen, Is.SameAs(shop));
            Assert.That(home.gameObject.activeSelf, Is.False);
            Assert.That(shop.Args, Is.EqualTo(42));
            Assert.That(home.Log, Is.EqualTo(new[] { "created", "opening", "opened", "covered" }));

            Assert.That(Done(_nav.PopScreen()), Is.True);

            Assert.That(_nav.CurrentScreen, Is.SameAs(home));
            Assert.That(home.gameObject.activeSelf, Is.True);
            Assert.That(home.Log[home.Log.Count - 1], Is.EqualTo("revealed"));
            Assert.That(shop.Log, Is.EqualTo(new[] { "created", "opening", "opened", "closing", "closed" }));
        }

        [Test]
        public void Root_screen_cannot_be_popped()
        {
            Done(_nav.PushScreen("home"));

            Assert.That(Done(_nav.PopScreen()), Is.False);
            Assert.That(_nav.ScreenCount, Is.EqualTo(1));
        }

        [Test]
        public void Back_closes_popup_then_pops_screen_then_reports_root()
        {
            Done(_nav.PushScreen("home"));
            Done(_nav.PushScreen("shop"));
            Done(_nav.ShowPopup("popup"));

            Assert.That(_nav.HandleBack(), Is.True);
            Assert.That(_nav.PopupCount, Is.Zero);
            Assert.That(_nav.HandleBack(), Is.True);
            Assert.That(_nav.CurrentScreen.Key, Is.EqualTo("home"));
            Assert.That(_nav.HandleBack(), Is.False);
        }

        [Test]
        public void Popup_that_forbids_back_stays_open_but_swallows_the_press()
        {
            Done(_nav.PushScreen("home"));
            Done(_nav.PushScreen("shop"));
            Done(_nav.ShowPopup("sticky"));

            Assert.That(_nav.HandleBack(), Is.True);
            Assert.That(_nav.TopPopup.Key, Is.EqualTo("sticky"));
            Assert.That(_nav.CurrentScreen.Key, Is.EqualTo("shop"));
        }

        [Test]
        public void Popup_hands_its_result_to_the_caller()
        {
            Done(_nav.PushScreen("home"));
            var result = _nav.ShowPopupAndWait("popup");
            Assert.That(result.IsCompleted, Is.False);

            Assert.That(_nav.ClosePopup(null, "confirmed").IsCompleted, Is.True);

            Assert.That(Done(result), Is.EqualTo("confirmed"));
        }

        [Test]
        public void Popups_stack_and_close_top_first()
        {
            Done(_nav.PushScreen("home"));
            var first = Done(_nav.ShowPopup("popup"));
            var second = Done(_nav.ShowPopup("sticky"));

            Assert.That(_nav.TopPopup, Is.SameAs(second));
            _nav.ClosePopup();
            Assert.That(_nav.TopPopup, Is.SameAs(first));
        }

        [Test]
        public void Pushing_a_screen_closes_open_popups()
        {
            Done(_nav.PushScreen("home"));
            var popup = Done(_nav.ShowPopup("popup"));

            Done(_nav.PushScreen("shop"));

            Assert.That(_nav.PopupCount, Is.Zero);
            Assert.That(popup.Closed.IsCompleted, Is.True);
        }

        [Test]
        public void Replace_swaps_the_top_without_growing_the_stack()
        {
            Done(_nav.PushScreen("home"));
            Done(_nav.PushScreen("level"));

            Done(_nav.ReplaceScreen("shop"));

            Assert.That(_nav.ScreenCount, Is.EqualTo(2));
            Assert.That(_nav.CurrentScreen.Key, Is.EqualTo("shop"));
        }

        [Test]
        public void Pop_to_root_closes_everything_above_home()
        {
            Done(_nav.PushScreen("home"));
            Done(_nav.PushScreen("shop"));
            Done(_nav.PushScreen("level"));
            Done(_nav.ShowPopup("popup"));

            Assert.That(_nav.PopToRoot().IsCompleted, Is.True);

            Assert.That(_nav.ScreenCount, Is.EqualTo(1));
            Assert.That(_nav.PopupCount, Is.Zero);
            Assert.That(_nav.CurrentScreen.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Keep_instance_reuses_the_object_and_others_are_destroyed()
        {
            Done(_nav.PushScreen("home"));
            var shop = (TestScreen)Done(_nav.PushScreen("shop"));
            Done(_nav.PopScreen());
            Assert.That(Done(_nav.PushScreen("shop")), Is.SameAs(shop));
            Done(_nav.PopScreen());

            var level = Done(_nav.PushScreen("level"));
            Done(_nav.PopScreen());
            Assert.That(level == null, Is.True, "non-kept screen is destroyed on close");
            Assert.That(shop.Log, Is.EqualTo(new[] { "created", "opening", "opened", "closing", "closed", "opening", "opened", "closing", "closed" }));
        }

        [Test]
        public void Unknown_key_and_wrong_kind_fail_loudly()
        {
            Assert.That(() => _nav.PushScreen("nope").GetAwaiter().GetResult(), Throws.ArgumentException);
            Assert.That(() => _nav.PushScreen("popup").GetAwaiter().GetResult(), Throws.ArgumentException);
            Assert.That(() => _nav.ShowPopup("home").GetAwaiter().GetResult(), Throws.ArgumentException);
            Assert.That(_nav.ScreenCount, Is.Zero, "a failed open leaves the stack untouched");
        }

        [Test]
        public void Opened_events_carry_key_and_kind()
        {
            Done(_nav.PushScreen("home"));
            Done(_nav.ShowPopup("popup"));

            Assert.That(_opened.Count, Is.EqualTo(2));
            Assert.That(_opened[0].Key, Is.EqualTo("home"));
            Assert.That(_opened[1].Kind, Is.EqualTo(UIViewKind.Popup));
        }

        [Test]
        public void Disposing_the_host_destroys_the_ui_root()
        {
            Done(_nav.PushScreen("home"));
            var root = Object.FindObjectOfType<UIRoot>();
            Assert.That(root, Is.Not.Null);

            _host.Dispose();

            Assert.That(root == null, Is.True);
        }

        [Test]
        public void Context_catalog_keeps_each_scene_ui_to_its_scene()
        {
            var homeUi = Catalog(Entry<TestScreen>("hub", true), Entry<TestPopup>("hub_popup", true));
            var gameplayUi = Catalog(Entry<TestScreen>("level_ui", true));

            _factory.SetContext(homeUi);
            var hub = Done(_nav.ReplaceScreen("hub"));
            var hubPopup = Done(_nav.ShowPopup("hub_popup"));
            Done(_nav.ClosePopup());
            var globalPopup = Done(_nav.ShowPopup("popup"));
            Done(_nav.ClosePopup());
            Assert.That(_factory.Has("level_ui"), Is.False, "Gameplay UI is not reachable from Home");

            _factory.SetContext(gameplayUi);
            Assert.That(hubPopup == null, Is.True, "idle instances of the previous context are destroyed");
            Assert.That(globalPopup == null, Is.False, "global views stay cached across contexts");
            Assert.That(_factory.Has("hub"), Is.False);
            Assert.That(_factory.Has("popup"), Is.True);

            var level = Done(_nav.ReplaceScreen("level_ui"));
            Assert.That(_nav.CurrentScreen, Is.SameAs(level));
            Assert.That(hub == null, Is.True, "an open view of the previous context is destroyed, not cached, once replaced");
            Assert.That(_nav.ScreenCount, Is.EqualTo(1));
        }

        [Test]
        public void Null_context_resolves_from_the_global_catalog_only()
        {
            _factory.SetContext(Catalog(Entry<TestScreen>("hub", true)));
            _factory.SetContext(null);

            Assert.That(_factory.Has("hub"), Is.False);
            Assert.That(Done(_nav.PushScreen("home")).Key, Is.EqualTo("home"));
        }
    }
}
