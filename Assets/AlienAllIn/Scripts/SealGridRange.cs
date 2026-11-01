using UnityEngine;
namespace AlienAllIn
{
    public static class SealGridRange
    {
        // Reference masks: normal 3/3/3; ultimate 3/5/5/5/3, including the center cell.
        public static bool Contains(Vector2Int offset, bool enhanced = false)
        {
            int x = Mathf.Abs(offset.x), y = Mathf.Abs(offset.y);
            return enhanced ? x <= 2 && y <= 2 && !(x == 2 && y == 2) : x <= 1 && y <= 1;
        }
    }
}
