using UnityEngine;
using UnityEngine.EventSystems;

namespace AlienAllIn
{
    /// <summary>Accepts matching item/Chip drops on one roster member.</summary>
    public sealed class AlienDropTarget : MonoBehaviour, IDropHandler
    {
        MainCoreGameplayUI ui;
        GridActor target;
        public void Configure(MainCoreGameplayUI owner, GridActor actor) { ui = owner; target = actor; }

        public void OnDrop(PointerEventData eventData)
        {
            UIResourceDragToken token = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<UIResourceDragToken>() : null;
            if (!ui || !token) return;
            ui.NotifyDrop(ui.TryDropOnActor(token, target));
        }
    }
}
