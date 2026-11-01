using UnityEngine;

namespace AlienAllIn
{
    [DisallowMultipleComponent]
    public sealed class BoardGrid : MonoBehaviour
    {
        [Min(1)] public int width = 10;
        [Min(1)] public int height = 10;
        [Min(0.01f), Tooltip("Cell spacing in local units. Keep Board scale at (1,1,1) for world-unit spacing.")]
        public float cellSize = 1;
        BoardCell[,] cells;

        public bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
        public Vector3 CellCenter(Vector2Int cell) => transform.TransformPoint(new Vector3((cell.x + .5f) * cellSize, (cell.y + .5f) * cellSize));

        // Half-open bounds: a boundary belongs to the cell on its right/top.
        // InverseTransformPoint also supports a board moved/rotated in the hierarchy.
        public bool TryWorldToCell(Vector3 world, out Vector2Int cell)
        {
            Vector3 local = transform.InverseTransformPoint(world) / cellSize;
            cell = new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));
            return Contains(cell);
        }

        public void RebuildIndex()
        {
            cells = new BoardCell[width, height];
            foreach (BoardCell cell in GetComponentsInChildren<BoardCell>())
                if (Contains(cell.coordinate)) cells[cell.coordinate.x, cell.coordinate.y] = cell;
        }

        public BoardCell GetCell(Vector2Int coordinate)
        {
            if (cells == null) RebuildIndex();
            return Contains(coordinate) ? cells[coordinate.x, coordinate.y] : null;
        }

        public void RefreshOccupancy(System.Collections.Generic.IEnumerable<GridActor> actors)
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
            for (int x = 0; x <= width; x++) Gizmos.DrawLine(new Vector3(x * cellSize, 0), new Vector3(x * cellSize, height * cellSize));
            for (int y = 0; y <= height; y++) Gizmos.DrawLine(new Vector3(0, y * cellSize), new Vector3(width * cellSize, y * cellSize));
        }
    }
}
