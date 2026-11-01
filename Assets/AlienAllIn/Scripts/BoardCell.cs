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
        [SerializeField] Color lockedColor = new Color(.055f, .09f, .12f, 1f);
        [SerializeField] bool unlocked = true;
        [SerializeField, Tooltip("Actors whose centers are currently inside this cell. Updated by BoardGrid, without physics triggers.")]
        List<GridActor> occupants = new List<GridActor>();
        public IReadOnlyList<GridActor> Occupants => occupants;
        public bool IsUnlocked => unlocked;
        public string Address => $"{(char)('A' + coordinate.x)}{coordinate.y + 1}";

        internal void ClearOccupants() => occupants.Clear();
        internal void AddOccupant(GridActor actor) => occupants.Add(actor);

        public void SetUnlocked(bool value)
        {
            unlocked = value;
            ResolveSurface();
            if (surface) surface.color = unlocked ? baseColor : lockedColor;
        }

        public void Highlight(Color? color)
        {
            ResolveSurface();
            if (surface) surface.color = color ?? (unlocked ? baseColor : lockedColor);
        }

        void ResolveSurface()
        {
            if (surface) return;
            surface = GetComponent<SpriteRenderer>();
            if (!surface) surface = GetComponentInChildren<SpriteRenderer>(true);
        }
    }
}
