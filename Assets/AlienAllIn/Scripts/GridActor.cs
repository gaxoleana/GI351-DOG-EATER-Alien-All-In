using UnityEngine;

namespace AlienAllIn
{
    // Keep existing serialized enum values stable; new species are appended after Enemy.
    public enum ActorKind { Duck, Seal, Enemy, Cat, Pufferfish }
    public enum ActorState { Idle, Moving, Collecting, Working, Fleeing, Hiding, Defending, Dead }

    [DisallowMultipleComponent]
    public sealed class GridActor : MonoBehaviour
    {
        public ActorKind kind;
        [Tooltip("Only active run members contribute to the RunLoop defeat condition. Enemies are never run members.")]
        public bool countsAsActiveRunMember = true;
        public BoardGrid board;
        [Min(1)] public float maxHealth = 30;
        [Min(.01f)] public float moveSpeed = 2.5f;
        [Min(0)] public float damage = 4;
        [Min(.1f)] public float attackRange = 2.7f;
        [Min(.05f)] public float attackInterval = .75f;
        public Transform art;
        public SpriteRenderer selectionRing;
        public SpriteRenderer attackEffect;
        [SerializeField] Vector2Int currentCell;
        [SerializeField] bool isOnBoard;
        [SerializeField] float health;
        [SerializeField] ActorState state;
        [SerializeField, Range(0, 1)] float switchCharge;
        [SerializeField] Vector3 startingPosition;
        [SerializeField] Vector2Int startingCell;
        [SerializeField] bool hasStartingPosition;
        float effectTimer;
        public Vector2Int CurrentCell => currentCell;
        public bool IsOnBoard => isOnBoard;
        public float Health => health;
        public bool Alive => health > 0;
        public bool CountsAsActiveRunMember => kind != ActorKind.Enemy && countsAsActiveRunMember;
        public Vector3 StartingPosition => startingPosition;
        public Vector2Int StartingCell => startingCell;
        public bool HasStartingPosition => hasStartingPosition;
        public ActorState State { get => state; set => state = value; }
        public float SwitchCharge => switchCharge;
        public float AttackTimer { get; set; }
        public float HitTimer { get; private set; }
        public bool CanPickUp => Alive && (kind != ActorKind.Duck || (state != ActorState.Fleeing && HitTimer <= 0));

        public void ResetActor()
        {
            gameObject.SetActive(true);
            health = maxHealth;
            switchCharge = 0;
            state = ActorState.Idle;
            AttackTimer = HitTimer = 0;
            effectTimer = 0;
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

        public void Hurt(float amount)
        {
            if (!Alive) return;
            health = Mathf.Max(0, health - Mathf.Max(0, amount));
            HitTimer = .25f;
            if (!Alive) { state = ActorState.Dead; gameObject.SetActive(false); }
        }

        public void AddCharge(float dealt) => switchCharge = Mathf.Clamp01(switchCharge + dealt / 30f);
        public void SpendCharge() => switchCharge = 0;

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
            HitTimer = Mathf.Max(0, HitTimer - dt);
            effectTimer = Mathf.Max(0, effectTimer - dt);
            if (attackEffect) attackEffect.enabled = effectTimer > 0;
            if (selectionRing) selectionRing.enabled = selected;
            if (art) art.localScale = HitTimer > 0 ? new Vector3(1.12f, .86f, 1) : Vector3.one;
        }

        void OnDrawGizmosSelected()
        {
            if (kind == ActorKind.Duck) return;
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
