using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlienAllIn.Editor
{
    /// <summary>Imports the six core sprites and wires visual prefabs into the existing session rules.</summary>
    public static class ArcadeCoreAuthoring
    {
        const string Art = "Assets/AlienAllIn/Art/Sprites/Arcade/";
        const string Activities = "Assets/AlienAllIn/Prefabs/Activities/";
        const string Enemies = "Assets/AlienAllIn/Prefabs/Enemies/";
        static Material material;
        static Sprite square;

        [MenuItem("Alien All In/Apply arcade core artwork")]
        public static void Apply()
        {
            Directory.CreateDirectory(Enemies);
            AssetDatabase.Refresh();
            material = AssetDatabase.LoadAssetAtPath<Material>("Assets/AlienAllIn/Art/UnlitSprites.mat");
            square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/AlienAllIn/Art/Square.png");
            var casino = CreateStation("ArcadeCasino", "CASINO", ActivityKind.Casino, ActorKind.Seal, 36, Import("StationCasino", .88f));
            var darts = CreateStation("Darts", "DARTS", ActivityKind.Darts, ActorKind.Pufferfish, 32, Import("StationDarts", .88f));
            var rope = CreateStation("JumpRope", "JUMP ROPE", ActivityKind.JumpRope, ActorKind.Seal, 40, Import("StationJumpRope", .88f));
            var roaster = CreateEnemy("Roaster", Import("EnemyRoaster", .88f));
            var toaster = CreateEnemy("Toaster", Import("EnemyToaster", .88f));
            var pan = CreateEnemy("PanShield", Import("EnemyPan", .88f));

            Scene scene = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            MainRunGameplaySystems systems = null; StageGridProgression stage = null; MainSceneLayout layout = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "Stage 1") root.name = "Arcade Session";
                if (!systems) systems = root.GetComponentInChildren<MainRunGameplaySystems>(true);
                if (!stage) stage = root.GetComponentInChildren<StageGridProgression>(true);
                if (!layout) layout = root.GetComponentInChildren<MainSceneLayout>(true);
            }
            if (!systems || !stage || !layout) throw new System.InvalidOperationException("Main is missing its session configuration.");
            var data = new SerializedObject(systems);
            var activities = data.FindProperty("activitySettings");
            for (int i = 0; i < activities.arraySize; i++)
            {
                var setting = activities.GetArrayElementAtIndex(i);
                ActivityKind kind = (ActivityKind)setting.FindPropertyRelative("kind").enumValueIndex;
                setting.FindPropertyRelative("prefab").objectReferenceValue = kind == ActivityKind.Casino ? casino : kind == ActivityKind.Darts ? darts : rope;
            }
            var enemies = data.FindProperty("enemySettings");
            for (int i = 0; i < enemies.arraySize; i++)
            {
                var setting = enemies.GetArrayElementAtIndex(i);
                EnemyArchetype kind = (EnemyArchetype)setting.FindPropertyRelative("archetype").enumValueIndex;
                setting.FindPropertyRelative("prefab").objectReferenceValue = kind == EnemyArchetype.RoasterMelee ? roaster : kind == EnemyArchetype.ToasterRanged ? toaster : pan;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            // Existing five-minute duration, milestones, spawn odds and combat values are preserved.
            layout.FrameMaximumBoard = false;
            stage.ApplyRunProgress(0);
            EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Arcade core saved: 5x5 opening, three station sprites, three enemy sprites, unchanged session timing and station rolls.");
        }

        static Sprite Import(string name, float visibleSize)
        {
            string path = Art + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048; importer.isReadable = true;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Color32[] pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a < 32) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
            importer.spritePixelsPerUnit = Mathf.Max(maxX - minX + 1, maxY - minY + 1) / visibleSize;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2((minX + maxX + 1f) / (2 * texture.width), (minY + maxY + 1f) / (2 * texture.height));
            importer.SetTextureSettings(settings);
            importer.isReadable = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static ActivityStation CreateStation(string file, string label, ActivityKind kind, ActorKind owner, float duration, Sprite sprite)
        {
            var root = new GameObject(file);
            var station = root.AddComponent<ActivityStation>();
            station.activityName = label; station.activityKind = kind; station.requiredKind = owner;
            station.duration = duration;
            station.artwork = Renderer("Artwork", root.transform, sprite, new Vector3(0, .12f), Vector3.one, Color.white, 2);
            Renderer("Progress Track", root.transform, square, new Vector3(0, -.46f), new Vector3(.82f, .055f, 1), new Color(.25f, .25f, .25f), 4);
            station.marker = Renderer("Progress", root.transform, square, new Vector3(0, -.46f), new Vector3(.82f, .055f, 1), Color.white, 5);
            station.progressFill = station.marker.transform;
            station.ResetActivity();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Activities + file + ".prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<ActivityStation>();
        }

        static GridActor CreateEnemy(string name, Sprite sprite)
        {
            var root = new GameObject(name);
            var body = root.AddComponent<SpriteRenderer>(); body.sprite = sprite; body.sharedMaterial = material; body.sortingOrder = 12;
            var actor = root.AddComponent<GridActor>(); actor.kind = ActorKind.Enemy; actor.countsAsActiveRunMember = false;
            actor.art = root.transform; actor.maxHealth = 10;
            var visual = root.AddComponent<EnemyPresentation>();
            Renderer("Health Track", root.transform, square, new Vector3(0, -.52f), new Vector3(.68f, .045f, 1), new Color(.2f, .2f, .2f), 20);
            visual.healthFill = Renderer("Health", root.transform, square, new Vector3(0, -.52f), new Vector3(.68f, .045f, 1), new Color(.95f, .36f, .3f), 21).transform;
            actor.ResetActor();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Enemies + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab.GetComponent<GridActor>();
        }

        static SpriteRenderer Renderer(string name, Transform parent, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
        {
            var root = new GameObject(name); root.transform.SetParent(parent, false);
            root.transform.localPosition = position; root.transform.localScale = scale;
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sharedMaterial = material; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }
    }
}
