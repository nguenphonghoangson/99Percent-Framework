using System;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Result
{
    /// <summary>
    ///     Opened with <see cref="WinArgs" /> under <see cref="LevelEndKeys.Win" />; closes with
    ///     <see cref="LevelEndChoice.Next" /> or <see cref="LevelEndChoice.Home" /> (back key = Home).
    /// </summary>
    public sealed class WinPopup : UIPopup
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Image[] stars = Array.Empty<Image>();
        [Tooltip("Optional star art; when set it replaces the tints below.")]
        [SerializeField] private Sprite starOnSprite;
        [SerializeField] private Sprite starOffSprite;
        [SerializeField] private Color starOn = new(0.95f, 0.76f, 0.31f);
        [SerializeField] private Color starOff = new(0.2f, 0.23f, 0.3f);
        [SerializeField] private Button nextButton;
        [SerializeField] private Button homeButton;

        private void Awake()
        {
            nextButton.onClick.AddListener(() => Close(LevelEndChoice.Next));
            homeButton.onClick.AddListener(() => Close(LevelEndChoice.Home));
        }

        protected override void OnOpening(object args)
        {
            var win = args as WinArgs ?? throw new ArgumentException($"{nameof(WinPopup)} needs {nameof(WinArgs)}.");

            titleText.text = $"Level {win.Result.LevelIndex + 1} complete!";
            scoreText.text = $"Score {win.Result.Score}";
            for (var i = 0; i < stars.Length; i++)
            {
                var earned = i < win.Result.Stars;
                var sprite = earned ? starOnSprite : starOffSprite;
                if (sprite) stars[i].sprite = sprite;
                stars[i].color = sprite ? Color.white : earned ? starOn : starOff;
            }
            rewardText.text = !win.Reward.IsEmpty ? "+" + win.Reward : win.FirstClear ? string.Empty : "Replay - no reward";
        }
    }
}
