using System.Globalization;
using System.Linq;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.DailyReward.UI
{
    public sealed class DayCellView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text rewardIconFallback;
        [SerializeField] private UIIconSet icons;
        [SerializeField] private GameObject claimedMark;
        [Tooltip("Optional per-state art; when set it replaces the tint below.")]
        [SerializeField] private Sprite claimedSprite;
        [SerializeField] private Sprite claimableSprite;
        [SerializeField] private Sprite upcomingSprite;
        [SerializeField] private Color claimedColor = new(0.25f, 0.28f, 0.36f);
        [SerializeField] private Color claimableColor = new(0.25f, 0.75f, 0.5f);
        [SerializeField] private Color upcomingColor = new(0.17f, 0.2f, 0.27f);

        public void Bind(DayCell cell)
        {
            dayText.text = $"Day {cell.DayIndex + 1}";
            var items = cell.Reward?.items;
            var first = items is { Count: > 0 } ? items[0].id : string.Empty;
            UIIconSet.Show(icons, rewardIcon, rewardIconFallback, first);
            // Amounts only; the icon says what. "500 +2" for a bundle, whose extra items show in the claim toast.
            rewardText.text = items == null ? string.Empty
                : string.Join(" +", items.Select(i => i.amount.ToString("N0", CultureInfo.InvariantCulture)));
            claimedMark.SetActive(cell.State == DayCellState.Claimed);
            var (sprite, tint) = cell.State switch
            {
                DayCellState.Claimed => (claimedSprite, claimedColor),
                DayCellState.Claimable => (claimableSprite, claimableColor),
                _ => (upcomingSprite, upcomingColor)
            };
            if (sprite) background.sprite = sprite;
            background.color = sprite ? Color.white : tint;
        }
    }
}
