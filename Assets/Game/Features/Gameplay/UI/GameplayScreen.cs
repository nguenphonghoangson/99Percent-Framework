using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NinetyNine.Core;
using NinetyNine.Game.Flow;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Moves;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Reward;
using NinetyNine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.PuzzleGameplay.UI
{
    /// <summary>
    ///     The level screen, root of the Gameplay scene under <see cref="PuzzleGameplayFeature.Id" /> (args: the
    ///     scene's load args, an optional progress index to replay). Draws HUD + board and runs the end-of-level
    ///     flow: it opens the Result popups by key (<see cref="LevelEndKeys" />) and acts on the
    ///     <see cref="LevelEndChoice" /> they close with. Leaving loads the Home scene.
    /// </summary>
    public sealed class GameplayScreen : UIScreen, IPuzzleHudView
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text objectivesText;
        [SerializeField] private BoardView board;
        [SerializeField] private Button homeButton;

        private readonly StringBuilder _objectives = new();
        private IPuzzleGameplayService _gameplay;
        private PuzzleHudPresenter _hud;
        private ISceneLoader _scenes;
        private IDisposable _started;

        protected override void OnCreated()
        {
            var services = ServiceLocator.Current ?? throw new InvalidOperationException("Services not booted.");
            _gameplay = services.Require<IPuzzleGameplayService>();
            _scenes = services.Require<ISceneLoader>();
            var events = services.Require<IEventBus>();

            _started = events.Subscribe<PuzzleLevelStartedEvent>(_ => board.Bind(_gameplay.ActiveSession));
            _hud = new PuzzleHudPresenter(_gameplay, events, this);
            board.CellTapped += OnCellTapped;
            homeButton.onClick.AddListener(GoHome);
        }

        protected override void OnOpening(object args)
        {
            if (args is int progressIndex) _gameplay.StartLevel(progressIndex);
            else _gameplay.StartCurrentLevel();
        }

        // Leaving mid-level (Home button → Home scene replaces this screen) ends the session as a Quit rather than
        // leaving it dangling.
        protected override void OnClosing()
        {
            if (_gameplay.ActiveSession is { State: not PuzzleSessionState.Ended } session) session.Quit();
        }

        private void OnDestroy()
        {
            _started?.Dispose();
            _hud?.Dispose();
        }

        private void GoHome() => Forget(_scenes.LoadAsync(GameScenes.Home));

        private void OnCellTapped(GridPos pos) => _gameplay.ActiveSession?.TryMove(PuzzleMove.Tap(pos));

        // ---------------------------------------------------------------- IPuzzleHudView

        public void SetLevelNumber(int levelNumber) => levelText.text = $"Level {levelNumber}";

        public void SetMovesLeft(int movesLeft, bool unlimited) => movesText.text = unlimited ? "No limit" : $"Moves {movesLeft}";

        public void SetScore(int score) => scoreText.text = $"Score {score}";

        public void SetObjectives(IReadOnlyList<ObjectiveProgress> objectives)
        {
            _objectives.Clear();
            foreach (var objective in objectives)
            {
                if (_objectives.Length > 0) _objectives.Append("    ");
                _objectives.Append(objective.Kind switch
                {
                    ObjectiveKind.CollectTile => TilePalette.Tag(objective.TileId),
                    ObjectiveKind.ReachScore => "Score",
                    _ => "Clear"
                });
                _objectives.Append(' ').Append(objective.Current).Append('/').Append(objective.Target);
            }

            objectivesText.text = _objectives.ToString();
        }

        public void ShowOutOfMoves(Cost reviveCost, bool canRevive) => Forget(OfferRevive(reviveCost, canRevive));

        public void ShowResult(LevelResult result, bool firstClear)
        {
            if (result.Outcome == LevelOutcome.Win) Forget(ShowWin(result, firstClear));
            else if (result.Outcome == LevelOutcome.Lose) Forget(ShowLose(result));
        }

        // ---------------------------------------------------------------- end-of-level flow

        private async Task OfferRevive(Cost cost, bool canAfford)
        {
            var session = _gameplay.ActiveSession;
            var args = LoseArgs.ReviveOffer(session.LevelIndex + 1, _gameplay.ReviveAvailable, canAfford, cost,
                _gameplay.ReviveMoves);
            var choice = await Navigator.ShowPopupAndWait(LevelEndKeys.Lose, args);

            // The player may have left the screen meanwhile; only act on the session that asked.
            if (_gameplay.ActiveSession != session || session.State != PuzzleSessionState.OutOfMoves) return;

            if (choice is LevelEndChoice.Revive && _gameplay.TryRevive().IsSuccess)
            {
                _hud.Refresh();
                return;
            }

            session.GiveUp();
        }

        private async Task ShowWin(LevelResult result, bool firstClear)
        {
            var reward = firstClear ? _gameplay.FirstClearReward : new RewardBundle();
            var choice = await Navigator.ShowPopupAndWait(LevelEndKeys.Win, new WinArgs(result, firstClear, reward));

            if (choice is LevelEndChoice.Next) _gameplay.StartCurrentLevel();
            else GoHome();
        }

        private async Task ShowLose(LevelResult result)
        {
            var choice = await Navigator.ShowPopupAndWait(LevelEndKeys.Lose, LoseArgs.Final(result.LevelIndex + 1));

            if (choice is LevelEndChoice.Retry) _gameplay.StartLevel(result.LevelIndex);
            else GoHome();
        }
    }
}
