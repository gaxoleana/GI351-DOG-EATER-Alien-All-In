using UnityEngine;

namespace AlienAllIn
{
    public sealed class ActivityStation : MonoBehaviour
    {
        public string activityName = "CASINO";
        public ActorKind requiredKind = ActorKind.Duck;
        public BoardGrid board;
        [Min(1)] public float duration = 36;
        [Min(.05f), Tooltip("Prototype interval for one Cash per tick while work is progressing.")]
        public float cashTickSeconds = 1f;
        [Tooltip("The Duck can work any species' station as a universal processor.")]
        public bool allowUniversalDuckWorker = true;
        [Min(0f), Tooltip("Prototype pause applied after an Enemy hits this station or its current worker.")]
        public float interruptionSeconds = 2f;
        public SpriteRenderer marker;
        [SerializeField] float progress;
        float rewardClock;
        float interruptionRemaining;
        GridActor currentWorker;
        public float Progress => progress;
        public bool Complete => progress >= 1;
        public float InterruptionRemaining => interruptionRemaining;
        public GridActor CurrentWorker => currentWorker;
        public bool IsInterrupted => interruptionRemaining > 0;
        public Vector2Int Cell { get { board.TryWorldToCell(transform.position, out var cell); return cell; } }
        public void ResetActivity()
        {
            progress = rewardClock = interruptionRemaining = 0;
            currentWorker = null;
            RefreshView();
        }

        public void Interrupt()
        {
            interruptionRemaining = Mathf.Max(interruptionRemaining, Mathf.Max(0f, interruptionSeconds));
        }

        public int Advance(float dt, GridActor worker, bool threatened) => Advance(dt, worker, threatened, 1f);

        public int Advance(float dt, GridActor worker, bool threatened, float workRate)
        {
            if (dt <= 0f || Complete) return 0;
            if (interruptionRemaining > 0f)
            {
                interruptionRemaining = Mathf.Max(0f, interruptionRemaining - dt);
                return 0;
            }
            bool correctWorker = worker && (worker.kind == requiredKind ||
                (allowUniversalDuckWorker && worker.kind == ActorKind.Duck));
            if (!board || !correctWorker || !worker.Alive || !worker.IsOnBoard || worker.CurrentCell != Cell)
            {
                currentWorker = null;
                return 0;
            }
            // Require the center, so merely crossing the cell boundary does not start work.
            if (!board || Vector3.Distance(worker.transform.position, transform.position) > .15f * board.cellSize)
            {
                currentWorker = null;
                return 0;
            }
            currentWorker = worker;
            if (threatened) return 0;

            float safeDuration = Mathf.Max(.01f, duration);
            float safeRate = Mathf.Max(.01f, workRate);
            float work = Mathf.Min(dt, (1 - progress) * safeDuration / safeRate);
            progress = Mathf.Min(1, progress + work * safeRate / safeDuration);
            worker.State = ActorState.Working;
            rewardClock += work;
            float tick = Mathf.Max(.05f, cashTickSeconds);
            int cash = Mathf.FloorToInt(rewardClock / tick);
            rewardClock -= cash * tick;
            RefreshView();
            return cash;
        }

        public void RefreshView()
        {
            if (marker) marker.color = Complete ? new Color(.25f, 1, .65f) : new Color(.87f, .73f, .36f);
        }
    }
}
