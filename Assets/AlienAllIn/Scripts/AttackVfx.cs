using UnityEngine;

namespace AlienAllIn
{
    /// <summary>Uses the editable LineRenderers saved under Main's Attack VFX hierarchy.</summary>
    public sealed class AttackVfx : MonoBehaviour
    {
        [SerializeField] LineRenderer catLaser;
        float laserTime;
        public LineRenderer CatRenderer => catLaser;

        void Awake() => Clear();
        public void Clear()
        {
            laserTime = 0;
            if (boardFeedback) boardFeedback.ClearRange();
            if (catLaser) catLaser.enabled = false;
        }

        [SerializeField] BoardInteractionFeedback boardFeedback;
        public void SealGrid(Vector3 center, bool enhanced, float duration)
        {
            if (boardFeedback) boardFeedback.ShowRange(center, enhanced, duration);
        }

        public void CatLaser(Vector3 from, Vector3 to)
        {
            if (!catLaser) return;
            laserTime = .14f; catLaser.enabled = true; catLaser.positionCount = 2;
            catLaser.SetPosition(0, from); catLaser.SetPosition(1, to);
        }

        void Update()
        {
            laserTime -= Time.deltaTime;
            if (catLaser && laserTime <= 0) catLaser.enabled = false;
        }
    }
}
