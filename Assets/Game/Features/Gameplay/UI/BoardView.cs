using System;
using System.Collections.Generic;
using NinetyNine.Modules.Puzzle;
using NinetyNine.Modules.Puzzle.Boards;
using NinetyNine.Modules.Puzzle.Moves;
using NinetyNine.Modules.Puzzle.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace NinetyNine.Features.PuzzleGameplay.UI
{
    /// <summary>
    ///     Draws a <see cref="PuzzleSession" /> board as uGUI cells and reports taps. No rules here: the screen
    ///     turns a tap into a move. Cells are positioned by hand (not a layout group) so row y = 0 sits at the
    ///     bottom, matching <see cref="Board" />, and the board scales to fit whatever rect it is given.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private Sprite cellSprite;
        [SerializeField] private float spacing = 8;

        private readonly List<Button> _buttons = new();
        private readonly List<Image> _cells = new();
        private PuzzleSession _session;

        public event Action<GridPos> CellTapped;

        public void Bind(PuzzleSession session)
        {
            if (_session != null) _session.MoveResolved -= OnMoveResolved;
            _session = session;
            if (_session == null) return;

            _session.MoveResolved += OnMoveResolved;
            EnsureCells(_session.Board.Width * _session.Board.Height);
            Layout();
            Render();
        }

        private void OnDestroy() => Bind(null);

        private void OnRectTransformDimensionsChange()
        {
            if (_session != null) Layout();
        }

        private void OnMoveResolved(PuzzleMove move, MoveOutcome outcome) => Render();

        private void EnsureCells(int count)
        {
            while (_cells.Count < count)
            {
                var index = _cells.Count;
                var cell = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Button)) { layer = gameObject.layer };
                cell.transform.SetParent(transform, false);
                var image = cell.GetComponent<Image>();
                image.sprite = cellSprite;
                image.type = Image.Type.Sliced;
                var button = cell.GetComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => OnCellClicked(index));
                _cells.Add(image);
                _buttons.Add(button);
            }

            for (var i = 0; i < _cells.Count; i++) _cells[i].gameObject.SetActive(i < count);
        }

        private void OnCellClicked(int index)
        {
            if (_session == null) return;
            var width = _session.Board.Width;
            CellTapped?.Invoke(new GridPos(index % width, index / width));
        }

        private void Layout()
        {
            var board = _session.Board;
            var area = ((RectTransform)transform).rect;
            var size = Mathf.Floor(Mathf.Min(
                (area.width - spacing * (board.Width - 1)) / board.Width,
                (area.height - spacing * (board.Height - 1)) / board.Height));
            size = Mathf.Max(size, 1);

            var step = size + spacing;
            var origin = -new Vector2(board.Width * step - spacing, board.Height * step - spacing) / 2;
            for (var i = 0; i < board.Width * board.Height; i++)
            {
                int x = i % board.Width, y = i / board.Width;
                var rect = (RectTransform)_cells[i].transform;
                rect.name = $"Cell_{x}_{y}";
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.anchoredPosition = origin + new Vector2(x * step + size / 2, y * step + size / 2);
            }
        }

        private void Render()
        {
            var board = _session.Board;
            for (var i = 0; i < board.Width * board.Height; i++)
            {
                var tile = board[i % board.Width, i / board.Width];
                _cells[i].enabled = tile != Tile.Empty;
                _cells[i].color = TilePalette.ColorOf(tile);
                _buttons[i].interactable = Tile.IsPlayable(tile);
            }
        }
    }
}
