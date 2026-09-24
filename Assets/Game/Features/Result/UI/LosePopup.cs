using System;
using NinetyNine.Features.PuzzleGameplay;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.Result
{
    /// <summary>
    ///     Opened with <see cref="LoseArgs" /> under <see cref="LevelEndKeys.Lose" />, in one of two modes:
    ///     out of moves → <see cref="LevelEndChoice.Revive" /> / <see cref="LevelEndChoice.GiveUp" />;
    ///     final → <see cref="LevelEndChoice.Retry" /> / <see cref="LevelEndChoice.Home" />. The back key closes
    ///     with no choice, which the gameplay screen reads as the secondary action.
    /// </summary>
    public sealed class LosePopup : UIPopup
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button primaryButton;
        [SerializeField] private TMP_Text primaryLabel;
        [SerializeField] private Button secondaryButton;
        [SerializeField] private TMP_Text secondaryLabel;

        private LoseArgs _args;

        private void Awake()
        {
            primaryButton.onClick.AddListener(() => Close(_args.OutOfMoves ? LevelEndChoice.Revive : LevelEndChoice.Retry));
            secondaryButton.onClick.AddListener(() => Close(_args.OutOfMoves ? LevelEndChoice.GiveUp : LevelEndChoice.Home));
        }

        protected override void OnOpening(object args)
        {
            _args = args as LoseArgs ?? throw new ArgumentException($"{nameof(LosePopup)} needs {nameof(LoseArgs)}.");

            if (_args.OutOfMoves)
            {
                titleText.text = "Out of moves";
                bodyText.text = !_args.ReviveAvailable ? "No revives left for this level."
                    : _args.CanAffordRevive ? $"Keep playing with +{_args.ReviveMoves} moves."
                    : $"+{_args.ReviveMoves} moves costs {_args.ReviveCost}.\nNot enough currency.";
                primaryButton.gameObject.SetActive(_args.ReviveAvailable);
                primaryButton.interactable = _args.CanAffordRevive;
                primaryLabel.text = $"Revive  {_args.ReviveCost}";
                secondaryLabel.text = "Give up";
            }
            else
            {
                titleText.text = $"Level {_args.LevelNumber} failed";
                bodyText.text = "Try again?";
                primaryButton.gameObject.SetActive(true);
                primaryButton.interactable = true;
                primaryLabel.text = "Retry";
                secondaryLabel.text = "Home";
            }
        }
    }
}
