using UnityEngine;

namespace AlienAllIn
{
    /// <summary>Shared animation entry points for every team species.</summary>
    [DisallowMultipleComponent]
    public sealed class AlienAnimationDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] GridActor actor;
        [SerializeField] AnimationClip idleClip;
        [SerializeField] AnimationClip attackClip;
        [SerializeField] AnimationClip ultimateClip;
        [SerializeField, Min(1f), Tooltip("Prototype playback rate. Clips are authored at 8 FPS; this scales their playback speed.")]
        float prototypeFrameRate = 8f;

        const float ClipSampleRate = 8f;

        public AnimationClip IdleClip => idleClip;
        public AnimationClip AttackClip => attackClip;
        public AnimationClip UltimateClip => ultimateClip;

        void Awake()
        {
            ResolveReferences();
            ApplyPrototypeFrameRate();
        }

        void OnValidate()
        {
            prototypeFrameRate = Mathf.Max(1f, prototypeFrameRate);
            ResolveReferences();
            ApplyPrototypeFrameRate();
        }

        void ResolveReferences()
        {
            if (!animator) animator = GetComponent<Animator>();
            if (!actor) actor = GetComponent<GridActor>();
        }

        void ApplyPrototypeFrameRate()
        {
            if (animator) animator.speed = prototypeFrameRate / ClipSampleRate;
        }

        public bool PlayAttack()
        {
            ResolveReferences();
            if (!animator || (actor && actor.kind == ActorKind.Duck)) return false;
            animator.ResetTrigger("Ultimate");
            animator.SetTrigger("Attack");
            return true;
        }

        public bool PlayUltimate()
        {
            ResolveReferences();
            if (!animator) return false;
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Ultimate");
            return true;
        }

        public void ResetToIdle()
        {
            ResolveReferences();
            if (!animator) return;
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Ultimate");
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }
    }
}
