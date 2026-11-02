using UnityEngine;

namespace AlienAllIn
{
    /// <summary>Uses the editable LineRenderers saved under Main's Attack VFX hierarchy.</summary>
    public sealed class AttackVfx : MonoBehaviour
    {
        [SerializeField] LineRenderer catLaser;
        [SerializeField] BoardGrid board;
        [SerializeField] GridActor cat;
        [SerializeField] LineRenderer[] catUltimateLasers;
        static readonly Vector2[] Directions = { Vector2.right, new Vector2(1,1), Vector2.up, new Vector2(-1,1), Vector2.left, new Vector2(-1,-1), Vector2.down, new Vector2(1,-1) };
        float laserTime, ultimateShotTime;
        public const float CatShotSeconds = .14f;
        public LineRenderer CatRenderer => catLaser;
        public LineRenderer[] CatUltimateRenderers => catUltimateLasers;

        void Awake() => Clear();
        public void Clear()
        {
            laserTime = ultimateShotTime = 0;
            if (boardFeedback) boardFeedback.ClearRange();
            if (catLaser) catLaser.enabled = false;
            if (catUltimateLasers != null) foreach (var beam in catUltimateLasers) if (beam) beam.enabled=false;
        }

        [SerializeField] BoardInteractionFeedback boardFeedback;
        public void SealGrid(Vector3 center, bool enhanced, float duration)
        {
            if (boardFeedback) boardFeedback.ShowRange(center, enhanced, duration);
        }

        public void CatLaser(Vector3 from, Vector3 to)
        {
            if (!catLaser) return;
            laserTime = CatShotSeconds; catLaser.enabled = true; catLaser.positionCount = 2;
            catLaser.SetPosition(0, from); catLaser.SetPosition(1, to);
        }

        void Update()
        {
            laserTime -= Time.deltaTime;
            ultimateShotTime = Mathf.Max(0, ultimateShotTime - Time.deltaTime);
            if (catLaser && laserTime <= 0) catLaser.enabled = false;
        }

        public void CatUltimateShot()
        {
            ultimateShotTime = CatShotSeconds;
            ShowCatUltimate();
        }
        public void ShowCatUltimate()
        {
            bool active=ultimateShotTime > 0 && cat && cat.Alive && cat.IsUltimateActive && board;
            if(active && catLaser) catLaser.enabled=false;
            if(catUltimateLasers==null) return;
            for(int i=0;i<catUltimateLasers.Length;i++)
            {
                var beam=catUltimateLasers[i]; if(!beam) continue;
                beam.enabled=active;
                if(!active) continue;
                beam.positionCount=2;
                beam.SetPosition(0,cat.transform.position);
                beam.SetPosition(1,CatAttackRange.BoardEdge(board,cat.transform.position,Directions[i%8]));
            }
        }
        void LateUpdate() => ShowCatUltimate();
    }
}
