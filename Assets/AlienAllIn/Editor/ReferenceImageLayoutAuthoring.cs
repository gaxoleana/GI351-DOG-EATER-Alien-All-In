using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AlienAllIn.Editor
{
    /// <summary>Saved HUD layout measured inside the reference's 992 x 558 game viewport.</summary>
    public static class ReferenceImageLayoutAuthoring
    {
        const float Width = 992, Height = 558;
        static readonly Color Frame = new Color(92f/255f,92f/255f,92f/255f);
        static readonly Color Paper = new Color(217f/255f,217f/255f,217f/255f);

        [MenuItem("Alien All In/Apply reference gameplay layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before authoring the HUD.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            MainCoreGameplayUI ui = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                ui = root.GetComponentInChildren<MainCoreGameplayUI>(true);
                if (ui) break;
            }
            if (!ui) throw new System.InvalidOperationException("Main HUD is missing.");
            ApplyTo(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyTo(MainCoreGameplayUI ui)
        {
            Undo.RegisterFullObjectHierarchyUndo(ui.gameObject, "Apply reference gameplay layout");
            var data = new SerializedObject(ui);
            var canvas = ui.HudCanvas;
            canvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(Width, Height);
            var layout = (MainSceneLayout)data.FindProperty("screenLayout").objectReferenceValue;
            var layoutData = new SerializedObject(layout);
            layoutData.FindProperty("headerRegion").rectValue = Pixels(0,0,992,52);
            layoutData.FindProperty("reservedLeftRegion").rectValue = Pixels(0,150,78,259);
            layoutData.FindProperty("statusRegion").rectValue = Pixels(909,168,78,259);
            layoutData.FindProperty("itemsRegion").rectValue = Pixels(909,168,78,259);
            layoutData.FindProperty("boardRegion").rectValue = Pixels(134,86,724,389);
            layoutData.FindProperty("teamRegion").rectValue = Pixels(86,480,820,78);
            layoutData.FindProperty("backgroundColor").colorValue = Paper;
            layoutData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var property in new[] { "headerRect", "leftRect", "rightRect", "itemBarRect", "boardDropRect" })
            {
                var rect = (RectTransform)data.FindProperty(property).objectReferenceValue;
                Set(rect, property == "headerRect" ? layout.Header : property == "leftRect" ? layout.LeftColumn :
                    property == "rightRect" ? layout.RightColumn : property == "itemBarRect" ? layout.ItemBar : layout.BoardViewport);
            }
            var margins = data.FindProperty("backgroundRects");
            for (int i=0;i<margins.arraySize;i++) ((RectTransform)margins.GetArrayElementAtIndex(i).objectReferenceValue).GetComponent<Image>().color = Frame;

            var header = (RectTransform)data.FindProperty("headerRect").objectReferenceValue;
            var info = header.Find("Score Grid Timer Panel");
            Set((RectTransform)info, HeaderPixels(367,0,259,52));
            info.GetComponent<Image>().color = Paper;
            var score = (Text)data.FindProperty("scoreText").objectReferenceValue;
            var timer = (Text)data.FindProperty("runText").objectReferenceValue;
            var grid = header.Find("Grid Scale") ? header.Find("Grid Scale").GetComponent<Text>() : Label("Grid Scale",header);
            Set(score.rectTransform, HeaderPixels(367,0,83,52)); Style(score,16); score.text = "Score\n0";
            Set(grid.rectTransform, HeaderPixels(450,0,99,52)); Style(grid,16); grid.text = "Grid Scale\n5x5";
            Set(timer.rectTransform, HeaderPixels(549,0,77,52)); Style(timer,16); timer.text = "Timer\n05:00";
            data.FindProperty("gridText").objectReferenceValue = grid;
            SetButton((Button)data.FindProperty("startButton").objectReferenceValue, HeaderPixels(734,2,52,26), "Start");
            SetButton((Button)data.FindProperty("pauseButton").objectReferenceValue, HeaderPixels(806,2,26,26), "II");
            SetButton((Button)data.FindProperty("restartButton").objectReferenceValue, HeaderPixels(832,2,26,26), "O");
            ((Text)data.FindProperty("statusText").objectReferenceValue).gameObject.SetActive(false);

            var left = (RectTransform)data.FindProperty("leftRect").objectReferenceValue;
            var process = canvas.transform.Find("Right Sidebar/Process Status") ?? left.Find("Process Status");
            process.SetParent(left,false); Set((RectTransform)process,new Rect(0,0,1,1)); process.GetComponent<Image>().color = Paper;
            process.Find("Process Header").gameObject.SetActive(false);
            Set((RectTransform)process.Find("Process List"),new Rect(.03f,.02f,.94f,.96f));
            var stationText = (Text)data.FindProperty("processText").objectReferenceValue;
            var enemyText = (Text)data.FindProperty("enemyStatusText").objectReferenceValue;
            Set(stationText.rectTransform,new Rect(0,.51f,1,.49f)); Style(stationText,18,TextAnchor.UpperLeft); stationText.text="Stations\n0";
            Set(enemyText.rectTransform,new Rect(0,0,1,.49f)); Style(enemyText,18,TextAnchor.UpperLeft); enemyText.text="Enemy\n0";
            var items = (RectTransform)data.FindProperty("itemBarRect").objectReferenceValue;
            items.GetComponent<Image>().color = Paper;
            items.Find("Items Header").gameObject.SetActive(false);
            var itemButtons = data.FindProperty("itemButtons");
            for(int i=0;i<itemButtons.arraySize;i++)
            {
                var button=(Button)itemButtons.GetArrayElementAtIndex(i).objectReferenceValue;
                Set((RectTransform)button.transform,new Rect(8f/77f,(259-11-63-i*87)/259f,62f/77f,63f/259f));
                button.GetComponent<Image>().color=new Color(.63f,.63f,.63f);
                Style(button.GetComponentInChildren<Text>(),12);
            }

            var team = canvas.transform.Find("Team Status Bar") ?? left.Find("Team Cards");
            team.SetParent(canvas.transform,false); team.name="Team Status Bar"; team.gameObject.SetActive(true);
            Set((RectTransform)team,layout.TeamBar); team.GetComponent<Image>().color=Paper;
            foreach(var group in team.GetComponents<LayoutGroup>()) group.enabled=false;
            data.FindProperty("teamBarRect").objectReferenceValue=team;
            var cards=data.FindProperty("cardButtons"); var actors=data.FindProperty("cardActors");
            var portraits=data.FindProperty("cardPortraits"); portraits.arraySize=cards.arraySize;
            for(int i=0;i<cards.arraySize;i++)
            {
                var button=(Button)cards.GetArrayElementAtIndex(i).objectReferenceValue;
                Set((RectTransform)button.transform,new Rect((6+i*204)/820f,6f/78f,194f/820f,66f/78f));
                button.GetComponent<Image>().color=new Color(.63f,.63f,.63f);
                var text=(Text)data.FindProperty("cardTexts").GetArrayElementAtIndex(i).objectReferenceValue;
                Set(text.rectTransform,new Rect(58f/194f,0,130f/194f,1)); Style(text,17,TextAnchor.MiddleLeft);
                text.resizeTextMinSize=15;
                var actor=(GridActor)actors.GetArrayElementAtIndex(i).objectReferenceValue;
                text.text=actor.kind==ActorKind.Duck ? "Duck\nIdle" : $"{actor.kind}\nHP {actor.maxHealth:0} ATK {actor.damage:0.0}\nIdle";
                var portrait=button.transform.Find("Portrait");
                if(!portrait) portrait=Panel("Portrait",button.transform,Color.white).transform;
                Set((RectTransform)portrait,new Rect(8f/194f,12f/66f,42f/194f,42f/66f));
                var image=portrait.GetComponent<Image>(); image.preserveAspect=true; image.raycastTarget=false;
                var sprite=actor.GetComponentInChildren<SpriteRenderer>(true); image.sprite=sprite?sprite.sprite:null;
                portraits.GetArrayElementAtIndex(i).objectReferenceValue=image;
                button.transform.Find("Ultimate Track").gameObject.SetActive(false);
            }

            var frame=canvas.transform.Find("Board Reference Frame");
            if(!frame) frame=Panel("Board Reference Frame",canvas.transform,Color.clear).transform;
            Set((RectTransform)frame,layout.BoardViewport); data.FindProperty("boardFrameRect").objectReferenceValue=frame;
            foreach(string side in new[]{"Top","Bottom","Left","Right"})
            {
                var edge=frame.Find(side); if(!edge) edge=Panel(side,frame,new Color(.15f,.65f,1)).transform;
                var rect=(RectTransform)edge;
                rect.anchorMin = side=="Top" ? Vector2.up : side=="Right" ? Vector2.right : Vector2.zero;
                rect.anchorMax = side=="Left" ? Vector2.up : side=="Bottom" ? Vector2.right : Vector2.one;
                rect.pivot = side=="Top" ? new Vector2(.5f,1) : side=="Bottom" ? new Vector2(.5f,0) : side=="Left" ? new Vector2(0,.5f) : new Vector2(1,.5f);
                rect.anchoredPosition=Vector2.zero; rect.sizeDelta= side=="Top"||side=="Bottom" ? new Vector2(0,2) : new Vector2(2,0);
            }

            var wallet=(Text)data.FindProperty("walletText").objectReferenceValue;
            var currencyPanel=canvas.transform.Find("Currency Panel");
            if(!currencyPanel) currencyPanel=Panel("Currency Panel",canvas.transform,new Color(.63f,.63f,.63f)).transform;
            Set((RectTransform)currencyPanel,Pixels(415,52,161,30));
            wallet.transform.SetParent(currencyPanel,false); Set(wallet.rectTransform,new Rect(.03f,0,.94f,1)); Style(wallet,12);
            wallet.gameObject.SetActive(true); wallet.text="Coins -> 0\nChips -> 0";
            Set((RectTransform)((Button)data.FindProperty("optionsResume").objectReferenceValue).transform,new Rect(.08f,.49f,.84f,.15f));
            Set((RectTransform)((Button)data.FindProperty("optionsRestart").objectReferenceValue).transform,new Rect(.08f,.29f,.84f,.15f));
            var shop=(Button)data.FindProperty("shopButton").objectReferenceValue;
            shop.transform.SetParent(canvas.transform,false); SetButton(shop,Pixels(917,81,62,62),"Exchange");
            shop.GetComponent<Image>().color=new Color(.63f,.63f,.63f);
            Style(shop.GetComponentInChildren<Text>(),12); shop.gameObject.SetActive(true);
            ((Button)data.FindProperty("exchangeOne").objectReferenceValue).name="Exchange One";
            ((Button)data.FindProperty("exchangeAll").objectReferenceValue).name="Exchange All";
            RepairExchangePanel(data);
            data.ApplyModifiedPropertiesWithoutUndo();
            layout.RefreshCamera(); EditorUtility.SetDirty(ui); EditorUtility.SetDirty(layout);
        }

        static void RepairExchangePanel(SerializedObject data)
        {
            var panel=(GameObject)data.FindProperty("shopPanel").objectReferenceValue;
            Set((RectTransform)panel.transform,Pixels(286,112,420,334));
            panel.GetComponent<Image>().color=new Color(.88f,.88f,.88f);
            panel.GetComponent<Image>().raycastTarget=true;
            var title=panel.transform.Find("Shop Title").GetComponent<Text>();
            Set(title.rectTransform,new Rect(.05f,.84f,.78f,.12f)); Style(title,20); title.text="EXCHANGE";
            var balance=(Text)data.FindProperty("shopBalance").objectReferenceValue;
            Set(balance.rectTransform,new Rect(.05f,.61f,.90f,.23f)); Style(balance,17);
            var close=(Button)data.FindProperty("shopClose").objectReferenceValue;
            SetButton(close,new Rect(.87f,.86f,.09f,.10f),"X");
            var fields=new[]{"exchangeCoinOne","exchangeCoinAll","exchangeOne","exchangeAll"};
            var labels=new[]{"1 Coin -> 1 Chip","All Coins -> Chips","1 Chip -> 1 Coin","All Chips -> Coins"};
            for(int i=0;i<fields.Length;i++)
            {
                var button=(Button)data.FindProperty(fields[i]).objectReferenceValue;
                button.gameObject.SetActive(true); button.enabled=true;
                SetButton(button,new Rect(.08f,.49f-i*.14f,.84f,.11f),labels[i]);
                var image=button.GetComponent<Image>();image.enabled=true;image.color=new Color(.60f,.60f,.60f);image.raycastTarget=true;
                button.targetGraphic=image;
                var label=button.GetComponentInChildren<Text>(true); label.enabled=true;label.gameObject.SetActive(true);
                Set(label.rectTransform,new Rect(.03f,.05f,.94f,.90f)); Style(label,16);
            }
        }

        static Rect Pixels(float x,float top,float w,float h) => new Rect(x/Width,(Height-top-h)/Height,w/Width,h/Height);
        static Rect HeaderPixels(float x,float top,float w,float h) => new Rect(x/Width,(52-top-h)/52,w/Width,h/52);
        static void Set(RectTransform rect,Rect bounds) { rect.anchorMin=bounds.min;rect.anchorMax=bounds.max;rect.offsetMin=rect.offsetMax=Vector2.zero; }
        static GameObject Panel(string name,Transform parent,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return go;
        }
        static Text Label(string name,Transform parent) { var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);return go.GetComponent<Text>(); }
        static void Style(Text text,int size,TextAnchor alignment=TextAnchor.MiddleCenter)
        {
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=new Color(.18f,.18f,.18f);
            text.alignment=alignment;text.resizeTextForBestFit=true;text.resizeTextMinSize=8;text.resizeTextMaxSize=size;text.raycastTarget=false;
        }
        static void SetButton(Button button,Rect bounds,string label)
        {
            Set((RectTransform)button.transform,bounds);button.GetComponent<Image>().color=Paper;
            var text=button.GetComponentInChildren<Text>();text.text=label;Style(text,18);
        }
    }
}
