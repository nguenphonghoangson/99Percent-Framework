using System;
using System.Collections.Generic;
using NinetyNine.Core;
using NinetyNine.Modules.Economy;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Moves;
using NinetyNine.Modules.Puzzle.Objectives;
using NinetyNine.Modules.Puzzle.Results;
using NinetyNine.Modules.Puzzle.Rules;

namespace NinetyNine.Features.PuzzleGameplay.UI
{
    public readonly struct ObjectiveProgress
    {
        public ObjectiveProgress(IObjective objective)
        {
            Kind = objective.Definition.kind;
            TileId = objective.Definition.tileId;
            Current = objective.Current;
            Target = objective.Target;
        }

        public ObjectiveKind Kind { get; }
        public int TileId { get; }
        public int Current { get; }
        public int Target { get; }
        public bool IsComplete => Current >= Target;
    }

    /// <summary>Implemented by the HUD MonoBehaviour. Board rendering listens to the session directly.</summary>
    public interface IPuzzleHudView
    {
        void SetLevelNumber(int levelNumber);

        void SetMovesLeft(int movesLeft, bool unlimited);

        void SetScore(int score);

        void SetObjectives(IReadOnlyList<ObjectiveProgress> objectives);

        void ShowOutOfMoves(Cost reviveCost, bool canRevive);

        void ShowResult(LevelResult result, bool firstClear);
    }

    public sealed class PuzzleHudPresenter : IDisposable
    {
        private readonly List<ObjectiveProgress> _objectives = new();
        private readonly IPuzzleGameplayService _service;
        private readonly IDisposable _startedSubscription;
        private readonly IDisposable _finishedSubscription;
        private readonly IPuzzleHudView _view;
        private PuzzleSession _session;

        public PuzzleHudPresenter(IPuzzleGameplayService service, IEventBus events, IPuzzleHudView view)
        {
            _service = service;
            _view = view;
            _startedSubscription = events.Subscribe<PuzzleLevelStartedEvent>(_ => Bind(_service.ActiveSession));
            _finishedSubscription = events.Subscribe<PuzzleLevelFinishedEvent>(e => _view.ShowResult(e.Result, e.FirstClear));
            Bind(_service.ActiveSession);
        }

        public void Dispose()
        {
            Bind(null);
            _startedSubscription.Dispose();
            _finishedSubscription.Dispose();
        }

        private void Bind(PuzzleSession session)
        {
            if (_session != null)
            {
                _session.MoveResolved -= OnMoveResolved;
                _session.OutOfMoves -= OnOutOfMoves;
            }

            _session = session;
            if (_session == null) return;

            _session.MoveResolved += OnMoveResolved;
            _session.OutOfMoves += OnOutOfMoves;
            _view.SetLevelNumber(_session.LevelIndex + 1);
            Render();
        }

        /// <summary>Re-renders after a change that is not a move (a revive adding moves).</summary>
        public void Refresh()
        {
            if (_session != null) Render();
        }

        private void OnMoveResolved(PuzzleMove move, MoveOutcome outcome) => Render();

        private void OnOutOfMoves() => _view.ShowOutOfMoves(_service.ReviveCost, _service.CanRevive);

        private void Render()
        {
            _view.SetMovesLeft(_session.Turn.MovesLeft, _session.Turn.IsUnlimited);
            _view.SetScore(_session.Score);

            _objectives.Clear();
            foreach (var objective in _session.Objectives) _objectives.Add(new ObjectiveProgress(objective));
            _view.SetObjectives(_objectives);
        }
    }
}
