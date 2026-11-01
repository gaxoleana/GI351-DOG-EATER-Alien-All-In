using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AlienAllIn.Editor
{
    /// <summary>Applies presentation changes to the existing Main scene without rebuilding its team or gameplay.</summary>
    public static class MainScenePresentation
    {
        [MenuItem("Alien All In/Apply Main gray layout")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyToOpenMain();
        }

        public static void ApplyToOpenMain()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
            BoardGrid board = null; Camera camera = null; MainCoreGameplayUI ui = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (!board) board = root.GetComponentInChildren<BoardGrid>(true);
                if (!camera) camera = root.GetComponentInChildren<Camera>(true);
                if (!ui) ui = root.GetComponentInChildren<MainCoreGameplayUI>(true);
            }
            if (!board || !camera || !ui) throw new System.InvalidOperationException("Main scene is missing its board, camera or HUD.");
            var layout = camera.GetComponent<MainSceneLayout>();
            if (!layout) layout = camera.gameObject.AddComponent<MainSceneLayout>();
            layout.Configure(board, camera);
            var uiData = new SerializedObject(ui);
            uiData.FindProperty("screenLayout").objectReferenceValue = layout;
            uiData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var cell in board.GetComponentsInChildren<BoardCell>(true))
            {
                float shade = (cell.coordinate.x + cell.coordinate.y) % 2 == 0 ? .98f : .86f;
                cell.baseColor = new Color(shade, shade, shade, 1);
                var data = new SerializedObject(cell);
                data.FindProperty("lockedColor").colorValue = new Color(.72f, .72f, .72f, 1);
                data.ApplyModifiedPropertiesWithoutUndo();
                cell.SetUnlocked(cell.IsUnlocked);
                EditorUtility.SetDirty(cell);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Main presentation saved: neutral board, docked HUD, and maximum-board camera framing.");
        }
    }
}
