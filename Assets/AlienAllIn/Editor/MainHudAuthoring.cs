using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AlienAllIn.Editor
{
    /// <summary>One-time authoring utility. The resulting objects are saved in Main, never built at runtime.</summary>
    public static class MainHudAuthoring
    {
        static readonly Color Panel = new Color(.9f, .9f, .9f);
        static readonly Color Ink = new Color(.12f, .12f, .12f);
        static readonly Color ButtonColor = new Color(.7f, .7f, .7f);
        [MenuItem("Alien All In/Author Main scene HUD and attack visuals")]
        public static void Apply()
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            SceneManager.SetActiveScene(scene);
            var ui = FindInScene<MainCoreGameplayUI>(scene);
            var game = FindInScene<MainRunGameplaySystems>(scene);
            var loop = FindInScene<RunLoopController>(scene);
            var layout = FindInScene<MainSceneLayout>(scene);
            var board = FindInScene<BoardGrid>(scene);
            var camera = FindInScene<Camera>(scene);
            if (!ui || !game || !loop || !layout) throw new System.InvalidOperationException("Main is missing gameplay references.");
            foreach (string name in new[] { "Main Core Gameplay UI", "Debug Tools (O)" })
            {
                var old = ui.transform.Find(name); if (old) Object.DestroyImmediate(old.gameObject);
            }
            var canvas = Canvas("Main Core Gameplay UI", ui.transform, 100);
            var data = new SerializedObject(ui);
            Ref(data, "runLoop", loop); Ref(data, "systems", game); Ref(data, "board", board);
            Ref(data, "stageCamera", camera); Ref(data, "screenLayout", layout); Ref(data, "hudCanvas", canvas);
            var margins = new Object[4];
            for (int i = 0; i < 4; i++)
            {
                var margin = Region("Window Margin " + i, canvas.transform, layout.BackgroundColor, new Rect());
                margin.GetComponent<Image>().raycastTarget = false; margins[i] = margin.transform;
            }
            Refs(data, "backgroundRects", margins);
            var drop = Region("Board Drop Surface", canvas.transform, Color.clear, layout.BoardViewport);
            drop.GetComponent<Image>().raycastTarget = true; drop.AddComponent<BoardDropSurface>();
            Ref(data, "boardDropRect", drop.transform);
            var header = Region("Run HUD", canvas.transform, Panel, layout.Header);
            Ref(data, "headerRect", header.transform);
            Ref(data, "runText", Text("Run State", header.transform, new Rect(.015f, .54f, .33f, .4f), "READY  05:00  5x5", 22));
            Ref(data, "walletText", Text("Wallet", header.transform, new Rect(.35f, .54f, .30f, .4f), "Coins  0    Chips  0", 22));
            Ref(data, "scoreText", Text("Score", header.transform, new Rect(.015f, .08f, .16f, .3f), "Score  0", 17));
            Ref(data, "statusText", Text("Status", header.transform, new Rect(.18f, .08f, .50f, .3f), "Click a character. Drag characters to stations.", 16));
            Ref(data, "startButton", Button("Start", header.transform, new Rect(.69f, .20f, .09f, .6f), "Start"));
            Ref(data, "pauseButton", Button("Pause", header.transform, new Rect(.79f, .20f, .09f, .6f), "Pause"));
            Ref(data, "restartButton", Button("Restart", header.transform, new Rect(.89f, .20f, .095f, .6f), "Restart"));
            var left = Region("Left Sidebar", canvas.transform, Color.clear, layout.LeftColumn);
            Ref(data, "leftRect", left.transform);
            var cards = Region("Team Cards", left.transform, Color.clear, new Rect(0, .32f, 1, .68f));
            var stack = cards.AddComponent<VerticalLayoutGroup>(); stack.spacing = 8;
            stack.childControlWidth = stack.childControlHeight = stack.childForceExpandWidth = stack.childForceExpandHeight = true;
            var actors = new Object[4]; var buttons = new Object[4]; var texts = new Object[4]; var bars = new Object[4];
            var kinds = new[] { ActorKind.Duck, ActorKind.Pufferfish, ActorKind.Cat, ActorKind.Seal };
            var roster = new SerializedObject(loop).FindProperty("survivingAliens");
            for (int i = 0; i < 4; i++)
            {
                GridActor actor = null;
                for (int j = 0; j < roster.arraySize; j++)
                {
                    var candidate = roster.GetArrayElementAtIndex(j).objectReferenceValue as GridActor;
                    if (candidate && candidate.kind == kinds[i]) actor = candidate;
                }
                if (!actor) throw new System.InvalidOperationException("Missing team member " + kinds[i]);
                actors[i] = actor;
                if (actor.kind == ActorKind.Duck) { actor.invulnerable = true; actor.countsAsActiveRunMember = false; actor.damage = 0; EditorUtility.SetDirty(actor); }
                var card = Region(kinds[i] + " Card", cards.transform, Panel, new Rect(0, 0, 1, 1));
                var button = card.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>(); buttons[i] = button;
                card.AddComponent<LayoutElement>().flexibleHeight = 1;
                card.AddComponent<CanvasGroup>();
                card.AddComponent<UIResourceDragToken>().SetActor(actor); card.AddComponent<AlienDropTarget>();
                texts[i] = Text(kinds[i] + " Info", card.transform, new Rect(.06f, .22f, .88f, .72f), kinds[i] == ActorKind.Duck ? "Duck\nStation worker" : kinds[i] + "\nHP 30/30  ATK 4.0\nIdle", 20);
                var track = Region("Ultimate Track", card.transform, ButtonColor, new Rect(.06f, .09f, .88f, .055f));
                var bar = Region("Ultimate", track.transform, new Color(.35f, .35f, .35f), new Rect(0, 0, 0, 1)).GetComponent<Image>();
                bar.raycastTarget = false; bars[i] = bar;
            }
            Refs(data, "cardActors", actors); Refs(data, "cardButtons", buttons); Refs(data, "cardTexts", texts); Refs(data, "cardBars", bars);
            var process = Region("Process Status", left.transform, Panel, new Rect(0, 0, 1, .30f));
            Text("Process Header", process.transform, new Rect(.06f, .81f, .88f, .15f), "STATIONS / ENEMIES", 16);
            var viewport = Region("Process List", process.transform, Color.clear, new Rect(.06f, .05f, .88f, .72f));
            viewport.AddComponent<RectMask2D>();
            Ref(data, "processText", Text("Live Status", viewport.transform, new Rect(0, 0, 1, 1), "Enemies  0", 17));
            var right = Region("Right Sidebar", canvas.transform, Color.clear, layout.RightColumn); Ref(data, "rightRect", right.transform);
            var items = Region("Items", right.transform, Panel, new Rect(0, .71f, 1, .29f));
            Text("Items Header", items.transform, new Rect(.06f, .80f, .88f, .15f), "ITEMS", 18);
            var itemButtons = new Object[3]; var itemTexts = new Object[3];
            string[] itemNames = { "Air Pump", "Laser Gun", "Trampoline" };
            for (int i = 0; i < 3; i++)
            {
                var button = Button("Item Token " + (FieldItemKind)i, items.transform, new Rect(.06f, .54f - .23f * i, .88f, .20f), itemNames[i] + "  x0");
                button.gameObject.AddComponent<UIResourceDragToken>().SetItem((FieldItemKind)i);
                button.gameObject.AddComponent<CanvasGroup>();
                itemButtons[i] = button; itemTexts[i] = button.GetComponentInChildren<Text>();
            }
            Refs(data, "itemButtons", itemButtons); Refs(data, "itemTexts", itemTexts);
            var result = Region("Session Result", canvas.transform, Panel, layout.BoardViewport);
            Ref(data, "resultPanel", result);
            var resultText = Text("Final Score", result.transform, new Rect(.1f, .40f, .80f, .40f), "TIME UP\nSCORE 0", 32); resultText.alignment = TextAnchor.MiddleCenter;
            Ref(data, "resultText", resultText); Ref(data, "playAgain", Button("PLAY AGAIN", result.transform, new Rect(.25f, .22f, .50f, .10f), "Play again")); result.SetActive(false);
            var overlay = Region("Character Popup", canvas.transform, Color.clear, new Rect(0, 0, 1, 1)); Ref(data, "popupOverlay", overlay);
            var outside = Region("Click Outside", overlay.transform, new Color(0, 0, 0, .15f), new Rect(0, 0, 1, 1));
            var outsideButton = outside.AddComponent<Button>(); outsideButton.targetGraphic = outside.GetComponent<Image>();
            var fade = outsideButton.colors; fade.normalColor = fade.highlightedColor = fade.pressedColor = Color.white; outsideButton.colors = fade; Ref(data, "popupOutside", outsideButton);
            var box = Region("Upgrade Box", overlay.transform, Panel, new Rect(.56f, .30f, .29f, .40f)); Ref(data, "popupBox", box.transform);
            Ref(data, "popupClose", Button("Close", box.transform, new Rect(.85f, .84f, .11f, .12f), "X"));
            var portrait = Region("Portrait", box.transform, Color.white, new Rect(.05f, .57f, .19f, .33f)).GetComponent<Image>();
            portrait.preserveAspect = true; portrait.sprite = ((GridActor)actors[3]).GetComponentInChildren<SpriteRenderer>().sprite; Ref(data, "popupPortrait", portrait);
            Ref(data, "popupName", Text("Character Name", box.transform, new Rect(.29f, .77f, .52f, .15f), "Seal", 28));
            Ref(data, "popupStats", Text("HP and ATK", box.transform, new Rect(.29f, .57f, .65f, .16f), "HP: 30/30  ATK: 4.0", 20));
            Ref(data, "popupDetails", Text("Upgrade Details", box.transform, new Rect(.06f, .30f, .88f, .22f), "ATK +0.1   Level 0\nCoins: 0   Next: 10", 19));
            var upgrade = Button("Upgrade", box.transform, new Rect(.06f, .07f, .88f, .17f), "Upgrade (10 Coins)");
            Ref(data, "popupUpgrade", upgrade); Ref(data, "popupAction", upgrade.GetComponentInChildren<Text>()); overlay.SetActive(false);
            var debug = ui.GetComponent<MainDebugPanel>(); if (!debug) debug = ui.gameObject.AddComponent<MainDebugPanel>();
            debug.BuildSavedSceneUI(); EditorUtility.SetDirty(debug); Ref(data, "debugPanel", debug);
            data.ApplyModifiedPropertiesWithoutUndo();
            var eventSystem = FindInScene<EventSystem>(scene);
            if (!eventSystem) eventSystem = new GameObject("Event System", typeof(EventSystem)).GetComponent<EventSystem>();
            if (!eventSystem.GetComponent<BaseInputModule>()) eventSystem.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var oldVfx = game.transform.Find("Attack VFX"); if (oldVfx) Object.DestroyImmediate(oldVfx.gameObject);
            var vfxRoot = new GameObject("Attack VFX"); vfxRoot.transform.SetParent(game.transform, false);
            var vfx = vfxRoot.AddComponent<AttackVfx>(); var vfxData = new SerializedObject(vfx);
            var laser = Line("Cat - Laser", vfxRoot.transform, new Color(.95f, .12f, .18f), .06f); laser.positionCount = 2;
            var cat = (GridActor)actors[2]; laser.SetPosition(0, cat.transform.position);
            laser.SetPosition(1, cat.transform.position + Vector3.right * cat.attackRange);
            Ref(vfxData, "catLaser", laser); vfxData.ApplyModifiedPropertiesWithoutUndo();
            var rules = new SerializedObject(game); Ref(rules, "attackVfx", vfx);
            rules.FindProperty("damageUpgradeBaseCost").intValue = 10; rules.FindProperty("damageUpgradeAmount").floatValue = .1f;
            rules.ApplyModifiedPropertiesWithoutUndo();
            ReferenceHudAuthoring.Apply(ui, game, board, camera);
            EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Saved editable Main HUD, character popup, debug tools and attack VFX in the scene.");
        }
        static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true); if (component) return component;
            }
            return null;
        }
        static LineRenderer Line(string name, Transform parent, Color color, float width)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/AlienAllIn/Art/UnlitSprites.mat");
            line.useWorldSpace = true; line.startWidth = line.endWidth = width; line.startColor = line.endColor = color;
            line.sortingOrder = 12; line.numCapVertices = 3; line.enabled = false; return line;
        }
        static Canvas Canvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f; return canvas;
        }
        static GameObject Region(string name, Transform parent, Color color, Rect bounds)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); Rect(go, bounds);
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = color.a > 0; return go;
        }
        static Text Text(string name, Transform parent, Rect bounds, string value, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); Rect(go, bounds);
            var text = go.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size;
            text.color = Ink; text.alignment = TextAnchor.MiddleLeft; text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
            text.raycastTarget = false; return text;
        }
        static Button Button(string name, Transform parent, Rect bounds, string title)
        {
            var go = Region(name, parent, ButtonColor, bounds); var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
            Text("Label", go.transform, new Rect(.025f, .05f, .95f, .90f), title, 19).alignment = TextAnchor.MiddleCenter; return button;
        }
        static void Rect(GameObject go, Rect bounds) { var rect = go.GetComponent<RectTransform>(); rect.anchorMin = bounds.min; rect.anchorMax = bounds.max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        static void Ref(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        static void Refs(SerializedObject data, string field, Object[] values)
        {
            var array = data.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
