using NUnit.Framework;
using NinetyNine.Features.Profile;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Unlock;

namespace NinetyNine.Tests
{
    public class ProfileFeatureTests
    {
        private static TestGame Game()
        {
            var game = new TestGame();
            game.Profile.avatars.Add(new ProfileOption { id = "cat" });
            game.Profile.avatars.Add(new ProfileOption { id = "dog", unlock = UnlockCondition.AtLevel(3) });
            game.Profile.avatars.Add(new ProfileOption { id = "crown", unlock = UnlockCondition.WithItem("avatar_crown") });
            game.Profile.freeRenames = 1;
            game.Profile.renameCost = new Cost("gem", 50);
            return game.Build();
        }

        [Test]
        public void Fresh_profile_gets_the_first_unlocked_avatar()
        {
            using var game = Game();
            Assert.That(game.Get<IProfileService>().AvatarId, Is.EqualTo("cat"));
        }

        [Test]
        public void First_rename_is_free_then_it_costs()
        {
            using var game = Game();
            var profile = game.Get<IProfileFeatureService>();

            Assert.That(profile.TryRename("Sơn Nguyễn").IsSuccess, Is.True);
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(100));

            Assert.That(profile.TryRename("Percas").IsSuccess, Is.True);
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(50));
            Assert.That(profile.DisplayName, Is.EqualTo("Percas"));
        }

        [Test]
        public void Invalid_name_costs_nothing()
        {
            using var game = Game();
            var profile = game.Get<IProfileFeatureService>();
            profile.TryRename("Valid Name");

            Assert.That(profile.TryRename("x").Error, Is.EqualTo(ProfileErrors.NameTooShort));
            Assert.That(game.Wallet.GetBalance("gem"), Is.EqualTo(100));
        }

        [Test]
        public void Locked_avatars_unlock_by_level_and_item()
        {
            using var game = Game();
            var profile = game.Get<IProfileFeatureService>();

            Assert.That(profile.TrySelectAvatar("dog").Error, Is.EqualTo(ProfileFeatureErrors.OptionLocked));
            Assert.That(profile.TrySelectAvatar("crown").Error, Is.EqualTo(ProfileFeatureErrors.OptionLocked));

            game.Progression.CompleteLevel(0);
            game.Progression.CompleteLevel(1);
            game.Inventory.Add("avatar_crown", 1, "test");

            Assert.That(profile.TrySelectAvatar("dog").IsSuccess, Is.True);
            Assert.That(profile.TrySelectAvatar("crown").IsSuccess, Is.True);
            Assert.That(profile.GetAvatars()[2].IsSelected, Is.True);
        }

        [Test]
        public void Unknown_avatar_is_refused()
        {
            using var game = Game();
            Assert.That(game.Get<IProfileFeatureService>().TrySelectAvatar("dragon").Error,
                Is.EqualTo(ProfileFeatureErrors.UnknownOption));
        }
    }
}
