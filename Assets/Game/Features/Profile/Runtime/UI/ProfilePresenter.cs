using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Inventory;
using NinetyNine.Modules.Profile;
using NinetyNine.Modules.Progression;

namespace NinetyNine.Features.Profile.UI
{
    public interface IProfileView
    {
        void Render(string displayName, IReadOnlyList<ProfileOptionView> avatars,
            IReadOnlyList<ProfileOptionView> titles, Cost nextRenameCost);

        void ShowError(string errorCode);
    }

    public sealed class ProfilePresenter : IDisposable
    {
        private readonly IProfileFeatureService _service;
        private readonly List<IDisposable> _subscriptions = new();
        private readonly IProfileView _view;

        public ProfilePresenter(IProfileFeatureService service, IEventBus events, IProfileView view)
        {
            _service = service;
            _view = view;
            _subscriptions.Add(events.Subscribe<ProfileChangedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<ItemChangedEvent>(_ => Refresh()));
            _subscriptions.Add(events.Subscribe<LevelProgressedEvent>(_ => Refresh()));
            Refresh();
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions) subscription.Dispose();
            _subscriptions.Clear();
        }

        public void Refresh() =>
            _view.Render(_service.DisplayName, _service.GetAvatars(), _service.GetTitles(), _service.NextRenameCost);

        public void Rename(string newName) => Report(_service.TryRename(newName));

        public void SelectAvatar(string avatarId) => Report(_service.TrySelectAvatar(avatarId));

        public void SelectTitle(string titleId) => Report(_service.TrySelectTitle(titleId));

        /// <summary>
        ///     Applies an edit screen's pending changes in one go. The avatar is checked first and the rename (the
        ///     step that may cost currency) goes last, so a refused avatar never leaves the player charged for a
        ///     name change. Unchanged values are skipped. False after reporting the first error.
        /// </summary>
        public bool Save(string newName, string avatarId)
        {
            var avatarChanged = false;
            if (!string.IsNullOrEmpty(avatarId))
                foreach (var avatar in _service.GetAvatars())
                {
                    if (avatar.Id != avatarId) continue;
                    if (!avatar.IsUnlocked) return Fail(ProfileFeatureErrors.OptionLocked);
                    avatarChanged = !avatar.IsSelected;
                }

            if (ProfileRules.NormalizeName(newName) != _service.DisplayName)
            {
                var rename = _service.TryRename(newName);
                if (!rename.IsSuccess) return Fail(rename.Error);
            }

            if (avatarChanged)
            {
                var select = _service.TrySelectAvatar(avatarId);
                if (!select.IsSuccess) return Fail(select.Error);
            }

            return true;
        }

        private bool Fail(string error)
        {
            _view.ShowError(error);
            return false;
        }

        private void Report(Result result)
        {
            if (!result.IsSuccess) _view.ShowError(result.Error);
        }
    }
}
