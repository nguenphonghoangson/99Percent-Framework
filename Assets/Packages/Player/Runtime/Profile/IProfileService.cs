using NinetyNine.Core;

namespace NinetyNine.Modules.Profile
{
    /// <summary>
    ///     Name / avatar / title of the local player. Holds the values and their invariants only — which
    ///     avatars exist, how they unlock and what a rename costs is design data owned by the Profile feature.
    /// </summary>
    public interface IProfileService
    {
        string DisplayName { get; }

        string AvatarId { get; }

        string TitleId { get; }

        /// <summary>Normalises then checks <see cref="ProfileRules" /> and the optional name filter.</summary>
        Result ValidateDisplayName(string rawName);

        Result SetDisplayName(string rawName);

        Result SetAvatar(string avatarId);

        Result SetTitle(string titleId);
    }

    /// <summary>Optional profanity/blocklist hook. Register one before ProfileModule to enable it.</summary>
    public interface IProfileNameFilter
    {
        bool IsAllowed(string normalizedName);
    }

    public static class ProfileErrors
    {
        public const string NameTooShort = "profile.name_too_short";
        public const string NameTooLong = "profile.name_too_long";
        public const string NameInvalidCharacters = "profile.name_invalid_characters";
        public const string NameRejected = "profile.name_rejected";
        public const string InvalidId = "profile.invalid_id";
    }

    public readonly struct ProfileChangedEvent
    {
        public ProfileChangedEvent(string displayName, string avatarId, string titleId)
        {
            DisplayName = displayName;
            AvatarId = avatarId;
            TitleId = titleId;
        }

        public string DisplayName { get; }
        public string AvatarId { get; }
        public string TitleId { get; }
    }
}
