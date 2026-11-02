using System;
using UnityEngine;

namespace AlienAllIn
{
    // Keep existing serialized enum values stable; new species are appended after Enemy.
    public enum ActorKind { Duck, Seal, Enemy, Cat, Pufferfish }
    public enum ActorState
    {
        Idle, Moving, Collecting, Working, Fleeing, Hiding, Defending, Dead,
        Panic, Inflating, UsingUltimate, Eating, Shielding, Stunned, Kidnapped
    }

    [DisallowMultipleComponent]
    public sealed class GridActor : MonoBehaviour
    {
        public static event Action<GridActor, Vector3, float> Damaged;
        const float HitFlashDurationSeconds = .25f;
        const float HitBounceDurationSeconds = .32f;
        const float HitBounceOscillations = 2.5f;
        const float HitBounceDamping = 4f;
        const float HitBounceAmplitude = .26f;

        public ActorKind kind;
        [Tooltip("Only active run members contribute to the RunLoop defeat condition. Enemies are never run members.")]
        public bool countsAsActiveRunMember = true;
        [Tooltip("Main's Duck is captured instead of taking HP damage. Leave off in the legacy reference scene.")]
        public bool invulnerable;
        public bool useGridAttackRange;
        public BoardGrid board;
        [Min(1)] public float maxHealth = 30;
        [Min(.01f)] public float moveSpeed = 2.5f;
        [Min(0)] public float damage = 4;
        [Min(.1f)] public float attackRange = 2.7f;
        [Min(.05f)] public float attackInterval = .75f;
        [Min(1f), Tooltip("Damage dealt to fill this actor's personal Ultimate bar. Prototype tuning.")]
        public float damageForFullCharge = 30f;
        public Transform art;
        public SpriteRenderer selectionRing;
        public SpriteRenderer attackEffect;
        [SerializeField] Vector2Int currentCell;
        [SerializeField] bool isOnBoard;
        [SerializeField] float health;
        [SerializeField] ActorState state;
        [SerializeField, Range(0, 1)] float switchCharge;
        SpriteRenderer bodyRenderer;
        [SerializeField] Vector3 startingPosition;
        [SerializeField] Vector2Int startingCell;
        [SerializeField] bool hasStartingPosition;
        float effectTimer;
        float bounceTimer;
        float ultimateRemaining;
        float maxHealthBonus;
        float damageBonus;
        float castSpeedMultiplier = 1f;
        float damageReduction;
        int damageUpgradeRank;
        int castSpeedUpgradeRank;
        int healthUpgradeRank;
        Color originalBodyColor = Color.white;
        bool bodyColorCaptured;
        Transform visualRoot;
        Vector3 visualBaseScale = Vector3.one;
        bool visualScaleCaptured;
        public Vector2Int CurrentCell => currentCell;
        public bool IsOnBoard => isOnBoard;
        public float Health => health;
        public float EffectiveMaxHealth => maxHealth + maxHealthBonus;
        public float EffectiveDamage => damage + damageBonus;
        public float EffectiveAttackInterval => attackInterval / Mathf.Max(.01f, castSpeedMultiplier);
        public int DamageUpgradeRank => damageUpgradeRank;
        public int CastSpeedUpgradeRank => castSpeedUpgradeRank;
        public int HealthUpgradeRank => healthUpgradeRank;
        public bool Alive => health > 0;
        public bool CountsAsActiveRunMember => kind != ActorKind.Enemy && countsAsActiveRunMember;
        public Vector3 StartingPosition => startingPosition;
        public Vector2Int StartingCell => startingCell;
        public bool HasStartingPosition => hasStartingPosition;
        public ActorState State { get => state; set => state = value; }
        public float SwitchCharge => switchCharge;
        public float UltimateCharge => switchCharge;
        public bool UltimateFull => switchCharge >= .999f;
        public bool IsUltimateActive => ultimateRemaining > 0f;
        public float UltimateRemaining => ultimateRemaining;
        public float AttackTimer { get; set; }
        public float HitTimer { get; private set; }
        public float BounceTimer => bounceTimer;
        public bool CanPickUp
        {
            get
            {
                if (!Alive) return false;
                if (kind == ActorKind.Duck)
                    return state == ActorState.Panic || state == ActorState.Hiding ||
                           state == ActorState.Working || HitTimer > 0f;
                return UltimateFull;
            }
        }

        void Awake()
        {
            ResolveBodyRenderer();
            ResolveVisualScale();
        }

        void OnValidate()
        {
            damageForFullCharge = Mathf.Max(1f, damageForFullCharge);
            ResolveBodyRenderer();
        }

