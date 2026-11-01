using UnityEngine;
using UnityEngine.EventSystems;

namespace AlienAllIn
{
    /// <summary>Receives a dragged fighter card over the world view for Change Position.</summary>
    public sealed class BoardDropSurface : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        MainCoreGameplayUI ui;
        GridActor dragged;
        public void Configure(MainCoreGameplayUI owner) => ui = owner;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (ui && !eventData.dragging) ui.OpenCharacter(ui.ActorAtScreenPoint(eventData.position));
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (ui) dragged = ui.ActorAtScreenPoint(eventData.pressPosition);
            ui?.Feedback?.BeginDrag(dragged, eventData.position);
            eventData.eligibleForClick = false;
        }
        public void OnDrag(PointerEventData eventData) { ui?.Feedback?.DragTo(eventData.position); }
        public void OnEndDrag(PointerEventData eventData)
        {
            if (ui && dragged) ui.NotifyDrop(ui.TryDropActor(dragged, eventData.position));
            dragged = null; ui?.Feedback?.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            UIResourceDragToken token = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<UIResourceDragToken>() : null;
            if (!ui || !token || token.Payload != UIDragPayload.Actor) return;
            ui.NotifyDrop(ui.TryDropActor(token.Actor, eventData.position));
        }
    }
}
