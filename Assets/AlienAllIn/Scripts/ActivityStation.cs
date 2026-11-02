using UnityEngine;

namespace AlienAllIn
{
    public sealed class ActivityStation : MonoBehaviour
    {
        public ActivityKind activityKind = ActivityKind.Casino;
        public string activityName = "CASINO";
        public ActorKind requiredKind = ActorKind.Duck;
        public BoardGrid board;
        [Min(1)] public float duration = 36;
        [Min(.05f), Tooltip("Prototype interval for one Cash per tick while work is progressing.")]
        public float cashTickSeconds = 1f;
        [Min(0f), Tooltip("Prototype interval for Processing score ticks. Zero disables Processing score ticks.")]
        public float scoreTickSeconds = 2f;
        [Tooltip("Legacy compatibility field. Station work is restricted to Duck.")]
        public bool allowUniversalDuckWorker = true;
        [Min(0f), Tooltip("Prototype pause applied after an Enemy hits this station or its current worker.")]
        public float interruptionSeconds = 2f;
        public SpriteRenderer marker;
        [Header("Optional station artwork")]
        public SpriteRenderer artwork;
        public Transform progressFill;
        [Min(.01f)] public float progressBarWidth = .82f;
        public Color workColor = new Color(.87f, .73f, .36f);
        [SerializeField] float progress;
        float rewardClock;
        float scoreClock;
        float interruptionRemaining;
        GridActor currentWorker;
        bool continuousProgress;
        public float Progress => progress;
        public bool Complete => !continuousProgress && progress >= 1;
        public float InterruptionRemaining => interruptionRemaining;
        public GridActor CurrentWorker => currentWorker;
        public void ReleaseWorker() => currentWorker = null;
        public bool IsInterrupted => interruptionRemaining > 0;
        public Vector2Int Cell { get { board.TryWorldToCell(transform.position, out var cell); return cell; } }
        public void ResetActivity()
        {
            progress = rewardClock = scoreClock = interruptionRemaining = 0;
            currentWorker = null;
            RefreshView();
        }

        public void Interrupt()
        {
            interruptionRemaining = Mathf.Max(interruptionRemaining, Mathf.Max(0f, interruptionSeconds));
            RefreshView();
        }

        public void SetContinuousProgress(bool enabled) => continuousProgress = enabled;

        public void ClearInterruption()
        {
            interruptionRemaining = 0f;
            RefreshView();
        }

        public bool AdvanceContinuous(float dt, GridActor worker, float workRate)
            => AdvanceContinuous(dt, worker, workRate, out _);

        public void FinishCycle()
        {
            progress = 1f;
            currentWorker = null;
            RefreshView();
        }

        public bool AdvanceContinuous(float dt, GridActor worker, float workRate, out float workedSeconds)
        {
            workedSeconds = 0f;
            if (dt <= 0f || progress >= 1f) return false;
            if (interruptionRemaining > 0f)
            {
                interruptionRemaining = Mathf.Max(0f, interruptionRemaining - dt);
                RefreshView();
                return false;
            }

            bool eligible = board && worker && worker.kind == ActorKind.Duck && worker.Alive && worker.IsOnBoard && worker.CurrentCell == Cell &&
                Vector3.Distance(worker.transform.position, transform.position) <= .15f * board.cellSize;
            if (!eligible)
            {
                currentWorker = null;
                return false;
            }

            currentWorker = worker;
            worker.State = ActorState.Working;
            float rate = Mathf.Max(.01f, workRate), cycleDuration = Mathf.Max(.01f, duration);
            float remaining = (1f - progress) * cycleDuration / rate;
            workedSeconds = Mathf.Min(dt, remaining);
            progress = remaining <= dt ? 1f : progress + dt * rate / cycleDuration;
            RefreshView();
            return true;
        }

        public int Advance(float dt, GridActor worker, bool threatened) =>
            Advance(dt, worker, threatened, 1f);

        public int Advance(float dt, GridActor worker, bool threatened, float workRate) =>
            Advance(dt, worker, threatened, workRate, 0f, out _);

        public int Advance(float dt, GridActor worker, bool threatened, float workRate, float processScoreTickSeconds, out int scoreTicks)
        {
            scoreTicks = 0;
            if (dt <= 0f || Complete) return 0;
            if (interruptionRemaining > 0f)
            {
                interruptionRemaining = Mathf.Max(0f, interruptionRemaining - dt);
                RefreshView();
                return 0;
            }
            bool correctWorker = worker && worker.kind == ActorKind.Duck;
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
            float scoreTick = Mathf.Max(0f, processScoreTickSeconds > 0f ? processScoreTickSeconds : scoreTickSeconds);
            if (scoreTick > 0f)
            {
                scoreClock += work;
                scoreTicks = Mathf.FloorToInt(scoreClock / scoreTick);
                scoreClock -= scoreTicks * scoreTick;
            }
            RefreshView();
            return cash;
        }

        public void RefreshView()
        {
            if (marker) marker.color = Complete ? new Color(.25f, 1, .65f) : workColor;
            if (artwork) artwork.color = IsInterrupted ? new Color(1, .65f, .65f) : Color.white;
            if (progressFill)
            {
                float fill = Mathf.Clamp01(progress);
                progressFill.localScale = new Vector3(progressBarWidth * fill, .055f, 1);
                progressFill.localPosition = new Vector3(-progressBarWidth * (1 - fill) * .5f, -.46f, 0);
            }
        }
    }
}
