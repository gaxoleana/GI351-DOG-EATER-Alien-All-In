using UnityEngine;
namespace AlienAllIn
{
    public static class SealGridRange
    {
        // Reference masks: 1/3/5/3/1 and 3/5/5/5/3, including the center cell.
        public static bool Contains(Vector2Int offset, bool enhanced = false)
        {
            int x = Mathf.Abs(offset.x), y = Mathf.Abs(offset.y);
            return enhanced ? x <= 2 && y <= 2 && !(x == 2 && y == 2) : x + y <= 2;
        }
    }
}
