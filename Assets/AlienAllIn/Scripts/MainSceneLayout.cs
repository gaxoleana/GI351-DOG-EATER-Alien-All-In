using UnityEngine;

namespace AlienAllIn
{
    /// <summary>Shared HUD/camera regions. Fits the currently open board as the arcade session expands.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MainSceneLayout : MonoBehaviour
    {
        [SerializeField] BoardGrid board;
        [SerializeField] Camera stageCamera;
        [Header("Board framing")]
        [SerializeField, Tooltip("Optional overview. Off shows only the currently unlocked arcade board.")]
        bool frameMaximumBoard;
        [SerializeField, Min(0)] float worldPadding = .65f;
        [SerializeField] Color backgroundColor = new Color(.87f, .87f, .87f, 1);
        [SerializeField, Range(.8f, .97f)] float selectedZoom = .88f;
        GridActor focusedActor;
        Vector3 focusPoint;
        float focusAmount;
        public void FocusActor(GridActor actor) { focusedActor = actor; if (actor) focusPoint = actor.transform.position; }

        [Header("Editable screen regions (normalized coordinates)")]
        [SerializeField] Rect headerRegion = new Rect(.16f,.86f,.80f,.10f);
        [SerializeField] Rect reservedLeftRegion = new Rect(.035f,.18f,.10f,.66f);
        [SerializeField] Rect statusRegion = new Rect(.885f,.29f,.075f,.45f);
        [SerializeField] Rect boardRegion = new Rect(.16f,.18f,.68f,.66f);
        [SerializeField] Rect itemsRegion = new Rect(.38f,.04f,.24f,.09f);
        public Rect Header => headerRegion;
        public Rect LeftColumn => reservedLeftRegion;
        public Rect RightColumn => statusRegion;
        public Rect BoardViewport => boardRegion;
        public Rect ItemBar => itemsRegion;
        public Color BackgroundColor => backgroundColor;
        public bool FrameMaximumBoard { get => frameMaximumBoard; set { frameMaximumBoard = value; RefreshCamera(); } }

        public void Configure(BoardGrid grid, Camera camera)
        {
            board = grid;
            stageCamera = camera;
            RefreshCamera();
        }

        void OnEnable() => RefreshCamera();
        void LateUpdate()
        {
            if (Application.isPlaying) focusAmount = Mathf.MoveTowards(focusAmount, focusedActor ? 1 : 0, Time.unscaledDeltaTime * 5);
            else focusAmount = 0;
            RefreshCamera();
        }
        void OnValidate() => RefreshCamera();

        public void RefreshCamera()
        {
            if (!board || !stageCamera) return;
            stageCamera.rect = BoardViewport;
            stageCamera.orthographic = true;
            stageCamera.backgroundColor = backgroundColor;
            stageCamera.clearFlags = CameraClearFlags.SolidColor;

            // Use all four corners so moving/scaling/rotating the board in XY remains supported.
            Vector2Int min = frameMaximumBoard ? Vector2Int.zero : board.ActiveMinCell;
            int width = frameMaximumBoard ? board.width : board.ActiveWidth;
            int height = frameMaximumBoard ? board.height : board.ActiveHeight;
            Vector3 a = new Vector3(min.x * board.cellSize, min.y * board.cellSize);
            Bounds bounds = new Bounds(board.transform.TransformPoint(a), Vector3.zero);
            bounds.Encapsulate(board.transform.TransformPoint(a + new Vector3(width * board.cellSize, 0)));
            bounds.Encapsulate(board.transform.TransformPoint(a + new Vector3(0, height * board.cellSize)));
            bounds.Encapsulate(board.transform.TransformPoint(a + new Vector3(width * board.cellSize, height * board.cellSize)));
            stageCamera.transform.position = new Vector3(bounds.center.x, bounds.center.y, stageCamera.transform.position.z);
            float aspect = Mathf.Max(.01f, stageCamera.aspect);
            stageCamera.orthographicSize = Mathf.Max(bounds.extents.y + worldPadding, (bounds.extents.x + worldPadding) / aspect);
            if (focusAmount > 0)
            {
                Vector3 center = Vector3.Lerp(bounds.center, focusPoint, .2f * focusAmount);
                stageCamera.transform.position = new Vector3(center.x, center.y, stageCamera.transform.position.z);
                stageCamera.orthographicSize *= Mathf.Lerp(1, selectedZoom, focusAmount);
            }
        }
    }
}
