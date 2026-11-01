using UnityEngine;
using UnityEngine.EventSystems;

namespace AlienAllIn
{
    /// <summary>Receives a dragged fighter card over the world view for Change Position.</summary>
    public sealed class BoardDropSurface : MonoBehaviour, IDropHandler
    {
        MainCoreGameplayUI ui;
        public void Configure(MainCoreGameplayUI owner) => ui = owner;

        public void OnDrop(PointerEventData eventData)
        {
            UIResourceDragToken token = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<UIResourceDragToken>() : null;
            if (!ui || !token || token.Payload != UIDragPayload.Actor) return;
            ui.NotifyDrop(ui.TryDropActor(token.Actor, eventData.position));
        }
    }
}
