using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace AlienAllIn
{
    public sealed class BoardInteractionFeedback : MonoBehaviour
    {
        public BoardGrid board;
        public Camera stageCamera;
        public MainCoreGameplayUI ui;
        public MainRunGameplaySystems systems;
        public LineRenderer hoverOutline, dragOutline;
        public Color hoverColor = new Color(1, .7f, .12f);
        public Color validColor = new Color(.2f, .85f, .55f);
        public Color invalidColor = new Color(1, .25f, .25f);
        public Color rangeColor = new Color(1, .66f, .25f);
        readonly List<BoardCell> painted = new List<BoardCell>();
        GridActor dragged;
        Vector2 pointer;
        Vector2Int rangeCenter;
        bool enhanced;
        float rangeTime;
        public Vector2Int? DragCell { get; private set; }
        public int RangeCellCount { get; private set; }
        public void BeginDrag(GridActor actor, Vector2 screen) { dragged = actor; pointer = screen; }
        public void DragTo(Vector2 screen) => pointer = screen;
        public void EndDrag() { dragged = null; DragCell = null; if (dragOutline) dragOutline.enabled = false; }
        public void ShowRange(Vector3 origin, bool large, float duration)
        { board.TryWorldToCell(origin, out rangeCenter); enhanced = large; rangeTime = duration; }
        public void ClearRange() { rangeTime = 0; Restore(); }
        void Restore() { foreach (var cell in painted) if (cell) cell.Highlight(null); painted.Clear(); RangeCellCount = 0; }
        void OnDisable() { Restore(); EndDrag(); if (hoverOutline) hoverOutline.enabled = false; }
        void LateUpdate()
        {
            Restore();
            var selected = ui.CharacterPopupOpen ? ui.SelectedActor : null;
            if (selected && selected.Alive)
            {
                DragCell = null;
                hoverOutline.enabled = true;
                Box(hoverOutline, selected.transform.position, .55f, hoverColor);
                dragOutline.enabled = false;
                ShowSelectedRange(selected);
                return;
            }
            if (rangeTime > 0)
            {
                foreach (var cell in board.GetComponentsInChildren<BoardCell>())
                    if (SealGridRange.Contains(cell.coordinate - rangeCenter, enhanced)) { Paint(cell, rangeColor); RangeCellCount++; }
                rangeTime -= Time.deltaTime;
            }
            bool blocked = ui.ShopOpen || ui.CharacterPopupOpen || ui.OptionsOpen || (ui.GetComponent<MainDebugPanel>() && ui.GetComponent<MainDebugPanel>().IsOpen);
            var mouse = Mouse.current;
            var actor = !blocked && mouse != null ? ui.ActorAtScreenPoint(mouse.position.ReadValue()) : null;
            hoverOutline.enabled = actor && !dragged;
            if (hoverOutline.enabled) Box(hoverOutline, actor.transform.position, .55f, hoverColor);
            dragOutline.enabled = false; DragCell = null;
            if (!dragged || blocked || !stageCamera.pixelRect.Contains(pointer)) return;
            Vector3 point = stageCamera.ScreenToWorldPoint(new Vector3(pointer.x, pointer.y, -stageCamera.transform.position.z));
            if (!board.TryWorldToCell(point, out var coordinate)) return;
            DragCell = coordinate;
            bool valid = systems.CanOrderMove(dragged, coordinate);
            Color color = valid ? validColor : invalidColor;
            Paint(board.GetCell(coordinate), color); dragOutline.enabled = true;
            Box(dragOutline, board.CellCenter(coordinate), .48f, color);
        }
        void ShowSelectedRange(GridActor selected)
        {
            if (selected.kind == ActorKind.Duck) return;
            board.TryWorldToCell(selected.transform.position, out var center);
            float radius = systems.GetAttackPreviewRadius(selected);
            bool gridRange = selected.kind == ActorKind.Seal || selected.kind == ActorKind.Cat;
            foreach (var cell in board.GetComponentsInChildren<BoardCell>())
            {
                if (!cell.IsUnlocked) continue;
                bool inRange = selected.kind == ActorKind.Cat
                    ? selected.IsUltimateActive || CatAttackRange.Contains(cell.coordinate - center)
                    : gridRange ? SealGridRange.Contains(cell.coordinate - center, selected.IsUltimateActive)
                    : Vector3.Distance(board.CellCenter(cell.coordinate), selected.transform.position) <= radius;
                if (inRange) { Paint(cell, rangeColor); RangeCellCount++; }
            }
            if (gridRange) return;
            // Puffer's lock-on range still uses a world-space distance check.
            dragOutline.enabled = true;
            dragOutline.loop = true;
            dragOutline.positionCount = 64;
            dragOutline.startColor = dragOutline.endColor = rangeColor;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64;
                dragOutline.SetPosition(i, selected.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }
        void Paint(BoardCell cell, Color color) { cell.Highlight(color); painted.Add(cell); }
        void Box(LineRenderer line, Vector3 center, float radius, Color color)
        {
            line.loop = true; line.positionCount = 4; line.startColor = line.endColor = color;
            Vector3[] offsets = { new Vector3(-radius,-radius),new Vector3(-radius,radius),new Vector3(radius,radius),new Vector3(radius,-radius) };
            for (int i = 0; i < 4; i++) line.SetPosition(i, center + board.transform.TransformVector(offsets[i] * board.cellSize));
        }
    }
}
