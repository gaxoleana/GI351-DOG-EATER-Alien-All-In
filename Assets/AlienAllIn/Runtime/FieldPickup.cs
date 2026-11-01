using UnityEngine;

namespace AlienAllIn
{
    public sealed class FieldPickup : MonoBehaviour
    {
        [Min(1)] public int trampolineCount = 1;
        [SerializeField] bool collected;
        public bool Collected => collected;
        public void ResetPickup() { collected = false; gameObject.SetActive(true); }
        public int Collect()
        {
            if (collected) return 0;
            collected = true;
            gameObject.SetActive(false);
            return trampolineCount;
        }
    }
}
