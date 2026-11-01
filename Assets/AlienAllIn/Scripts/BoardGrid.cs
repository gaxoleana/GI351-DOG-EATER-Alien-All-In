using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    [DisallowMultipleComponent]
    public sealed class BoardGrid : MonoBehaviour
    {
        [Header("Maximum board")]
        [Min(1)] public int width = 10;
        [Min(1)] public int height = 10;
        [Min(0.01f), Tooltip("Cell spacing in local units. Keep Board scale at (1,1,1) for world-unit spacing.")]
        public float cellSize = 1;

        [Header("Currently open area")]
        [SerializeField, Min(1)] int activeWidth = 10;
        [SerializeField, Min(1)] int activeHeight = 10;
        BoardCell[,] cells;

        public int ActiveWidth => activeWidth;
        public int ActiveHeight => activeHeight;
        public Vector2Int ActiveMinCell => new Vector2Int((width - activeWidth + 1) / 2, (height - activeHeight + 1) / 2);
        public Vector3 ActiveCenter => transform.TransformPoint(new Vector3(
            (ActiveMinCell.x + activeWidth * .5f) * cellSize,
            (ActiveMinCell.y + activeHeight * .5f) * cellSize,
            0));

        public bool Contains(Vector2Int cell)
        {
            Vector2Int min = ActiveMinCell;
            return cell.x >= min.x && cell.y >= min.y &&
                   cell.x < min.x + activeWidth && cell.y < min.y + activeHeight;
        }

        public Vector3 CellCenter(Vector2Int cell) => transform.TransformPoint(new Vector3((cell.x + .5f) * cellSize, (cell.y + .5f) * cellSize));

        // Half-open bounds: a boundary belongs to the cell on its right/top.
        // InverseTransformPoint also supports a board moved/rotated in the hierarchy.
        public bool TryWorldToCell(Vector3 world, out Vector2Int cell)
        {
            Vector3 local = transform.InverseTransformPoint(world) / cellSize;
            cell = new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
            return Contains(cell);
        }

        public bool SetActiveSize(int requestedWidth, int requestedHeight)
        {
            if (cells == null) RebuildIndex();

            int nextWidth = Mathf.Clamp(requestedWidth, 1, width);
            int nextHeight = Mathf.Clamp(requestedHeight, 1, height);
            bool changed = activeWidth != nextWidth || activeHeight != nextHeight;
            activeWidth = nextWidth;
            activeHeight = nextHeight;

            Vector2Int min = ActiveMinCell;
            foreach (BoardCell cell in cells)
            {
                if (!cell) continue;
                Vector2Int coordinate = cell.coordinate;
                bool inside = coordinate.x >= min.x && coordinate.y >= min.y &&
                              coordinate.x < min.x + activeWidth && coordinate.y < min.y + activeHeight;
                if (cell.gameObject.activeSelf != inside) cell.gameObject.SetActive(inside);
            }

            return changed;
        }

        public void RebuildIndex()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            cells = new BoardCell[width, height];
            foreach (BoardCell cell in GetComponentsInChildren<BoardCell>(true))
                if (cell.coordinate.x >= 0 && cell.coordinate.y >= 0 && cell.coordinate.x < width && cell.coordinate.y < height)
                    cells[cell.coordinate.x, cell.coordinate.y] = cell;
        }

        public BoardCell GetCell(Vector2Int coordinate)
        {
            if (cells == null) RebuildIndex();
            return Contains(coordinate) ? cells[coordinate.x, coordinate.y] : null;
        }

        public void RefreshOccupancy(IEnumerable<GridActor> actors)
        {
            if (cells == null) RebuildIndex();
            foreach (BoardCell cell in cells) if (cell) cell.ClearOccupants();
            foreach (GridActor actor in actors)
            {
                if (!actor || !actor.gameObject.activeInHierarchy || !actor.Alive) continue;
                actor.RefreshCell();
                if (actor.IsOnBoard) GetCell(actor.CurrentCell)?.AddOccupant(actor);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.3f, 1, .85f, .6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector2Int min = ActiveMinCell;
            for (int x = 0; x <= activeWidth; x++)
                Gizmos.DrawLine(new Vector3((min.x + x) * cellSize, min.y * cellSize), new Vector3((min.x + x) * cellSize, (min.y + activeHeight) * cellSize));
            for (int y = 0; y <= activeHeight; y++)
                Gizmos.DrawLine(new Vector3(min.x * cellSize, (min.y + y) * cellSize), new Vector3((min.x + activeWidth) * cellSize, (min.y + y) * cellSize));
        }
    }
}
