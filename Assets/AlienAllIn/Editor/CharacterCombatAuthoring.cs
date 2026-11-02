using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace AlienAllIn.Editor
{
    /// <summary>Adds only the new status bars and Cat beams, preserving the current authored HUD.</summary>
    public static class CharacterCombatAuthoring
    {
        [MenuItem("Alien All In/Apply character gauges and Cat Ultimate")]
        public static void Apply()
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            MainCoreGameplayUI ui=null; MainRunGameplaySystems game=null; AttackVfx vfx=null;
            foreach(var root in scene.GetRootGameObjects())
            {
                if(!ui)ui=root.GetComponentInChildren<MainCoreGameplayUI>(true);
                if(!game)game=root.GetComponentInChildren<MainRunGameplaySystems>(true);
                if(!vfx)vfx=root.GetComponentInChildren<AttackVfx>(true);
            }
            var data=new SerializedObject(ui); var canvas=ui.HudCanvas;
            var board=game.Board;
            if(!board)board=(BoardGrid)new SerializedObject(game).FindProperty("board").objectReferenceValue;
            var camera=(Camera)data.FindProperty("stageCamera").objectReferenceValue;
            var old=canvas.transform.Find("Character Status Bars"); if(old)Object.DestroyImmediate(old.gameObject);
            var container=new GameObject("Character Status Bars",typeof(RectTransform)); container.transform.SetParent(canvas.transform,false);
            Set((RectTransform)container.transform,new Rect(0,0,1,1));
            // Keep modal panels above the bars and let pointer rays pass through every bar.
            container.transform.SetSiblingIndex(canvas.transform.Find("Board Drop Surface").GetSiblingIndex()+1);
            var roster=data.FindProperty("cardActors");
            for(int i=0;i<roster.arraySize;i++)
            {
                var actor=(GridActor)roster.GetArrayElementAtIndex(i).objectReferenceValue;
                var go=new GameObject(actor.kind+" Status",typeof(RectTransform),typeof(CanvasGroup),typeof(CharacterStatusBar));
                go.transform.SetParent(container.transform,false);var rect=(RectTransform)go.transform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(58,5);
                var view=go.GetComponent<CharacterStatusBar>();view.actor=actor;view.board=board;view.systems=game;view.stageCamera=camera;view.hudCanvas=canvas;
                view.visibility=go.GetComponent<CanvasGroup>();view.visibility.blocksRaycasts=false;view.visibility.interactable=false;
                var hp=Panel("HP Track",go.transform,new Rect(0,.6f,1,.4f),new Color(0,0,0,.12f));
                view.healthRoot=(RectTransform)hp.transform;
                var health=Panel("HP Fill",hp.transform,new Rect(0,0,1,1),view.healthColor);view.healthFill=(RectTransform)health.transform;view.healthImage=health.GetComponent<Image>();
                var ult=Panel("Ultimate Track",go.transform,new Rect(0,0,1,.4f),new Color(0,0,0,.12f));
                var fill=Panel("Ultimate Fill",ult.transform,new Rect(0,0,0,1),view.chargeColor);view.chargeFill=(RectTransform)fill.transform;view.chargeImage=fill.GetComponent<Image>();
                if(actor.kind==ActorKind.Duck)hp.SetActive(false);
                Vector3 screen=camera.WorldToScreenPoint(actor.transform.position-board.transform.TransformVector(Vector3.up*board.cellSize*.61f));
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,screen,null,out var local);rect.anchoredPosition=local;
                EditorUtility.SetDirty(view);
            }
            var vfxData=new SerializedObject(vfx); vfxData.FindProperty("board").objectReferenceValue=board;
            GridActor cat=null; for(int i=0;i<roster.arraySize;i++){var actor=(GridActor)roster.GetArrayElementAtIndex(i).objectReferenceValue;if(actor.kind==ActorKind.Cat)cat=actor;}
            vfxData.FindProperty("cat").objectReferenceValue=cat;
            var catData=new SerializedObject(cat);catData.FindProperty("useGridAttackRange").boolValue=true;catData.ApplyModifiedPropertiesWithoutUndo();
            var oldBeams=vfx.transform.Find("Cat Ultimate - Eight Board Lasers");if(oldBeams)Object.DestroyImmediate(oldBeams.gameObject);
            var beams=new GameObject("Cat Ultimate - Eight Board Lasers");beams.transform.SetParent(vfx.transform,false);
            var array=vfxData.FindProperty("catUltimateLasers");array.arraySize=8;
            for(int i=0;i<8;i++)
            {
                var go=new GameObject($"Beam {i*45} degrees");go.transform.SetParent(beams.transform,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=vfx.CatRenderer.sharedMaterial;
                line.startWidth=line.endWidth=.045f;line.startColor=line.endColor=new Color(1,.22f,.3f,.85f);
                line.sortingOrder=11;line.useWorldSpace=true;line.positionCount=2;line.numCapVertices=3;line.enabled=false;
                var direction=new Vector2(Mathf.Cos(i*Mathf.PI/4),Mathf.Sin(i*Mathf.PI/4));
                line.SetPosition(0,cat.transform.position);line.SetPosition(1,CatAttackRange.BoardEdge(board,cat.transform.position,direction));
                array.GetArrayElementAtIndex(i).objectReferenceValue=line;
            }
            vfxData.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(vfx);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static GameObject Panel(string name,Transform parent,Rect bounds,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Set((RectTransform)go.transform,bounds);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return go;
        }
        [MenuItem("Alien All In/Simplify character bars")]
        public static void SimplifyBars()
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            foreach(var root in scene.GetRootGameObjects()) foreach(var bar in root.GetComponentsInChildren<CharacterStatusBar>(true))
            {
                foreach(var text in bar.GetComponentsInChildren<Text>(true))Object.DestroyImmediate(text.gameObject);
                bar.chargeColor=new Color(1,.78f,.12f);bar.chargeImage.color=bar.chargeColor;
                ((RectTransform)bar.transform).sizeDelta=new Vector2(58,5);
                Set(bar.healthRoot,new Rect(0,.6f,1,.4f));
                Set((RectTransform)bar.chargeFill.parent,new Rect(0,0,1,.4f));
                bar.healthRoot.GetComponent<Image>().color=new Color(0,0,0,.12f);
                bar.chargeFill.parent.GetComponent<Image>().color=new Color(0,0,0,.12f);
                EditorUtility.SetDirty(bar);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void Set(RectTransform rect,Rect bounds){rect.anchorMin=bounds.min;rect.anchorMax=bounds.max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
