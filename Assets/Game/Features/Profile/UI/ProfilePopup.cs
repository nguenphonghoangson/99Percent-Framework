using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Profile;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Profile.UI
{
    /// <summary>
    ///     "Edit Profile", opened under <see cref="ProfileFeature.Id" />. Avatar picks and the name edit stay
    ///     pending until Save, which applies them through <see cref="ProfilePresenter.Save" /> and closes on
    ///     success; closing any other way discards them.
    /// </summary>
    public sealed class ProfilePopup : UIPopup, IProfileView
    {
        [SerializeField] private Image previewIcon;
        [SerializeField] private TMP_Text previewFallback;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private RectTransform grid;
        [SerializeField] private ProfileAvatarOption optionTemplate;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private UIIconSet icons;

        private readonly List<ProfileAvatarOption> _options = new();
        private IReadOnlyList<ProfileOptionView> _avatars = new List<ProfileOptionView>();
        private bool _nameEdited;
        private string _pendingAvatar;
        private ProfilePresenter _presenter;

        private void Awake()
        {
            optionTemplate.gameObject.SetActive(false);
            nameInput.characterLimit = ProfileRules.MaxNameLength;
            nameInput.onValueChanged.AddListener(_ => _nameEdited = true);
            saveButton.onClick.AddListener(OnSave);
            closeButton.onClick.AddListener(() => Close());
        }

        private void OnEnable()
        {
            var services = ServiceLocator.Current;
            if (services == null || !services.TryGet<IProfileFeatureService>(out var profile))
            {
                Debug.LogWarning("[ProfilePopup] Profile feature unavailable - start from the Intro scene.", this);
                return;
            }

            _pendingAvatar = null;
            _nameEdited = false;
            errorText.text = string.Empty;
            _presenter = new ProfilePresenter(profile, services.Require<IEventBus>(), this);
        }

        private void OnDisable()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        public void Render(string displayName, IReadOnlyList<ProfileOptionView> avatars,
            IReadOnlyList<ProfileOptionView> titles, Cost nextRenameCost)
        {
            _avatars = avatars;
            if (_pendingAvatar == null)
                foreach (var avatar in avatars)
                    if (avatar.IsSelected)
                        _pendingAvatar = avatar.Id;

            if (!_nameEdited) nameInput.SetTextWithoutNotify(displayName);
            hintText.text = nextRenameCost.IsFree ? "First rename is free" : $"Rename costs {nextRenameCost}";
            RenderAvatars();
        }

        public void ShowError(string errorCode) => errorText.text = errorCode switch
        {
            ProfileErrors.NameTooShort => $"Name needs at least {ProfileRules.MinNameLength} characters.",
            ProfileErrors.NameTooLong => $"Name can have at most {ProfileRules.MaxNameLength} characters.",
            ProfileErrors.NameInvalidCharacters => "Letters, numbers, spaces and _ only.",
            ProfileErrors.NameRejected => "That name is not allowed.",
            ProfileFeatureErrors.OptionLocked => "That avatar is still locked.",
            EconomyErrors.InsufficientFunds => "Not enough currency to rename.",
            _ => $"Could not save ({errorCode})."
        };

        private void RenderAvatars()
        {
            while (_options.Count < _avatars.Count)
            {
                var option = Instantiate(optionTemplate, grid);
                option.gameObject.SetActive(true);
                _options.Add(option);
            }

            for (var i = 0; i < _options.Count; i++)
            {
                var visible = i < _avatars.Count;
                _options[i].gameObject.SetActive(visible);
                if (visible) _options[i].Bind(_avatars[i], _avatars[i].Id == _pendingAvatar, icons, OnAvatarClicked);
            }

            UIIconSet.Show(icons, previewIcon, previewFallback, _pendingAvatar ?? string.Empty);
        }

        private void OnAvatarClicked(string avatarId)
        {
            foreach (var avatar in _avatars)
            {
                if (avatar.Id != avatarId) continue;
                if (!avatar.IsUnlocked)
                {
                    var hint = ProfileAvatarOption.UnlockHint(avatar.Unlock);
                    errorText.text = string.IsNullOrEmpty(hint) ? "Locked." : $"Locked - {hint}";
                    return;
                }
            }

            errorText.text = string.Empty;
            _pendingAvatar = avatarId;
            RenderAvatars();
        }

        private void OnSave()
        {
            errorText.text = string.Empty;
            if (_presenter != null && _presenter.Save(nameInput.text, _pendingAvatar)) Close();
        }
    }
}
