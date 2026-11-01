using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace AlienAllIn.Editor
{
    public static class ReferenceHudAuthoring
    {
        public static void Apply(MainCoreGameplayUI ui, MainRunGameplaySystems game, BoardGrid board, Camera camera)
        {
            var data = new SerializedObject(ui); var canvas = ui.HudCanvas.transform;
            var roster=data.FindProperty("cardActors");
            for(int i=0;i<roster.arraySize;i++) { var actor=(GridActor)roster.GetArrayElementAtIndex(i).objectReferenceValue; actor.useGridAttackRange=actor.kind==ActorKind.Seal; EditorUtility.SetDirty(actor); }
            var layoutData = new SerializedObject(camera.GetComponent<MainSceneLayout>());
            layoutData.FindProperty("backgroundColor").colorValue = new Color(.36f,.36f,.36f); layoutData.ApplyModifiedPropertiesWithoutUndo();
            for (int i=0;i<4;i++) canvas.Find("Window Margin "+i).GetComponent<Image>().color = new Color(.36f,.36f,.36f);
            canvas.Find("Left Sidebar/Team Cards").gameObject.SetActive(false);
            var process = canvas.Find("Left Sidebar/Process Status");
            process.SetParent(canvas.Find("Right Sidebar"), false); Set((RectTransform)process, new Rect(0,0,1,1));
            process.Find("Process Header").gameObject.SetActive(false);
            Set((RectTransform)process.Find("Process List"), new Rect(.04f,.03f,.92f,.94f));
            var stationText=process.Find("Process List/Live Status").GetComponent<Text>();
            Set(stationText.rectTransform,new Rect(0,.51f,1,.49f)); stationText.alignment=TextAnchor.UpperLeft;
            var enemyLabel=new GameObject("Enemy Status",typeof(RectTransform),typeof(Text)); enemyLabel.transform.SetParent(process.Find("Process List"),false);
            Set((RectTransform)enemyLabel.transform,new Rect(0,0,1,.47f));
            var enemyText=enemyLabel.GetComponent<Text>(); enemyText.font=stationText.font; enemyText.fontSize=17; enemyText.color=stationText.color;
            enemyText.alignment=TextAnchor.UpperLeft; enemyText.text="ENEMY\n0"; enemyText.raycastTarget=false;
            data.FindProperty("enemyStatusText").objectReferenceValue=enemyText;
            process.GetComponentInChildren<Text>(true).fontSize = 16;
            var items = canvas.Find("Right Sidebar/Items"); items.SetParent(canvas, false);
            Set((RectTransform)items, new Rect(.38f,.04f,.24f,.09f));
            data.FindProperty("itemBarRect").objectReferenceValue = items;
            items.Find("Items Header").gameObject.SetActive(false);
            var buttons = items.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++) Set((RectTransform)buttons[i].transform, new Rect(.02f + i*.325f,.10f,.31f,.80f));
            var header = canvas.Find("Run HUD");
            header.GetComponent<Image>().color=Color.clear; header.GetComponent<Image>().raycastTarget=false;
            var info = Panel("Score Grid Timer Panel",header,new Rect(.26f,0,.45f,1)); info.transform.SetAsFirstSibling();
            Set((RectTransform)header.Find("Run State"), new Rect(.40f,.40f,.30f,.50f));
            Set((RectTransform)header.Find("Score"), new Rect(.27f,.40f,.12f,.50f));
            Set((RectTransform)header.Find("Wallet"), new Rect(.27f,.04f,.43f,.29f));
            header.Find("Status").gameObject.SetActive(false);
            Set((RectTransform)header.Find("Start"), new Rect(.73f,.45f,.085f,.43f));
            Set((RectTransform)header.Find("Pause"), new Rect(.83f,.45f,.065f,.43f));
            header.Find("Pause").GetComponentInChildren<Text>().text = "II";
            Set((RectTransform)header.Find("Restart"), new Rect(.91f,.45f,.065f,.43f));
            header.Find("Restart").GetComponentInChildren<Text>().text = "O";
            var options = Panel("Options", canvas, new Rect(.38f,.28f,.30f,.42f));
            var close = Button("Close", options.transform, new Rect(.84f,.83f,.12f,.12f), "X");
            var resume = Button("Resume", options.transform, new Rect(.08f,.48f,.84f,.18f), "Resume");
            var restart = Button("Restart", options.transform, new Rect(.08f,.22f,.84f,.18f), "Restart");
            data.FindProperty("optionsPanel").objectReferenceValue = options;
            data.FindProperty("optionsClose").objectReferenceValue = close;
            data.FindProperty("optionsResume").objectReferenceValue = resume;
            data.FindProperty("optionsRestart").objectReferenceValue = restart; options.SetActive(false);
            var old = game.transform.Find("Board Feedback"); if (old) Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Board Feedback"); root.transform.SetParent(game.transform,false);
            var feedback = root.AddComponent<BoardInteractionFeedback>(); feedback.board = board; feedback.stageCamera = camera; feedback.ui = ui; feedback.systems = game;
            feedback.hoverOutline = Line("Character Hover", root.transform); feedback.dragOutline = Line("Drag Destination", root.transform);
            data.FindProperty("feedback").objectReferenceValue = feedback; data.ApplyModifiedPropertiesWithoutUndo();
            var vfx = game.GetComponentInChildren<AttackVfx>(); var vfxData = new SerializedObject(vfx);
            vfxData.FindProperty("boardFeedback").objectReferenceValue = feedback; vfxData.ApplyModifiedPropertiesWithoutUndo();
            camera.GetComponent<MainSceneLayout>().RefreshCamera();
        }
        static LineRenderer Line(string name, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent,false); var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/AlienAllIn/Art/UnlitSprites.mat");
            line.startWidth = line.endWidth = .045f; line.sortingOrder = 20; line.useWorldSpace = true; line.loop = true; line.positionCount = 4; line.enabled = false; return line;
        }
        static GameObject Panel(string name, Transform parent, Rect bounds)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false); Set((RectTransform)go.transform,bounds); go.GetComponent<Image>().color = new Color(.85f,.85f,.85f); return go;
        }
        static Button Button(string name, Transform parent, Rect bounds, string title)
        {
            var go=Panel(name,parent,bounds); var button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>();
            var label = new GameObject("Label",typeof(RectTransform),typeof(Text)); label.transform.SetParent(go.transform,false); Set((RectTransform)label.transform,new Rect(0,0,1,1));
            var text=label.GetComponent<Text>(); text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text=title; text.fontSize=22; text.color=Color.black; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; return button;
        }
        static void Set(RectTransform rect, Rect bounds) { rect.anchorMin=bounds.min; rect.anchorMax=bounds.max; rect.offsetMin=rect.offsetMax=Vector2.zero; }
    }
}
