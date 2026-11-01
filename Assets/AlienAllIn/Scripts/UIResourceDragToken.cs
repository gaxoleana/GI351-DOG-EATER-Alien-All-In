using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AlienAllIn
{
    public enum UIDragPayload { None, Actor, Item }

    /// <summary>Small uGUI token that can be dropped on an Alien card or dragged onto the board.</summary>
    public sealed class UIResourceDragToken : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] UIDragPayload payload;
        [SerializeField] GridActor actor;
        [SerializeField] FieldItemKind item;
        RectTransform rect;
        Canvas rootCanvas;
        CanvasGroup canvasGroup;
        Vector2 initialPosition;

        public UIDragPayload Payload => payload;
        public GridActor Actor => actor;
        public FieldItemKind Item => item;

        void Awake()
        {
            rect = transform as RectTransform;
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void SetActor(GridActor value) { payload = UIDragPayload.Actor; actor = value; }
        public void SetItem(FieldItemKind value) { payload = UIDragPayload.Item; item = value; }

        public void SetLabel(string value, bool available)
        {
            Text text = GetComponentInChildren<Text>(true);
            if (text) text.text = value;
            Image image = GetComponent<Image>();
            if (image) image.color = available ? new Color(.78f, .78f, .78f, 1f) : new Color(.90f, .90f, .90f, 1f);
            if (canvasGroup) canvasGroup.interactable = available || payload == UIDragPayload.Actor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (payload == UIDragPayload.None || (payload == UIDragPayload.Actor && (!actor || !actor.Alive))) return;
            if (!rect) rect = transform as RectTransform;
            if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            initialPosition = rect.anchoredPosition;
            canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (payload == UIDragPayload.Actor || !rect || !rootCanvas) return;
            RectTransform canvasRect = rootCanvas.transform as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                rect.position = rootCanvas.transform.TransformPoint(local);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup) canvasGroup.blocksRaycasts = true;
            if (rect && payload != UIDragPayload.Actor) rect.anchoredPosition = initialPosition;
        }
    }
}
