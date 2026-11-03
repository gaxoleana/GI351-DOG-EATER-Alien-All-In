using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    /// <summary>
    /// Constructs a full 2.5D floating casino island diorama around the active board,
    /// featuring 3D perspective front and side walls, padded casino rails, 
    /// ambient island drop shadows, character contact shadows, and luxury background floating pads.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoardGrid))]
    public sealed class BoardIslandVisual : MonoBehaviour
    {
        [Header("Casino Floor Carpet Sprites")]
        [SerializeField] Sprite casinoTileA;
        [SerializeField] Sprite casinoTileB;

        [Header("2.5D Island Diorama Walls")]
        [SerializeField] Sprite cliffFront;
        [SerializeField] Sprite cliffCornerLeft;
        [SerializeField] Sprite cliffCornerRight;
        [SerializeField] Sprite cliffSideLeft;
        [SerializeField] Sprite cliffSideRight;
        [SerializeField] Sprite rimTop;

        [Header("3D Shadows")]
        [SerializeField] Sprite islandShadow;
        [SerializeField] Sprite characterShadow;

        [Header("Background Elements")]
        [SerializeField] Sprite floatingCasinoPad;
        [SerializeField] Color casinoAmbientColor = new Color(0.985f, 0.945f, 0.940f, 1f);

        [Header("Diorama Offsets & Sorting")]
        [SerializeField] float cliffYOffset = -0.6f;
        [SerializeField] int cliffSortingOrder = 2;
        [SerializeField] int rimSortingOrder = 1;
        [SerializeField] int charShadowSortingOrder = 4;
        [SerializeField] int islandShadowSortingOrder = -15;
        [SerializeField] int backgroundSortingOrder = -25;

        BoardGrid board;
        Transform dioramaContainer;
        Transform bgContainer;
        Transform charShadowContainer;

        int lastWidth = -1;
        int lastHeight = -1;
        Vector2Int lastMin = new Vector2Int(-999, -999);

        void Awake()
        {
            board = GetComponent<BoardGrid>();
            ApplyCellSprites();
            RefreshVisuals();
        }

        void OnEnable()
        {
            board = GetComponent<BoardGrid>();
            ApplyCellSprites();
            RefreshVisuals();
        }

        void LateUpdate()
        {
            if (!board) board = GetComponent<BoardGrid>();
            if (!board) return;

            if (board.ActiveWidth != lastWidth || board.ActiveHeight != lastHeight || board.ActiveMinCell != lastMin)
            {
                RefreshVisuals();
            }

            UpdateCharacterShadows();
        }

        public void ApplyCellSprites()
        {
            if (!board) board = GetComponent<BoardGrid>();
            if (!board) return;

            var cells = GetComponentsInChildren<BoardCell>(true);
            foreach (var cell in cells)
            {
                if (!cell || !cell.surface) continue;
                cell.transform.localScale = Vector3.one;
                bool isEven = (cell.coordinate.x + cell.coordinate.y) % 2 == 0;
                if (casinoTileA && casinoTileB)
                {
                    cell.surface.sprite = isEven ? casinoTileA : casinoTileB;
                }
                cell.baseColor = Color.white;
                if (cell.IsUnlocked)
                {
                    cell.surface.color = Color.white;
                }
            }
        }

        public void RefreshVisuals()
        {
            if (!board) board = GetComponent<BoardGrid>();
            if (!board) return;

            lastWidth = board.ActiveWidth;
            lastHeight = board.ActiveHeight;
            lastMin = board.ActiveMinCell;

            EnsureContainers();
            RebuildDiorama();
            EnsureBackgroundPads();
        }

        void EnsureContainers()
        {
            if (!dioramaContainer)
            {
                var existing = transform.Find("Diorama_Container");
                if (existing) dioramaContainer = existing;
                else
                {
                    var go = new GameObject("Diorama_Container");
                    go.transform.SetParent(transform, false);
                    dioramaContainer = go.transform;
                }
            }

            if (!bgContainer)
            {
                var existing = transform.Find("Background_Container");
                if (existing) bgContainer = existing;
                else
                {
                    var go = new GameObject("Background_Container");
                    go.transform.SetParent(transform, false);
                    bgContainer = go.transform;
                }
            }

            if (!charShadowContainer)
            {
                var existing = transform.Find("CharShadow_Container");
                if (existing) charShadowContainer = existing;
                else
                {
                    var go = new GameObject("CharShadow_Container");
                    go.transform.SetParent(transform, false);
                    charShadowContainer = go.transform;
                }
            }
        }

        void RebuildDiorama()
        {
            if (!dioramaContainer) return;

            // Clear old diorama pieces
            for (int i = dioramaContainer.childCount - 1; i >= 0; i--)
            {
                var child = dioramaContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            int minX = lastMin.x;
            int minY = lastMin.y;
            int width = lastWidth;
            int height = lastHeight;
            float cs = board.cellSize;
            float cliffY = (minY + cliffYOffset) * cs;

            // 1. Giant Island Drop Shadow beneath the platform
            if (islandShadow)
            {
                var shadowObj = new GameObject("Island_Drop_Shadow");
                shadowObj.transform.SetParent(dioramaContainer, false);
                shadowObj.transform.localPosition = new Vector3(
                    (minX + width * 0.5f) * cs,
                    (minY - 0.7f) * cs,
                    0
                );
                shadowObj.transform.localScale = new Vector3(width * cs * 1.35f, height * cs * 0.95f, 1f);
                var sr = shadowObj.AddComponent<SpriteRenderer>();
                sr.sprite = islandShadow;
                sr.sortingOrder = islandShadowSortingOrder;
            }

            // 2. Front Cliff Face across active width
            if (cliffFront)
            {
                for (int x = 0; x < width; x++)
                {
                    var seg = new GameObject($"Cliff_Front_{x}");
                    seg.transform.SetParent(dioramaContainer, false);
                    seg.transform.localPosition = new Vector3((minX + x + 0.5f) * cs, cliffY, 0);
                    var sr = seg.AddComponent<SpriteRenderer>();
                    sr.sprite = cliffFront;
                    sr.sortingOrder = cliffSortingOrder;
                }
            }

            // 3. Front Left and Right Corners
            if (cliffCornerLeft)
            {
                var leftCorner = new GameObject("Cliff_Corner_Left");
                leftCorner.transform.SetParent(dioramaContainer, false);
                leftCorner.transform.localPosition = new Vector3((minX - 0.45f) * cs, cliffY, 0);
                var sr = leftCorner.AddComponent<SpriteRenderer>();
                sr.sprite = cliffCornerLeft;
                sr.sortingOrder = cliffSortingOrder;
            }

            if (cliffCornerRight)
            {
                var rightCorner = new GameObject("Cliff_Corner_Right");
                rightCorner.transform.SetParent(dioramaContainer, false);
                rightCorner.transform.localPosition = new Vector3((minX + width + 0.45f) * cs, cliffY, 0);
                var sr = rightCorner.AddComponent<SpriteRenderer>();
                sr.sprite = cliffCornerRight;
                sr.sortingOrder = cliffSortingOrder;
            }

            // 4. Left and Right 2.5D Side Walls (thickness along depth)
            if (cliffSideLeft)
            {
                for (int y = 0; y < height; y++)
                {
                    var sideL = new GameObject($"Cliff_Side_Left_{y}");
                    sideL.transform.SetParent(dioramaContainer, false);
                    sideL.transform.localPosition = new Vector3((minX - 0.45f) * cs, (minY + y + 0.5f) * cs, 0);
                    var sr = sideL.AddComponent<SpriteRenderer>();
                    sr.sprite = cliffSideLeft;
                    sr.sortingOrder = rimSortingOrder;
                }
            }

            if (cliffSideRight)
            {
                for (int y = 0; y < height; y++)
                {
                    var sideR = new GameObject($"Cliff_Side_Right_{y}");
                    sideR.transform.SetParent(dioramaContainer, false);
                    sideR.transform.localPosition = new Vector3((minX + width + 0.45f) * cs, (minY + y + 0.5f) * cs, 0);
                    var sr = sideR.AddComponent<SpriteRenderer>();
                    sr.sprite = cliffSideRight;
                    sr.sortingOrder = rimSortingOrder;
                }
            }

            // 5. Top Rim Rail (Framing the back edge of the casino table)
            if (rimTop)
            {
                for (int x = 0; x < width; x++)
                {
                    var rim = new GameObject($"Rim_Top_{x}");
                    rim.transform.SetParent(dioramaContainer, false);
                    rim.transform.localPosition = new Vector3((minX + x + 0.5f) * cs, (minY + height + 0.35f) * cs, 0);
                    var sr = rim.AddComponent<SpriteRenderer>();
                    sr.sprite = rimTop;
                    sr.sortingOrder = rimSortingOrder;
                }
            }
        }

        void EnsureBackgroundPads()
        {
            if (!bgContainer || !floatingCasinoPad) return;
            if (bgContainer.childCount > 0) return;

            Vector3[] offsets = new Vector3[]
            {
                new Vector3(-3.2f, 2.7f, 0),
                new Vector3(5.2f, 2.9f, 0),
                new Vector3(-3.3f, -2.1f, 0),
                new Vector3(5.4f, -2.2f, 0),
            };

            float[] scales = new float[] { 0.85f, 1.0f, 0.8f, 0.95f };

            for (int i = 0; i < offsets.Length; i++)
            {
                var pad = new GameObject($"CasinoPad_{i}");
                pad.transform.SetParent(bgContainer, false);
                pad.transform.localPosition = new Vector3(
                    (lastMin.x + lastWidth * 0.5f) * board.cellSize + offsets[i].x,
                    (lastMin.y + lastHeight * 0.5f) * board.cellSize + offsets[i].y,
                    0
                );
                pad.transform.localScale = Vector3.one * scales[i];
                var sr = pad.AddComponent<SpriteRenderer>();
                sr.sprite = floatingCasinoPad;
                sr.sortingOrder = backgroundSortingOrder;
            }
        }

        void UpdateCharacterShadows()
        {
            if (!characterShadow || !charShadowContainer) return;

            // Find all active GridActors
            var actors = FindObjectsByType<GridActor>();
            
            // Adjust pool size
            while (charShadowContainer.childCount < actors.Length)
            {
                var shadowObj = new GameObject($"Shadow_{charShadowContainer.childCount}");
                shadowObj.transform.SetParent(charShadowContainer, false);
                shadowObj.transform.localScale = new Vector3(0.8f, 0.45f, 1f);
                var sr = shadowObj.AddComponent<SpriteRenderer>();
                sr.sprite = characterShadow;
                sr.sortingOrder = charShadowSortingOrder;
            }

            int index = 0;
            for (int i = 0; i < actors.Length; i++)
            {
                var actor = actors[i];
                if (!actor || !actor.gameObject.activeInHierarchy || !actor.Alive) continue;

                var shadow = charShadowContainer.GetChild(index);
                shadow.gameObject.SetActive(true);
                shadow.position = actor.transform.position + new Vector3(0, -0.32f, 0);
                index++;
            }

            for (int i = index; i < charShadowContainer.childCount; i++)
            {
                charShadowContainer.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
