using System;
using System.Collections.Generic;
using NUnit.Framework;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Tests
{
    public class CoreTests
    {
        private sealed class Probe : IInitializable
        {
            public bool Initialized;
            public void Initialize() => Initialized = true;
        }

        private sealed class ProviderModule : IModule
        {
            public string Name => "Provider";
            public void Install(IServiceRegistry registry) => registry.Register(new Probe());
        }

        private sealed class ConsumerFeature : IFeature
        {
            public Probe Seen;
            public string FeatureId => "consumer";
            public string Name => FeatureId;
            public void Install(IServiceRegistry registry) => Seen = registry.Require<Probe>();
        }

        private static CoreModule Core(IFeatureToggles toggles = null) => new(new ManualTimeService(), null, toggles);

        [Test]
        public void Features_install_after_modules_whatever_the_add_order()
        {
            var feature = new ConsumerFeature();
            using var host = new ModuleHost().AddFeature(feature).AddModule(Core()).AddModule(new ProviderModule());

            host.Build();

            Assert.That(feature.Seen, Is.Not.Null);
            Assert.That(feature.Seen.Initialized, Is.True);
        }

        [Test]
        public void Disabled_feature_is_not_installed()
        {
            var feature = new ConsumerFeature();
            using var host = new ModuleHost()
                .AddModule(Core(new FeatureToggles().Set("consumer", false)))
                .AddModule(new ProviderModule())
                .AddFeature(feature);

            host.Build();

            Assert.That(feature.Seen, Is.Null);
            Assert.That(host.SkippedFeatures, Is.EqualTo(new[] { "consumer" }));
        }

        [Test]
        public void A_feature_cannot_be_added_as_a_module() =>
            Assert.Throws<ArgumentException>(() => new ModuleHost().AddModule(new ConsumerFeature()));

        [Test]
        public void Missing_dependency_fails_at_boot_naming_the_module()
        {
            using var host = new ModuleHost().AddModule(Core()).AddFeature(new ConsumerFeature());

            var error = Assert.Throws<InvalidOperationException>(() => host.Build());
            StringAssert.Contains("consumer", error.Message);
            StringAssert.Contains(nameof(Probe), error.Message);
        }

        [Test]
        public void Throwing_listener_does_not_stop_the_others()
        {
            var errors = new List<Exception>();
            var bus = new EventBus(errors.Add);
            var reached = false;
            bus.Subscribe<int>(_ => throw new InvalidOperationException());
            bus.Subscribe<int>(_ => reached = true);

            bus.Publish(1);

            Assert.That(reached, Is.True);
            Assert.That(errors, Has.Count.EqualTo(1));
        }

        [Test]
        public void Unreadable_save_falls_back_to_fresh_state_and_keeps_the_blob()
        {
            var provider = new InMemorySaveProvider();
            provider.Write("key", "{not json");
            using var host = new ModuleHost()
                .AddModule(new CoreModule(new ManualTimeService()))
                .AddModule(new SaveModule(provider, new JsonUtilitySerializer()));
            var save = host.Build().Require<ISaveService>();

            var state = save.Load<CounterTable>("key");

            Assert.That(state.entries, Is.Empty);
            Assert.That(provider.TryRead("key.corrupt", out var kept), Is.True);
            Assert.That(kept, Is.EqualTo("{not json"));
        }
    }
}
