using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Unlock;

namespace NinetyNine.Features.Profile
{
    public readonly struct ProfileOptionView
    {
        public ProfileOptionView(ProfileOption option, bool isUnlocked, bool isSelected)
        {
            Id = option.id;
            Unlock = option.unlock;
            IsUnlocked = isUnlocked;
            IsSelected = isSelected;
        }

        public string Id { get; }
        public UnlockCondition Unlock { get; }
        public bool IsUnlocked { get; }
        public bool IsSelected { get; }
    }

    /// <summary>
    ///     Profile screen rules on top of the Profile module: which avatars/titles exist and are unlocked, and
    ///     what a rename costs.
    /// </summary>
    public interface IProfileFeatureService
    {
        string DisplayName { get; }

        IReadOnlyList<ProfileOptionView> GetAvatars();

        IReadOnlyList<ProfileOptionView> GetTitles();

        /// <summary>Free (default <see cref="Cost" />) while free renames remain.</summary>
        Cost NextRenameCost { get; }

        /// <summary>Validates the name before charging, so an invalid name never costs anything.</summary>
        Result TryRename(string newName);

        Result TrySelectAvatar(string avatarId);

        Result TrySelectTitle(string titleId);
    }

    public static class ProfileFeatureErrors
    {
        public const string UnknownOption = "profile_feature.unknown_option";
        public const string OptionLocked = "profile_feature.option_locked";
        public const string SameName = "profile_feature.same_name";
    }
}
