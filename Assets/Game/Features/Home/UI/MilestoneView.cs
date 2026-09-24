using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Home
{
    /// <summary>A "LEVEL N" badge beside the level path: something that unlocks when the player gets there.</summary>
    public sealed class MilestoneView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text iconFallback;
        [SerializeField] private TMP_Text levelText;

        public void Bind(string id, int levelNumber, UIIconSet icons)
        {
            gameObject.SetActive(true);
            UIIconSet.Show(icons, icon, iconFallback, id);
            levelText.text = $"LEVEL {levelNumber}";
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
