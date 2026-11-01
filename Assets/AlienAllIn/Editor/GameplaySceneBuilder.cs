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

            BoardGrid board = new GameObject("Board - 10 x 10 - 1 unit cells").AddComponent<BoardGrid>();
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
            Transform allies = new GameObject("Team - move these objects to change starting cells").transform;
            GridActor duck = MakeActor("Duck", ActorKind.Duck, allies);
            duck.board = board; duck.maxHealth = 25; duck.moveSpeed = 2.7f;
            duck.transform.position = board.CellCenter(new Vector2Int(2, 2));
            GridActor seal = MakeActor("Seal", ActorKind.Seal, allies);
            seal.board = board; seal.maxHealth = 45; seal.damage = 4; seal.attackRange = 2.7f; seal.attackInterval = .75f;
            seal.transform.position = board.CellCenter(new Vector2Int(5, 5));
            GridActor enemy = MakeActor("Enemy", ActorKind.Enemy, null);
            enemy.maxHealth = 10; enemy.moveSpeed = .8f; enemy.damage = 3; enemy.attackRange = .65f; enemy.attackInterval = 1.2f;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy.gameObject, "Assets/AlienAllIn/Prefabs/Enemy.prefab");
            Object.DestroyImmediate(enemy.gameObject);
            Transform enemies = new GameObject("Enemies - spawned during the shift").transform;
            Transform activityRoot = new GameObject("Activities - move stations to other cell centers").transform;
            ActivityStation casino = MakeActivity("CASINO", ActorKind.Duck, board, new Vector2Int(4, 5), 36, activityRoot);
            ActivityStation rope = MakeActivity("JUMP ROPE", ActorKind.Seal, board, new Vector2Int(5, 5), 42, activityRoot);
            Transform pickupRoot = new GameObject("Pickups - duck collects these").transform;
            FieldPickup pickup = new GameObject("Trampoline supply", typeof(FieldPickup)).GetComponent<FieldPickup>();
            pickup.transform.SetParent(pickupRoot); pickup.transform.position = board.CellCenter(new Vector2Int(2, 3));
            Shape("Supply marker", pickup.transform, ring, Vector2.zero, new Vector2(.54f, .54f), Mint, 3);

            GameplayController game = new GameObject("Gameplay - balance and scene references").AddComponent<GameplayController>();
            game.board = board; game.gameCamera = camera; game.duck = duck; game.seal = seal;
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
            var canvasObject = new GameObject("HUD - editable Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameplayHUD));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            var hud = canvasObject.GetComponent<GameplayHUD>(); hud.game = game;
            Transform parent = canvasObject.transform;
            var background = Rect("Sidebar", parent, .72f, .11f, .97f, .90f).gameObject.AddComponent<Image>(); background.color = new Color(.08f, .13f, .20f);
            Label("ALIEN ALL IN", parent, .045f, .93f, .69f, .99f, 36, Color.white);
            hud.phaseText = Label("", parent, .74f, .93f, .98f, .99f, 24, Mint);
            hud.cashText = Label("", parent, .745f, .86f, .96f, .90f, 18, new Color(1, .83f, .37f));
            hud.objectiveText = Label("", parent, .745f, .55f, .95f, .79f, 18, Color.white);
            hud.selectedText = Label("", parent, .745f, .32f, .95f, .54f, 17, new Color(.72f, .84f, .88f));
            hud.messageText = Label("", parent, .045f, .018f, .96f, .077f, 18, Color.white);
            hud.startButton = Button("START", parent, .745f, .80f, .945f, .85f, Mint, gameObjectName: "Start Shift");
            hud.duckButton = Button("DUCK", parent, .745f, .25f, .84f, .30f, new Color(1, .82f, .23f));
            hud.sealButton = Button("SEAL", parent, .85f, .25f, .945f, .30f, Mint);
            hud.trampolineButton = Button("", parent, .745f, .185f, .945f, .235f, new Color(.52f, .81f, .93f), gameObjectName: "Use Trampoline");
            hud.toolText = hud.trampolineButton.GetComponentInChildren<Text>();
            hud.pauseButton = Button("PAUSE", parent, .745f, .12f, .84f, .17f, new Color(.56f, .65f, .73f));
            hud.restartButton = Button("RESTART", parent, .85f, .12f, .945f, .17f, new Color(.56f, .65f, .73f));
            return hud;
        }

        static RectTransform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1); rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text Label(string text, Transform parent, float x0, float y0, float x1, float y1, int size, Color color)
        {
            var label = Rect(string.IsNullOrEmpty(text) ? "Readout" : text, parent, x0, y0, x1, y1).gameObject.AddComponent<Text>();
            label.font = font; label.fontSize = size; label.text = text; label.color = color;
            label.alignment = TextAnchor.UpperLeft; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        static Button Button(string text, Transform parent, float x0, float y0, float x1, float y1, Color color, string gameObjectName = null)
        {
            var rect = Rect(gameObjectName ?? text, parent, x0, y0, x1, y1);
            var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = Label(text, rect, .03f, .08f, .97f, .92f, 16, Ink); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
