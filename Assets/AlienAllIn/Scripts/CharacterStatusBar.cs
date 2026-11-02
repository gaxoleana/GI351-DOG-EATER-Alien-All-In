using UnityEngine;
using UnityEngine.UI;
namespace AlienAllIn
{
    /// <summary>Follows an actor with saved scene UI; never inherits the sprite's hit bounce.</summary>
    public sealed class CharacterStatusBar : MonoBehaviour
    {
        public GridActor actor;
        public BoardGrid board;
        public MainRunGameplaySystems systems;
        public Camera stageCamera;
        public Canvas hudCanvas;
        public CanvasGroup visibility;
        public RectTransform healthRoot, healthFill, chargeFill;
        public Image healthImage, chargeImage;
        [Header("Appearance in HUD reference pixels")]
        [Min(16)] public float maximumWidth=58;
        [Min(12)] public float minimumWidth=24;
        [Range(.4f,1)] public float cellWidthFraction=.78f;
        public float verticalCellOffset=.61f;
        public Color healthColor=new Color(.23f,.8f,.35f);
        public Color chargeColor=new Color(1,.78f,.12f);

        void LateUpdate()
        {
            bool visible=actor && actor.Alive && actor.gameObject.activeInHierarchy;
            visibility.alpha=visible?1:0;
            if(!visible || !stageCamera || !hudCanvas) return;
            bool duck=actor.kind==ActorKind.Duck;
            bool captured=duck && systems.DuckCaptured;
            healthRoot.gameObject.SetActive(!duck || captured);
            float health=captured ? systems.DuckRescueRemaining/Mathf.Max(1,systems.DuckRescueDuration) : actor.Health/Mathf.Max(1,actor.EffectiveMaxHealth);
            Fill(healthFill,health); Fill(chargeFill,actor.UltimateCharge);
            healthImage.color=captured?new Color(1,.23f,.18f):healthColor;
            chargeImage.color=chargeColor;
            float scale=Mathf.Max(.01f,hudCanvas.scaleFactor);
            Vector3 center=stageCamera.WorldToScreenPoint(actor.transform.position);
            Vector3 across=stageCamera.WorldToScreenPoint(actor.transform.position+board.transform.TransformVector(Vector3.right*board.cellSize));
            float cellPixels=Vector2.Distance(center,across)/scale;
            float width=Mathf.Clamp(cellPixels*cellWidthFraction,minimumWidth,maximumWidth);
            const float height=5;
            var rect=(RectTransform)transform; rect.sizeDelta=new Vector2(width,height);
            Vector3 world=actor.transform.position-board.transform.TransformVector(Vector3.up*board.cellSize*verticalCellOffset);
            Vector2 screen=stageCamera.WorldToScreenPoint(world);
            Rect view=stageCamera.pixelRect;
            screen.x=Mathf.Clamp(screen.x,view.xMin+width*scale*.5f,view.xMax-width*scale*.5f);
            screen.y=Mathf.Clamp(screen.y,view.yMin+height*.5f*scale,view.yMax-height*.5f*scale);
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hudCanvas.transform,screen,null,out var local)) rect.anchoredPosition=local;
        }
        static void Fill(RectTransform rect,float amount) { rect.anchorMax=new Vector2(Mathf.Clamp01(amount),1); }
    }
}