        void ResolveBodyRenderer()
        {
            if (!bodyRenderer) bodyRenderer = GetComponent<SpriteRenderer>();
            if (!bodyRenderer) bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (bodyRenderer && !bodyColorCaptured)
            {
                originalBodyColor = bodyRenderer.color;
                bodyColorCaptured = true;
            }
        }

        void ResolveVisualScale()
        {
            Transform target = art ? art : transform;
            if (visualRoot != target || !visualScaleCaptured)
            {
                visualRoot = target;
                visualBaseScale = target.localScale;
                visualScaleCaptured = true;
            }
        }

        void ApplyBounceScale()
        {
            ResolveVisualScale();
            if (!visualRoot) return;
            if (bounceTimer <= 0f)
            {
                visualRoot.localScale = visualBaseScale;
                return;
            }
            float progress = 1f - Mathf.Clamp01(bounceTimer / HitBounceDurationSeconds);
            float wave = Mathf.Sin(progress * Mathf.PI * 2f * HitBounceOscillations) * Mathf.Exp(-progress * HitBounceDamping);
            visualRoot.localScale = Vector3.Scale(visualBaseScale, new Vector3(
                1f + wave * HitBounceAmplitude,
                1f - wave * HitBounceAmplitude,
                1f));
        }

        public void ResetActor()
        {
            gameObject.SetActive(true);
            health = maxHealth;
            switchCharge = 0;
            ultimateRemaining = 0f;
            maxHealthBonus = 0f;
            damageBonus = 0f;
            castSpeedMultiplier = 1f;
            damageReduction = 0f;
            damageUpgradeRank = castSpeedUpgradeRank = healthUpgradeRank = 0;
            state = ActorState.Idle;
            AttackTimer = HitTimer = bounceTimer = 0;
            effectTimer = 0;
            ApplyBounceScale();
            RefreshCell();
            Present(0, false);
            if (TryGetComponent(out AlienAnimationDriver animationDriver)) animationDriver.ResetToIdle();
        }

        public void CaptureStartingPosition()
        {
            startingPosition = transform.position;
            RefreshCell();
            startingCell = isOnBoard ? currentCell : default;
            hasStartingPosition = true;
        }

        public void RestoreStartingPosition()
        {
            if (hasStartingPosition) transform.position = startingPosition;
            RefreshCell();
        }

        public void RefreshCell()
        {
            isOnBoard = board && board.TryWorldToCell(transform.position, out currentCell);
        }

        public float Hurt(float amount)
        {
            if (!Alive || invulnerable) return 0f;
            float before = health;
            float applied = Mathf.Max(0f, amount) * (1f - damageReduction);
            float dealt = Mathf.Min(before, applied);
            health = Mathf.Max(0, health - applied);
            HitTimer = HitFlashDurationSeconds;
            bounceTimer = HitBounceDurationSeconds;
            ResolveBodyRenderer();
            if (bodyRenderer) bodyRenderer.color = Color.Lerp(originalBodyColor, Color.red, .85f);
            ApplyBounceScale();
            Vector3 hitPosition = transform.position;
            if (!Alive) { state = ActorState.Dead; gameObject.SetActive(false); }
            if (dealt > 0f) Damaged?.Invoke(this, hitPosition, dealt);
            return dealt;
        }

        public float Heal(float amount)
        {
            if (!Alive) return 0f;
            float before = health;
            health = Mathf.Min(EffectiveMaxHealth, health + Mathf.Max(0f, amount));
            return health - before;
        }

        public void DebugRestoreHealth()
        {
            health = EffectiveMaxHealth;
            HitTimer = bounceTimer = 0;
            ResolveBodyRenderer();
            if (bodyRenderer) bodyRenderer.color = originalBodyColor;
            ApplyBounceScale();
            if (state == ActorState.Dead) state = ActorState.Idle;
            gameObject.SetActive(true);
            RefreshCell();
        }

        public void Revive()
        {
            health = EffectiveMaxHealth;
            switchCharge = ultimateRemaining = effectTimer = 0f;
            AttackTimer = HitTimer = bounceTimer = 0f;
            state = ActorState.Idle;
            ResolveBodyRenderer();
            if (bodyRenderer) bodyRenderer.color = originalBodyColor;
            ApplyBounceScale();
            if (attackEffect) attackEffect.enabled = false;
            gameObject.SetActive(true);
            RefreshCell();
            if (TryGetComponent(out AlienAnimationDriver animationDriver)) animationDriver.ResetToIdle();
        }

