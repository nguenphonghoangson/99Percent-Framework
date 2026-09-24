using System;
using NinetyNine.Modules.Unlock;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Profile.UI
{
    /// <summary>One avatar tile: icon, selected frame, lock overlay with its unlock hint.</summary>
    public sealed class ProfileAvatarOption : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text iconFallback;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private TMP_Text lockText;

        private Action<string> _onClick;

        public string Id { get; private set; }

        private void Awake() => button.onClick.AddListener(() => _onClick?.Invoke(Id));

        public void Bind(ProfileOptionView option, bool selected, UIIconSet icons, Action<string> onClick)
        {
            Id = option.Id;
            _onClick = onClick;
            name = "Avatar_" + option.Id;
            UIIconSet.Show(icons, icon, iconFallback, option.Id);
            selectedFrame.SetActive(selected);
            lockOverlay.SetActive(!option.IsUnlocked);
            lockText.text = UnlockHint(option.Unlock);
        }

        public static string UnlockHint(UnlockCondition unlock) => unlock.kind switch
        {
            UnlockKind.ReachLevel => $"Lv {unlock.levelNumber}",
            UnlockKind.OwnItem => "Shop",
            _ => string.Empty
        };
    }
}
