using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    /// <summary>
    /// Stage 1's authored grid setup and the small public surface other run systems can use.
    /// A run controller supplies normalized progress; this component does not own a timer.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageGridProgression : MonoBehaviour
    {
        [Header("Stage references")]
        [SerializeField] BoardGrid board;
        [SerializeField] Camera stageCamera;
        [SerializeField, Tooltip("Optional authored team start cells. Empty means a future placement system chooses within the active grid.")]
        Vector2Int[] playerStartCells = Array.Empty<Vector2Int>();

        [Header("Prototype grid sizes")]
        [SerializeField, Min(1)] int openingSize = 3;
        [SerializeField, Min(1)] int middleSize = 5;
        [SerializeField, Min(1)] int lateSize = 7;

        [Header("Prototype run-progress milestones")]
        [SerializeField, Range(0f, 1f)] float expandToMiddleAt = .25f;
        [SerializeField, Range(0f, 1f)] float expandToLateAt = .5f;
        [SerializeField, Range(0f, 1f)] float expandToFullAt = .75f;

        [Header("Camera framing")]
        [SerializeField, Min(0f)] float cameraPadding = .35f;

        readonly List<Vector2Int> processSpawnCandidates = new List<Vector2Int>(100);

        public BoardGrid Board => board;
        public IReadOnlyList<Vector2Int> PlayerStartCells => playerStartCells ?? Array.Empty<Vector2Int>();
        public IReadOnlyList<Vector2Int> ProcessSpawnCandidates
        {
            get
            {
                if (board) RefreshProcessSpawnCandidates();
                else processSpawnCandidates.Clear();
                return processSpawnCandidates;
            }
        }

        void Awake() => ApplyRunProgress(0f);

        void OnValidate()
        {
            openingSize = Mathf.Max(1, openingSize);
            middleSize = Mathf.Max(openingSize, middleSize);
            lateSize = Mathf.Max(middleSize, lateSize);
            expandToMiddleAt = Mathf.Clamp01(expandToMiddleAt);
            expandToLateAt = Mathf.Max(expandToMiddleAt, Mathf.Clamp01(expandToLateAt));
            expandToFullAt = Mathf.Max(expandToLateAt, Mathf.Clamp01(expandToFullAt));
            cameraPadding = Mathf.Max(0f, cameraPadding);
        }

        /// <summary>
        /// Applies a normalized 0..1 run-progress value to the prototype grid milestones.
        /// Spawn timing, activity type selection, and occupancy rules are owned by future systems.
        /// </summary>
        public void ApplyRunProgress(float progress)
        {
            if (!board)
            {
                Debug.LogError("StageGridProgression requires a BoardGrid reference.", this);
                return;
            }

            float normalizedProgress = Mathf.Clamp01(progress);
            bool fullGrid = normalizedProgress >= expandToFullAt;
            int size = normalizedProgress < expandToMiddleAt ? openingSize :
                normalizedProgress < expandToLateAt ? middleSize : lateSize;

            board.KeepLockedCellsVisible = true;
            board.SetActiveSize(
                fullGrid ? board.width : Mathf.Min(size, board.width),
                fullGrid ? board.height : Mathf.Min(size, board.height));

            RefreshProcessSpawnCandidates();
            FrameActiveArea();
        }

        public bool TryGetPlayerStartPosition(int index, out Vector3 worldPosition)
        {
            if (board && playerStartCells != null && index >= 0 && index < playerStartCells.Length &&
                board.Contains(playerStartCells[index]))
            {
                worldPosition = board.CellCenter(playerStartCells[index]);
                return true;
            }

            worldPosition = default;
            return false;
        }

        public bool IsInActiveArea(Vector2Int cell) => board && board.Contains(cell);

        void RefreshProcessSpawnCandidates()
        {
            processSpawnCandidates.Clear();
            for (int y = 0; y < board.height; y++)
            for (int x = 0; x < board.width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (board.Contains(cell)) processSpawnCandidates.Add(cell);
            }
        }

        void FrameActiveArea()
        {
            if (!stageCamera) return;

            stageCamera.orthographic = true;
            Vector3 center = board.ActiveCenter;
            Vector3 position = stageCamera.transform.position;
            position.x = center.x;
            position.y = center.y;
            stageCamera.transform.position = position;

            float halfHeight = board.ActiveHeight * board.cellSize * .5f;
            float halfWidth = board.ActiveWidth * board.cellSize * .5f / Mathf.Max(.1f, stageCamera.aspect);
            stageCamera.orthographicSize = Mathf.Max(halfHeight, halfWidth) + cameraPadding;
        }
    }
}