        public void Eliminate()
        {
            if (!Alive || invulnerable) return;
            health = 0f;
            HitTimer = bounceTimer = 0f;
            ApplyBounceScale();
            state = ActorState.Dead;
            gameObject.SetActive(false);
        }

        public void AddCharge(float dealt) => AddUltimateCharge(dealt / Mathf.Max(1f, damageForFullCharge));
        public void AddUltimateCharge(float normalizedAmount) =>
            switchCharge = Mathf.Clamp01(switchCharge + Mathf.Max(0f, normalizedAmount));
        public void SpendCharge() => switchCharge = 0;

        public bool BeginUltimate(float duration)
        {
            if (!Alive || !UltimateFull || kind == ActorKind.Duck) return false;
            SpendCharge();
            ultimateRemaining = Mathf.Max(.05f, duration);
            state = ActorState.UsingUltimate;
            if (TryGetComponent(out AlienAnimationDriver animationDriver)) animationDriver.PlayUltimate();
            return true;
        }

        public void BeginDuckOverdrive(float duration)
        {
            if (!Alive || kind != ActorKind.Duck || !UltimateFull) return;
            SpendCharge();
            ultimateRemaining = Mathf.Max(.05f, duration);
            state = ActorState.UsingUltimate;
            if (TryGetComponent(out AlienAnimationDriver animationDriver)) animationDriver.PlayUltimate();
        }

        public void AdvanceRuntime(float dt)
        {
            if (dt <= 0f) return;
            HitTimer = Mathf.Max(0f, HitTimer - dt);
            bounceTimer = Mathf.Max(0f, bounceTimer - dt);
            effectTimer = Mathf.Max(0f, effectTimer - dt);
            ultimateRemaining = Mathf.Max(0f, ultimateRemaining - dt);
            if (attackEffect) attackEffect.enabled = effectTimer > 0f;
            if (bodyRenderer)
                bodyRenderer.color = HitTimer > 0f ? Color.Lerp(originalBodyColor, Color.red, .85f) : originalBodyColor;
            ApplyBounceScale();
            if (!IsUltimateActive && state == ActorState.UsingUltimate) state = ActorState.Idle;
        }

        public void AddDamageUpgrade(float amount)
        {
            damageBonus += Mathf.Max(0f, amount);
            damageUpgradeRank++;
        }

        public void AddCastSpeedUpgrade(float multiplier)
        {
            castSpeedMultiplier *= Mathf.Max(1f, multiplier);
            castSpeedUpgradeRank++;
        }

        public void AddMaxHealthUpgrade(float amount)
        {
            float increase = Mathf.Max(0f, amount);
            maxHealthBonus += increase;
            health = Mathf.Min(EffectiveMaxHealth, health + increase);
            healthUpgradeRank++;
        }

        public void AddChipDamage(float amount) => damageBonus += Mathf.Max(0f, amount);

        public void SetDamageReduction(float amount) => damageReduction = Mathf.Clamp01(amount);

        public void SetPresentationColor(Color color)
        {
            ResolveBodyRenderer();
            originalBodyColor = color;
            bodyColorCaptured = true;
            if (bodyRenderer) bodyRenderer.color = color;
        }

        public void ShowAttack(float radius)
        {
            effectTimer = .22f;
            if (attackEffect) attackEffect.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
        }

        public void MoveTowards(Vector3 destination, float dt)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * dt);
            RefreshCell();
        }

        public void Present(float dt, bool selected)
        {
            AdvanceRuntime(dt);
            if (selectionRing) selectionRing.enabled = selected;
            ApplyBounceScale();
        }

        void OnDrawGizmosSelected()
        {
            if (kind == ActorKind.Duck) return;
            if (kind == ActorKind.Seal && useGridAttackRange && board)
            {
                board.TryWorldToCell(transform.position, out var center);
                Gizmos.color = new Color(1, .6f, .1f, .7f);
                for (int y=-2;y<=2;y++) for (int x=-2;x<=2;x++)
                {
                    var offset = new Vector2Int(x,y); var cell = center + offset;
                    if (SealGridRange.Contains(offset) && board.Contains(cell))
                        Gizmos.DrawWireCube(board.CellCenter(cell), board.transform.TransformVector(Vector3.one * board.cellSize * .96f));
                }
                return;
            }
            Gizmos.color = new Color(.4f, .94f, .77f, .65f);
            Vector3 previous = transform.position + Vector3.right * attackRange;
            for (int i = 1; i <= 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                Vector3 next = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * attackRange;
                Gizmos.DrawLine(previous, next); previous = next;
            }
        }
    }
}
