using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Unlock;
using NinetyNine.Persistence;

namespace NinetyNine.Features.Profile
{
    [Serializable]
    internal sealed class ProfileFeatureState
    {
        public int renameCount;
    }

    internal sealed class ProfileFeatureService : IProfileFeatureService, IInitializable
    {
        private const string SaveKey = "feature.profile";
        private const string RenameSource = "profile_rename";

        private readonly ProfileFeatureConfig _config;
        private readonly IEconomyService _economy;
        private readonly IProfileService _profile;
        private readonly ISaveService _save;
        private readonly UnlockEvaluator _unlocks;
        private ProfileFeatureState _state = new();

        public ProfileFeatureService(ProfileFeatureConfig config, IProfileService profile, IEconomyService economy,
            UnlockEvaluator unlocks, ISaveService save)
        {
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _profile = profile;
            _economy = economy;
            _unlocks = unlocks;
            _save = save;
        }

        public void Initialize()
        {
            _state = _save.Load<ProfileFeatureState>(SaveKey);

            if (string.IsNullOrEmpty(_profile.AvatarId)) SelectFirstUnlocked(_config.avatars, id => _profile.SetAvatar(id));
            if (string.IsNullOrEmpty(_profile.TitleId)) SelectFirstUnlocked(_config.titles, id => _profile.SetTitle(id));
        }

        public string DisplayName => _profile.DisplayName;

        public Cost NextRenameCost => _state.renameCount < _config.freeRenames ? default : _config.renameCost;

        public IReadOnlyList<ProfileOptionView> GetAvatars() => BuildViews(_config.avatars, _profile.AvatarId);

        public IReadOnlyList<ProfileOptionView> GetTitles() => BuildViews(_config.titles, _profile.TitleId);

        public Result TryRename(string newName)
        {
            var validation = _profile.ValidateDisplayName(newName);
            if (!validation.IsSuccess) return validation;
            if (ProfileRules.NormalizeName(newName) == _profile.DisplayName) return Result.Fail(ProfileFeatureErrors.SameName);

            var spend = _economy.TrySpend(NextRenameCost, RenameSource);
            if (!spend.IsSuccess) return spend;

            _profile.SetDisplayName(newName);
            _state.renameCount++;
            _save.Save(SaveKey, _state);
            return Result.Success;
        }

        public Result TrySelectAvatar(string avatarId) =>
            TrySelect(_config.FindAvatar(avatarId), id => _profile.SetAvatar(id));

        public Result TrySelectTitle(string titleId) =>
            TrySelect(_config.FindTitle(titleId), id => _profile.SetTitle(id));

        private Result TrySelect(ProfileOption option, Func<string, Result> apply)
        {
            if (option == null) return Result.Fail(ProfileFeatureErrors.UnknownOption);
            if (!_unlocks.IsMet(option.unlock)) return Result.Fail(ProfileFeatureErrors.OptionLocked);
            return apply(option.id);
        }

        private void SelectFirstUnlocked(List<ProfileOption> options, Func<string, Result> apply)
        {
            var first = options.Find(o => _unlocks.IsMet(o.unlock));
            if (first != null) apply(first.id);
        }

        private List<ProfileOptionView> BuildViews(List<ProfileOption> options, string selectedId)
        {
            var views = new List<ProfileOptionView>(options.Count);
            foreach (var option in options)
                views.Add(new ProfileOptionView(option, _unlocks.IsMet(option.unlock), option.id == selectedId));
            return views;
        }
    }
}
