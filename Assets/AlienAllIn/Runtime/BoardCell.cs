using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    [DisallowMultipleComponent]
    public sealed class BoardCell : MonoBehaviour
    {
        [Tooltip("Zero-based grid coordinate. A1 is (0, 0).")]
        public Vector2Int coordinate;
        public SpriteRenderer surface;
        public Color baseColor;
        [SerializeField, Tooltip("Actors whose centers are currently inside this cell. Updated by BoardGrid, without physics triggers.")]
        List<GridActor> occupants = new List<GridActor>();
        public IReadOnlyList<GridActor> Occupants => occupants;
        public string Address => $"{(char)('A' + coordinate.x)}{coordinate.y + 1}";

        internal void ClearOccupants() => occupants.Clear();
        internal void AddOccupant(GridActor actor) => occupants.Add(actor);
        public void Highlight(Color? color) { if (surface) surface.color = color ?? baseColor; }
    }
}
