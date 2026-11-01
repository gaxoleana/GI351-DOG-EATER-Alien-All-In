using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AlienAllIn
{
    /// <summary>
    /// Lets the configured RunLoop roster select members and place them on open cells
    /// without adding physics colliders to the board.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TeamPlacementController : MonoBehaviour
    {
        [SerializeField] RunLoopController runLoop;
        [SerializeField] BoardGrid board;
        [SerializeField] Camera stageCamera;
        [SerializeField, Tooltip("Enable the legacy IMGUI placement panel; MainCoreGameplayUI replaces it in Main.")]
        bool showLegacyPanel;

        const float PanelWidth = 152f;
        const float PanelHeight = 208f;
        GridActor selected;
        string message = "Ready to place.";
        public string CurrentMessage => message;

        Rect PanelRect => new Rect(Screen.width - PanelWidth - 16f, 16f, PanelWidth, PanelHeight);

        void Awake()
        {
            if (!runLoop) Debug.LogError("TeamPlacementController requires RunLoopController.", this);
            if (!board) Debug.LogError("TeamPlacementController requires BoardGrid.", this);
            if (!stageCamera) Debug.LogError("TeamPlacementController requires the stage camera.", this);
        }

        void Update()
        {
            if (!runLoop || !board || !stageCamera || !runLoop.CanPlaceTeam) return;

            ReadKeyboardSelection();

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Vector2 screenPosition = mouse.position.ReadValue();
            if (IsPointerOverBlockingUI(screenPosition)) return;
            Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            if (PanelRect.Contains(guiPosition) || !stageCamera.pixelRect.Contains(screenPosition)) return;

            Vector3 worldPosition = stageCamera.ScreenToWorldPoint(new Vector3(
                screenPosition.x,
                screenPosition.y,
                -stageCamera.transform.position.z));

            if (!board.TryWorldToCell(worldPosition, out Vector2Int cell))
            {
                message = IsInsideMaximumBoard(cell) ? "That cell is locked." : "Click inside the board.";
                return;
            }

            board.RefreshOccupancy(runLoop.Survivors);
            BoardCell target = board.GetCell(cell);
            GridActor occupant = null;
            if (target)
            {
                foreach (GridActor actor in target.Occupants)
                {
                    if (actor && actor.Alive) { occupant = actor; break; }
                }
            }

            if (occupant)
            {
                if (selected && occupant != selected)
                {
                    message = "Cell occupied.";
                    return;
                }

                selected = occupant;
                message = $"{selected.kind} selected.";
                return;
            }

            if (!selected || !selected.Alive)
            {
                selected = null;
                message = "Select a living member.";
                return;
            }

            selected.transform.position = board.CellCenter(cell);
            selected.RefreshCell();
            board.RefreshOccupancy(runLoop.Survivors);
            message = target ? $"{selected.kind} to {target.Address}." : $"{selected.kind} moved.";
        }

        bool IsInsideMaximumBoard(Vector2Int cell) =>
            cell.x >= 0 && cell.y >= 0 && cell.x < board.width && cell.y < board.height;

        void ReadKeyboardSelection()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.digit1Key.wasPressedThisFrame) SelectMember(0);
            else if (keyboard.digit2Key.wasPressedThisFrame) SelectMember(1);
            else if (keyboard.digit3Key.wasPressedThisFrame) SelectMember(2);
            else if (keyboard.digit4Key.wasPressedThisFrame) SelectMember(3);
        }

        void SelectMember(int index)
        {
            if (index < 0 || index >= runLoop.Survivors.Count) return;
            GridActor member = runLoop.Survivors[index];
            if (!member || !member.Alive) return;
            selected = member;
            message = $"{selected.kind} selected.";
        }

        public void SelectActorFromUI(GridActor actor)
        {
            if (!runLoop || !runLoop.CanPlaceTeam || !actor || !actor.Alive) return;
            selected = actor;
            message = $"{selected.kind} selected.";
        }

        bool IsPointerOverBlockingUI(Vector2 screenPosition)
        {
            if (!EventSystem.current) return false;
            var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (RaycastResult result in results)
                if (!result.gameObject.GetComponent<BoardDropSurface>()) return true;
            return false;
        }

        static string DisplayName(ActorKind kind) => kind == ActorKind.Pufferfish ? "Puff" : kind.ToString();

        void OnGUI()
        {
            if (!showLegacyPanel || !runLoop) return;

            Rect panel = PanelRect;
            GUI.Box(panel, string.Empty);
            GUI.Label(new Rect(panel.x + 10f, panel.y + 7f, PanelWidth - 20f, 22f),
                runLoop.CanPlaceTeam ? "TEAM PLACEMENT" : "TEAM ROSTER");
            string instruction = runLoop.CanPlaceTeam ? "Choose member; click empty cell.\nSpace starts; 1-4 selects." :
                runLoop.State == RunState.Running ? "Space pauses. R restarts." :
                runLoop.State == RunState.Paused ? "Space resumes. R restarts." : "R restarts.";
            GUI.Label(new Rect(panel.x + 10f, panel.y + 29f, PanelWidth - 20f, 34f),
                instruction);

            for (int i = 0; i < runLoop.Survivors.Count; i++)
            {
                GridActor member = runLoop.Survivors[i];
                if (!member) continue;

                Rect buttonRect = new Rect(panel.x + 10f, panel.y + 66f + i * 27f, PanelWidth - 20f, 24f);
                string cell = member.IsOnBoard && board && board.GetCell(member.CurrentCell)
                    ? board.GetCell(member.CurrentCell).Address
                    : "off board";
                string label = $"{i + 1} {DisplayName(member.kind)} {Mathf.CeilToInt(member.Health)}/{member.maxHealth} {cell}";

                if (runLoop.CanPlaceTeam)
                {
                    if (GUI.Button(buttonRect, selected == member ? $"> {label}" : label)) SelectMember(i);
                }
                else GUI.Label(buttonRect, label);
            }

            GUI.Label(new Rect(panel.x + 10f, panel.y + 177f, PanelWidth - 20f, 26f), message);
        }
    }
}
