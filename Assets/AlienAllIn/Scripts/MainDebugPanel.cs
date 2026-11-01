using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AlienAllIn
{
    /// <summary>Runtime-only O-key test tools. Hidden by default; available in Editor/Development Builds.</summary>
    [DisallowMultipleComponent]
    public sealed class MainDebugPanel : MonoBehaviour
    {
        [SerializeField] RunLoopController runLoop;
        [SerializeField] MainRunGameplaySystems systems;
        [SerializeField] bool allowInReleaseBuild;
        [SerializeField] GameObject root;
        bool bound;
#if UNITY_EDITOR
        bool authoringScene;
#endif
        Transform content;
        [SerializeField] Text stateText, feedbackText, timerToggle, enemyToggle, stationToggle;
        string feedback = "Scroll for more tools. Actions work in Placement, Running and Paused. O / Escape closes.";
        float refreshClock;
        public bool IsAvailable => Application.isEditor || Debug.isDebugBuild || allowInReleaseBuild;
        public bool IsOpen => root && root.activeSelf;

        public void Configure(RunLoopController loop, MainRunGameplaySystems game, bool allowInRelease = false)
        {
            runLoop = loop; systems = game; allowInReleaseBuild = allowInRelease;
            if (root) root.SetActive(false);
            if (!root || bound) return;
            content = root.transform.Find("Input blocker/Window/Actions/Content");
            BindActions(); bound = true;
            root.transform.Find("Input blocker/Window/Close").GetComponent<Button>().onClick.AddListener(() => root.SetActive(false));
        }

        public void Toggle()
        {
            if (!IsAvailable || !runLoop || !systems) return;
            if (!root) return;
            if (!IsOpen)
            {
                var ui = GetComponent<MainCoreGameplayUI>();
                ui?.CloseCharacter();
                if (ui && ui.OptionsOpen) ui.CloseOptions();
            }
            root.SetActive(!root.activeSelf);
            Refresh();
        }

        void Update()
        {
            if (!IsAvailable) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.oKey.wasPressedThisFrame) Toggle();
            if (!IsOpen) return;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) root.SetActive(false);
            refreshClock -= Time.unscaledDeltaTime;
            if (refreshClock > 0) return;
            refreshClock = .1f;
            Refresh();
        }

#if UNITY_EDITOR
        public void BuildSavedSceneUI()

        {
            authoringScene = true;
            root = new GameObject("Debug Tools (O)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 1000;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            // This full-screen scrim blocks clicks and drops from reaching the game beneath the tools.
            var scrim = Image("Input blocker", root.transform, new Rect(0, 0, 1, 1), new Color(0, 0, 0, .45f));
            var window = Image("Window", scrim.transform, new Rect(.10f, .06f, .80f, .88f), new Color(.95f, .95f, .95f));
            Label("DEBUG TOOLS  /  O", window.transform, new Rect(.02f, .93f, .65f, .05f), 24);
            Button("Close", window.transform, new Rect(.87f, .93f, .11f, .05f), () => root.SetActive(false));
            stateText = Label("", window.transform, new Rect(.02f, .87f, .96f, .05f), 17);
            feedbackText = Label("", window.transform, new Rect(.02f, .015f, .96f, .08f), 16);
            var viewport = Image("Actions", window.transform, new Rect(.02f, .11f, .93f, .74f), new Color(.95f, .95f, .95f));
            viewport.AddComponent<RectMask2D>();
            var scroll = viewport.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.scrollSensitivity = 35;
            scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.movementType = ScrollRect.MovementType.Clamped;
            var stack = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            stack.transform.SetParent(viewport.transform, false);
            var rect = stack.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, 1); rect.sizeDelta = Vector2.zero;
            var vertical = stack.GetComponent<VerticalLayoutGroup>(); vertical.spacing = 8;
            vertical.childControlHeight = vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false; vertical.childForceExpandWidth = true;
            stack.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content = stack.transform; scroll.content = rect;
            var track = Image("Scrollbar", window.transform, new Rect(.963f, .11f, .017f, .74f), new Color(.85f, .85f, .85f));
            var handle = Image("Handle", track.transform, new Rect(0, 0, 1, 1), new Color(.48f, .48f, .48f));
            var scrollbar = track.AddComponent<Scrollbar>(); scrollbar.handleRect = handle.GetComponent<RectTransform>();
            scrollbar.targetGraphic = handle.GetComponent<Image>(); scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            BindActions();
            timerToggle.text = "Freeze timer: off"; enemyToggle.text = "Enemy spawns: on"; stationToggle.text = "Station rolls: on";
            var note = Label("Shrinking relocates allies and removes off-board objects. Restart clears all debug overrides.", content, new Rect(0, 0, 1, 1), 15);
            note.gameObject.AddComponent<LayoutElement>().preferredHeight = 38;
            root.SetActive(false); authoringScene = false;
        }
