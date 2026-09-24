using System;
using NinetyNine.Core;
using NinetyNine.Persistence;

namespace NinetyNine.Modules.Profile
{
    [Serializable]
    internal sealed class PlayerProfile
    {
        public string displayName;
        public string avatarId;
        public string titleId;
    }

    internal sealed class ProfileService : IProfileService, IInitializable
    {
        private const string SaveKey = "module.profile";

        private readonly Func<string> _defaultName;
        private readonly IEventBus _events;
        private readonly IProfileNameFilter _filter;
        private readonly ISaveService _save;
        private PlayerProfile _profile = new();

        public ProfileService(ISaveService save, IEventBus events, IProfileNameFilter filter, Func<string> defaultName)
        {
            _save = save;
            _events = events;
            _filter = filter;
            _defaultName = defaultName;
        }

        public void Initialize()
        {
            _profile = _save.Load<PlayerProfile>(SaveKey);
            if (!string.IsNullOrEmpty(_profile.displayName)) return;

            _profile.displayName = _defaultName();
            _save.Save(SaveKey, _profile);
        }

        public string DisplayName => _profile.displayName;

        public string AvatarId => _profile.avatarId;

        public string TitleId => _profile.titleId;

        public Result ValidateDisplayName(string rawName)
        {
            var name = ProfileRules.NormalizeName(rawName);
            var result = ProfileRules.ValidateName(name);
            if (!result.IsSuccess) return result;

            return _filter == null || _filter.IsAllowed(name) ? Result.Success : Result.Fail(ProfileErrors.NameRejected);
        }

        public Result SetDisplayName(string rawName)
        {
            var result = ValidateDisplayName(rawName);
            if (!result.IsSuccess) return result;

            _profile.displayName = ProfileRules.NormalizeName(rawName);
            Commit();
            return Result.Success;
        }

        public Result SetAvatar(string avatarId)
        {
            if (string.IsNullOrEmpty(avatarId)) return Result.Fail(ProfileErrors.InvalidId);

            _profile.avatarId = avatarId;
            Commit();
            return Result.Success;
        }

        public Result SetTitle(string titleId)
        {
            if (string.IsNullOrEmpty(titleId)) return Result.Fail(ProfileErrors.InvalidId);

            _profile.titleId = titleId;
            Commit();
            return Result.Success;
        }

        private void Commit()
        {
            _save.Save(SaveKey, _profile);
            _events.Publish(new ProfileChangedEvent(_profile.displayName, _profile.avatarId, _profile.titleId));
        }
    }
}
