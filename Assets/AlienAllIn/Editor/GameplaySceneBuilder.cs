using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AlienAllIn.Editor
{
    // Authoring tool only. The saved scene has real serialized objects; nothing is generated on Play.
    public static class GameplaySceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Gameplay.unity";
        const string ArtPath = "Assets/AlienAllIn/Art";
        static Sprite square, circle, ring;
        static Material spriteMaterial;
        static Font font;
        static readonly Color Ink = new Color(.045f, .075f, .13f);
        static readonly Color Mint = new Color(.40f, .94f, .77f);
        static readonly Color Gold = new Color(1f, .78f, .25f);
        static readonly Color Cream = new Color(.94f, .95f, .88f);
        static readonly Color Muted = new Color(.63f, .76f, .75f);
        static readonly Color Panel = new Color(.07f, .12f, .16f);
        static readonly Color Raised = new Color(.10f, .17f, .21f);
        static readonly Color Track = new Color(.15f, .24f, .28f);

        [MenuItem("Alien All In/Rebuild Gameplay HUD (preserves board)")]
        public static void RebuildGameplayHUD()
        {
            if (!EditorUtility.DisplayDialog("Rebuild Gameplay HUD", "This replaces only the HUD in Gameplay.unity and keeps the board and actors.", "Rebuild HUD", "Cancel")) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            square = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/Square.png");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameplayController game = Object.FindAnyObjectByType<GameplayController>();
            if (!game)
            {
                Debug.LogError("ALIEN ALL IN: GameplayController was not found in Gameplay.unity.");
                return;
            }

            if (game.hud) Object.DestroyImmediate(game.hud.gameObject);
            game.hud = MakeHUD(game);
            game.ResetStage();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("ALIEN ALL IN: replaced Gameplay HUD while preserving the board and actors.");
        }

        [MenuItem("Alien All In/Rebuild Gameplay scene (replaces scene)")]
        public static void BuildScene()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog("Rebuild Gameplay", "This recreates Gameplay.unity. Save a copy first to keep your scene edits.", "Rebuild", "Cancel")) return;
            Directory.CreateDirectory(ArtPath);
            Directory.CreateDirectory("Assets/AlienAllIn/Prefabs");
            square = MakeSprite("Square", 0); circle = MakeSprite("Circle", 1); ring = MakeSprite("Ring", 2);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string materialPath = ArtPath + "/UnlitSprites.mat";
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!spriteMaterial)
            {
                spriteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(spriteMaterial, materialPath);
            }
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            camera.orthographic = true; camera.orthographicSize = 5.65f;
            camera.rect = new Rect(0, .10f, .71f, .79f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ink;

            BoardGrid board = new GameObject("Board").AddComponent<BoardGrid>();
            board.transform.position = new Vector3(-5, -5);
            for (int y = 0; y < 10; y++)
            for (int x = 0; x < 10; x++)
            {
                GameObject tile = new GameObject($"Cell {(char)('A' + x)}{y + 1} ({x}, {y})");
                tile.transform.SetParent(board.transform, false);
                tile.transform.localPosition = new Vector3(x + .5f, y + .5f);
                var cell = tile.AddComponent<BoardCell>(); cell.coordinate = new Vector2Int(x, y);
                cell.baseColor = (x + y) % 2 == 0 ? new Color(.12f, .22f, .28f) : new Color(.14f, .26f, .31f);
                cell.surface = Shape("Surface", tile.transform, square, Vector2.zero, new Vector2(.96f, .96f), cell.baseColor, 0);
            }
            board.RebuildIndex();
            Transform allies = new GameObject("Team").transform;
            GridActor duck = MakeActor("Duck", ActorKind.Duck, allies);
            duck.board = board; duck.maxHealth = 25; duck.moveSpeed = 2.7f;
            duck.transform.position = board.CellCenter(new Vector2Int(4, 4));
            GridActor seal = MakeActor("Seal", ActorKind.Seal, allies);
            seal.board = board; seal.maxHealth = 45; seal.damage = 4; seal.attackRange = 2.7f; seal.attackInterval = .75f;
            seal.transform.position = board.CellCenter(new Vector2Int(5, 4));
            GridActor enemy = MakeActor("Enemy", ActorKind.Enemy, null);
            enemy.maxHealth = 10; enemy.moveSpeed = .8f; enemy.damage = 3; enemy.attackRange = .65f; enemy.attackInterval = 1.2f;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy.gameObject, "Assets/AlienAllIn/Prefabs/Enemy.prefab");
            Object.DestroyImmediate(enemy.gameObject);
            Transform enemies = new GameObject("Enemies").transform;
            Transform activityRoot = new GameObject("Activities").transform;
            ActivityStation casino = MakeActivity("CASINO", ActorKind.Duck, board, new Vector2Int(4, 6), 36, activityRoot);
            ActivityStation rope = MakeActivity("JUMP ROPE", ActorKind.Seal, board, new Vector2Int(6, 6), 42, activityRoot);
            Transform pickupRoot = new GameObject("Pickups").transform;
            FieldPickup pickup = new GameObject("Trampoline supply", typeof(FieldPickup)).GetComponent<FieldPickup>();
            pickup.transform.SetParent(pickupRoot); pickup.transform.position = board.CellCenter(new Vector2Int(6, 4));
            Shape("Supply marker", pickup.transform, ring, Vector2.zero, new Vector2(.54f, .54f), Mint, 3);

            GameplayController game = new GameObject("Game Controller").AddComponent<GameplayController>();
            game.board = board; game.gameCamera = camera; game.duck = duck; game.seal = seal;
            game.runDuration = 300; game.openingBoardSize = 3; game.middleBoardSize = 5; game.lateBoardSize = 7;
            game.maxBoardCameraSize = camera.orthographicSize;
            game.enemyPrefab = prefab.GetComponent<GridActor>(); game.enemyContainer = enemies;
            game.activities = new[] { casino, rope }; game.pickups = new[] { pickup };
            game.hud = MakeHUD(game);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            game.ResetStage();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false) };
            AssetDatabase.SaveAssets();
            // Open at the board, with a collapsed hierarchy and a useful 2D Scene view.
            if (SceneView.lastActiveSceneView)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.LookAt(Vector3.zero, Quaternion.identity, 7);
            }
            Selection.activeGameObject = game.gameObject;
            Debug.Log("ALIEN ALL IN: saved editable Gameplay.unity with 100 cells, duck, seal, activities, supply, prefab and HUD.");
        }

        static Sprite MakeSprite(string name, int shape)
        {
            string path = ArtPath + "/" + name + ".png";
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float distance = new Vector2(x - 31.5f, y - 31.5f).magnitude;
                float alpha = shape == 0 ? 1 : Mathf.Clamp01(31.5f - distance);
                if (shape == 2) alpha *= Mathf.Clamp01(distance - 26);
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static SpriteRenderer Shape(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name, typeof(SpriteRenderer)); go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            renderer.sortingOrder = order; renderer.sharedMaterial = spriteMaterial;
            return renderer;
        }

        static GridActor MakeActor(string name, ActorKind kind, Transform parent)
        {
            var actor = new GameObject(name, typeof(GridActor)).GetComponent<GridActor>();
            actor.kind = kind; actor.transform.SetParent(parent, false);
            actor.selectionRing = Shape("Selection", actor.transform, ring, Vector2.zero, new Vector2(.95f, .95f), Mint, 5);
            actor.attackEffect = Shape("Attack radius flash", actor.transform, ring, Vector2.zero, Vector2.one, new Color(.4f, .94f, .77f, .4f), 4);
            actor.attackEffect.enabled = false;
            Shape("Shadow", actor.transform, circle, new Vector2(0, -.23f), new Vector2(.73f, .25f), new Color(0, 0, 0, .35f), 6);
            actor.art = new GameObject("Art - simple editable sprites").transform; actor.art.SetParent(actor.transform, false);
            if (kind == ActorKind.Duck)
            {
                Color yellow = new Color(1, .82f, .23f), orange = new Color(1, .45f, .12f);
                Shape("Body", actor.art, circle, Vector2.zero, new Vector2(.63f, .47f), yellow, 10);
                Shape("Belly", actor.art, circle, new Vector2(-.05f, -.07f), new Vector2(.38f, .23f), new Color(1, .95f, .65f), 11);
                Shape("Head", actor.art, circle, new Vector2(.13f, .22f), new Vector2(.36f, .36f), yellow, 12);
                Shape("Bill", actor.art, square, new Vector2(.33f, .16f), new Vector2(.23f, .11f), orange, 13);
                Shape("Eye", actor.art, circle, new Vector2(.18f, .26f), new Vector2(.055f, .065f), Ink, 14);
                Shape("Left foot", actor.art, square, new Vector2(-.13f, -.25f), new Vector2(.17f, .08f), orange, 11);
                Shape("Right foot", actor.art, square, new Vector2(.13f, -.25f), new Vector2(.17f, .08f), orange, 11);
            }
            else if (kind == ActorKind.Seal)
            {
                Shape("Body", actor.art, circle, Vector2.zero, new Vector2(.72f, .46f), Mint, 10);
                Shape("Left flipper", actor.art, circle, new Vector2(-.31f, -.15f), new Vector2(.27f, .12f), Mint, 11);
                Shape("Right flipper", actor.art, circle, new Vector2(.31f, -.15f), new Vector2(.27f, .12f), Mint, 11);
                Shape("Head", actor.art, circle, new Vector2(0, .18f), new Vector2(.43f, .38f), Mint, 12);
                Shape("Muzzle", actor.art, circle, new Vector2(0, .12f), new Vector2(.24f, .15f), Color.white, 13);
                Shape("Nose", actor.art, circle, new Vector2(0, .18f), new Vector2(.07f, .05f), Ink, 14);
                Shape("Left eye", actor.art, circle, new Vector2(-.11f, .27f), new Vector2(.045f, .05f), Ink, 14);
                Shape("Right eye", actor.art, circle, new Vector2(.11f, .27f), new Vector2(.045f, .05f), Ink, 14);
            }
            else
            {
                Shape("Slime", actor.art, circle, Vector2.zero, new Vector2(.6f, .58f), new Color(.96f, .38f, .51f), 10);
                Shape("Left eye", actor.art, circle, new Vector2(-.12f, .1f), new Vector2(.12f, .16f), Color.white, 11);
                Shape("Right eye", actor.art, circle, new Vector2(.12f, .1f), new Vector2(.12f, .16f), Color.white, 11);
                Shape("Left pupil", actor.art, circle, new Vector2(-.12f, .08f), new Vector2(.05f, .07f), Ink, 12);
                Shape("Right pupil", actor.art, circle, new Vector2(.12f, .08f), new Vector2(.05f, .07f), Ink, 12);
            }
            return actor;
        }

        static ActivityStation MakeActivity(string name, ActorKind kind, BoardGrid board, Vector2Int cell, float duration, Transform parent)
        {
            var station = new GameObject(name, typeof(ActivityStation)).GetComponent<ActivityStation>();
            station.transform.SetParent(parent); station.transform.position = board.CellCenter(cell);
            station.activityName = name; station.requiredKind = kind; station.duration = duration; station.board = board;
            station.marker = Shape("Station border", station.transform, square, Vector2.zero, new Vector2(.90f, .90f), Color.white, 2);
            Shape("Station inset", station.transform, square, Vector2.zero, new Vector2(.81f, .81f), new Color(.21f, .28f, .28f), 3);
            station.ResetActivity();
            return station;
        }

        static GameplayHUD MakeHUD(GameplayController game)
        {
            var canvasObject = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameplayHUD));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            var hud = canvasObject.GetComponent<GameplayHUD>(); hud.game = game;
            Transform parent = canvasObject.transform;
            AddPanel("Top rail", parent, 0, .90f, 1, 1, Ink);
            AddPanel("Status rail", parent, 0, 0, .71f, .095f, Ink);
            AddPanel("Game controls", parent, .72f, .105f, .985f, .90f, Panel);
            AddPanel("Panel top line", parent, .72f, .895f, .985f, .90f, Mint);

            Label("ALIEN ALL IN", parent, .04f, .915f, .67f, .985f, 38, Cream, FontStyle.Bold, TextAnchor.MiddleLeft);
            hud.phaseText = Label("", parent, .72f, .915f, .975f, .985f, 20, Mint, FontStyle.Bold, TextAnchor.MiddleRight);
            hud.cashText = Label("000", parent, .84f, .822f, .975f, .89f, 32, Gold, FontStyle.Bold, TextAnchor.MiddleRight);
            Label("CASH", parent, .745f, .822f, .835f, .89f, 16, Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
            AddPanel("Cash divider", parent, .745f, .807f, .975f, .81f, Track);
            hud.objectiveText = Label("OBJECTIVES", parent, .745f, .765f, .975f, .80f, 18, Cream, FontStyle.Bold, TextAnchor.MiddleLeft);

            hud.activityRows = new GameplayActivityReadout[game.activities.Length];
            for (int i = 0; i < game.activities.Length; i++)
            {
                float top = .75f - i * .112f;
                hud.activityRows[i] = ActivityReadout(game.activities[i], parent, top - .095f, top);
            }

            AddPanel("Crew divider", parent, .745f, .522f, .975f, .525f, Track);
            Label("CREW", parent, .745f, .485f, .975f, .518f, 17, Cream, FontStyle.Bold, TextAnchor.MiddleLeft);
            hud.selectedText = Label("", parent, .745f, .402f, .975f, .48f, 17, Cream, FontStyle.Bold, TextAnchor.MiddleLeft);
            AddPanel("Health track", parent, .745f, .389f, .975f, .399f, Track);
            hud.selectedHealthFill = AddFill("Health fill", parent, .745f, .389f, .975f, .399f);

            hud.duckButton = Button("DUCK", parent, .745f, .34f, .854f, .386f, Raised, Muted, 15);
            hud.sealButton = Button("SEAL", parent, .866f, .34f, .975f, .386f, Raised, Muted, 15);
            hud.startButton = Button("START RUN", parent, .745f, .225f, .975f, .275f, Mint, Ink, 18, "Start Run");
            hud.trampolineButton = Button("", parent, .745f, .166f, .975f, .216f, new Color(.52f, .81f, .93f), Ink, 15, "Use Trampoline");
            hud.toolText = hud.trampolineButton.GetComponentInChildren<Text>();
            hud.pauseButton = Button("PAUSE", parent, .745f, .112f, .854f, .157f, Raised, Cream, 14);
            hud.restartButton = Button("RESTART", parent, .866f, .112f, .975f, .157f, Raised, Cream, 14);
            hud.messageText = Label("", parent, .04f, .022f, .68f, .073f, 17, Cream, FontStyle.Normal, TextAnchor.MiddleLeft);
            return hud;
        }

        static GameplayActivityReadout ActivityReadout(ActivityStation activity, Transform parent, float y0, float y1)
        {
            RectTransform root = Rect(activity.activityName + " readout", parent, .745f, y0, .975f, y1);
            var row = new GameplayActivityReadout { root = root.gameObject };
            Color accent = activity.requiredKind == ActorKind.Duck ? Gold : Mint;
            row.nameText = Label(activity.activityName, root, 0, .52f, .68f, 1, 18, Cream, FontStyle.Bold, TextAnchor.MiddleLeft);
            row.progressText = Label("0%", root, .68f, .52f, 1, 1, 17, accent, FontStyle.Bold, TextAnchor.MiddleRight);
            row.kindText = Label(activity.requiredKind.ToString().ToUpperInvariant(), root, 0, .15f, 1, .49f, 13, Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
            AddPanel("Progress track", root, 0, 0, 1, .09f, Track);
            row.progressFill = AddFill("Progress fill", root, 0, 0, 1, .09f);
            return row;
        }

        static Image AddPanel(string name, Transform parent, float x0, float y0, float x1, float y1, Color color)
        {
            Image image = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Image AddFill(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            Image image = AddPanel(name, parent, x0, y0, x1, y1, Color.white);
            image.sprite = square;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 0;
            return image;
        }

        static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text Label(string text, Transform parent, float x0, float y0, float x1, float y1, int size, Color color,
            FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var label = Rect(string.IsNullOrEmpty(text) ? "Readout" : text, parent, x0, y0, x1, y1).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = size; label.fontStyle = style; label.text = text; label.color = color;
            label.alignment = alignment; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow; label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        static Button Button(string text, Transform parent, float x0, float y0, float x1, float y1,
            Color backgroundColor, Color textColor, int fontSize, string gameObjectName = null)
        {
            var rect = Rect(gameObjectName ?? text, parent, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, .12f);
            colors.pressedColor = Color.Lerp(backgroundColor, Ink, .18f);
            colors.disabledColor = Raised;
            colors.fadeDuration = .08f;
            button.colors = colors;
            var label = Label(text, rect, .04f, .08f, .96f, .92f, fontSize, textColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            return button;
        }
    }
}
