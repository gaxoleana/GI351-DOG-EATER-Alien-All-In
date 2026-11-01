using UnityEngine;

namespace AlienAllIn
{
    public enum FieldItemKind { AirPump, LaserGun, Trampoline }

    public sealed class FieldPickup : MonoBehaviour
    {
        [Min(1)] public int trampolineCount = 1;
        public FieldItemKind itemKind = FieldItemKind.Trampoline;
        [Min(1)] public int quantity = 1;
        [SerializeField] bool collected;
        public bool Collected => collected;
        public FieldItemKind ItemKind => itemKind;
        public int Quantity => Mathf.Max(1, quantity);
        public void ResetPickup() { collected = false; gameObject.SetActive(true); }
        public int Collect()
        {
            if (collected) return 0;
            collected = true;
            gameObject.SetActive(false);
            return trampolineCount;
        }

        public bool TryCollect(out FieldItemKind collectedKind, out int collectedQuantity)
        {
            collectedKind = itemKind;
            collectedQuantity = Quantity;
            if (collected) return false;
            collected = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
