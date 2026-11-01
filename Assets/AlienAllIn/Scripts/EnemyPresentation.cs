using UnityEngine;

namespace AlienAllIn
{
    /// <summary>A small health meter for the core's static enemy sprite.</summary>
    [RequireComponent(typeof(GridActor))]
    public sealed class EnemyPresentation : MonoBehaviour
    {
        public Transform healthFill;
        [Min(.01f)] public float barWidth = .68f;
        GridActor actor;

        void Awake() => actor = GetComponent<GridActor>();
        void LateUpdate()
        {
            if (!actor || !healthFill) return;
            float fill = Mathf.Clamp01(actor.Health / Mathf.Max(1, actor.EffectiveMaxHealth));
            healthFill.localScale = new Vector3(barWidth * fill, .045f, 1);
            healthFill.localPosition = new Vector3(-barWidth * (1 - fill) * .5f, -.52f, 0);
        }
    }
}