#endif

        void BindActions()
        {
            Row("Session", ("Start", () => runLoop.StartRun()), ("Pause / Resume", PauseResume), ("Restart", () => runLoop.Restart()));
            Row("Spawn enemies", ("Random", () => systems.DebugSpawnEnemy()),
                ("Roaster", () => systems.DebugSpawnEnemy(EnemyArchetype.RoasterMelee)),
                ("Toaster", () => systems.DebugSpawnEnemy(EnemyArchetype.ToasterRanged)),
                ("Pan Shield", () => systems.DebugSpawnEnemy(EnemyArchetype.PanShield)));
            Row("Enemy actions", ("Spawn 5", SpawnFive), ("Kill all (+score)", () => systems.DebugKillEnemies()),
                ("Clear enemies (no score)", () => systems.DebugKillEnemies(false)), ("Clear world", () => systems.DebugClearWorld()));
            Row("Spawn stations", ("Casino", () => systems.DebugSpawnStation(ActivityKind.Casino)),
                ("Darts", () => systems.DebugSpawnStation(ActivityKind.Darts)),
                ("Jump Rope", () => systems.DebugSpawnStation(ActivityKind.JumpRope)), ("All three", SpawnStations));
            Row("Board", ("Reduce -1", () => systems.DebugSetBoardSize(systems.Board.ActiveWidth - 1)),
                ("Expand +1", () => systems.DebugSetBoardSize(systems.Board.ActiveWidth + 1)),
                ("5x5", () => systems.DebugSetBoardSize(5)), ("7x7", () => systems.DebugSetBoardSize(7)),
                ("10x10", () => systems.DebugSetBoardSize(10)), ("Auto", () => systems.DebugUseAutomaticBoard()));
            Row("Timer", ("-60 sec", () => AdjustTime(-60)), ("-30 sec", () => AdjustTime(-30)),
                ("+30 sec", () => AdjustTime(30)), ("+60 sec", () => AdjustTime(60)),
                ("Set 5:00", () => SetTime(300)), ("Time up", () => SetTime(0)));
            Row("Resources", ("+100 Coins", () => systems.DebugGrantCash()), ("+5 each Item", () => systems.DebugGrantItems()),
                ("+5 Chips", () => systems.DebugGrantChips()), ("+1000 Score", () => systems.DebugGrantScore()));
            Row("Team", ("Heal / Revive", () => systems.DebugRestoreTeam()), ("Fill Ultimates", () => systems.DebugFillUltimates()));
            var toggles = Row("Automatic systems", ("", () => runLoop.DebugFreezeTimer = !runLoop.DebugFreezeTimer),
                ("", () => systems.DebugStopEnemySpawns = !systems.DebugStopEnemySpawns),
                ("", () => systems.DebugStopStationSpawns = !systems.DebugStopStationSpawns));
            timerToggle = toggles[0]; enemyToggle = toggles[1]; stationToggle = toggles[2];
        }

        void PauseResume()
        {
            if (runLoop.State == RunState.Running) runLoop.Pause();
            else if (runLoop.State == RunState.Paused) runLoop.Resume();
        }

        void SpawnFive()
        {
            int count = 0;
            for (int i = 0; i < 5; i++) if (systems.DebugSpawnEnemy()) count++;
            systems.SetStatusFromUI($"DEBUG / Spawned {count}/5 enemies (free cells required).");
        }

        void SpawnStations()
        {
            int count = 0;
            foreach (ActivityKind kind in System.Enum.GetValues(typeof(ActivityKind))) if (systems.DebugSpawnStation(kind)) count++;
            systems.SetStatusFromUI($"DEBUG / Spawned {count}/3 stations (free cells required).");
        }

        void AdjustTime(float delta)
        {
            if (runLoop.DebugAddTime(delta)) feedback = $"Timer adjusted by {delta:+0;-0} seconds. Elapsed play and spawn clocks kept.";
            else feedback = "Restart before adjusting a finished session's timer.";
        }

        void SetTime(float seconds)
        {
            feedback = runLoop.DebugSetRemainingTime(seconds) ? $"Timer set to {seconds:0} seconds remaining." : "Restart before changing a finished session's timer.";
        }

        void Refresh()
        {
            if (!stateText || !runLoop || !systems) return;
            int seconds = Mathf.CeilToInt(runLoop.TimeRemainingSeconds);
            stateText.text = $"{runLoop.State}    Time {seconds / 60:00}:{seconds % 60:00}    Board {systems.Board.ActiveWidth}x{systems.Board.ActiveHeight} ({(systems.DebugBoardOverride ? "manual" : "auto")})    Enemies {systems.Enemies.Count}    Stations {systems.Stations.Count}";
            feedbackText.text = feedback;
            timerToggle.text = $"Freeze timer: {(runLoop.DebugFreezeTimer ? "ON" : "off")}";
            enemyToggle.text = $"Enemy spawns: {(systems.DebugStopEnemySpawns ? "OFF" : "on")}";
            stationToggle.text = $"Station rolls: {(systems.DebugStopStationSpawns ? "OFF" : "on")}";
        }

        Text[] Row(string title, params (string label, System.Action action)[] actions)
        {
#if UNITY_EDITOR
            if (authoringScene)
            {
                var heading = Label(title.ToUpperInvariant(), content, new Rect(0, 0, 1, 1), 15);
                heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;
                var row = Image(title, content, new Rect(0, 0, 1, 1), Color.clear);
                row.AddComponent<LayoutElement>().preferredHeight = 46;
                var horizontal = row.AddComponent<HorizontalLayoutGroup>(); horizontal.spacing = 8;
                horizontal.childControlWidth = horizontal.childControlHeight = true;
                horizontal.childForceExpandWidth = horizontal.childForceExpandHeight = true;
                var labels = new Text[actions.Length];
                for (int i = 0; i < actions.Length; i++)
                {
                    var button = Button(actions[i].label, row.transform, new Rect(0, 0, 1, 1), () => { });
                    button.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                    labels[i] = button.GetComponentInChildren<Text>();
                }
                return labels;
            }
#endif
            var savedRow = content.Find(title);
            var texts = new Text[actions.Length];
            for (int i = 0; i < actions.Length; i++)
            {
                var action = actions[i].action;
                var button = savedRow.GetChild(i).GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    string before = systems.StatusMessage; action();
                    if (systems.StatusMessage != before) feedback = systems.StatusMessage;
                    Refresh();
                });
                texts[i] = button.GetComponentInChildren<Text>();
            }
            return texts;
        }

#if UNITY_EDITOR
        static GameObject Image(string name, Transform parent, Rect bounds, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), bounds);
            go.GetComponent<Image>().color = color; go.GetComponent<Image>().raycastTarget = color.a > 0;
            return go;
        }

        static Text Label(string text, Transform parent, Rect bounds, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), bounds);
            var label = go.GetComponent<Text>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size; label.color = new Color(.12f, .12f, .12f); label.alignment = TextAnchor.MiddleLeft;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 12; label.resizeTextMaxSize = size;
            label.verticalOverflow = VerticalWrapMode.Truncate; label.raycastTarget = false;
            return label;
        }

        static Button Button(string name, Transform parent, Rect bounds, UnityEngine.Events.UnityAction action)
        {
            var go = Image(name.Length == 0 ? "Toggle" : name, parent, bounds, new Color(.81f, .81f, .81f));
            var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); button.onClick.AddListener(action);
            Label(name, go.transform, new Rect(.025f, .05f, .95f, .90f), 17).alignment = TextAnchor.MiddleCenter;
            return button;
        }

        static void SetRect(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = bounds.min; rect.anchorMax = bounds.max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
#endif
    }
}
