using UnityEngine;

namespace AlienAllIn
{
    public sealed class ActivityStation : MonoBehaviour
    {
        public string activityName = "CASINO";
        public ActorKind requiredKind = ActorKind.Duck;
        public BoardGrid board;
        [Min(1)] public float duration = 36;
        public SpriteRenderer marker;
        [SerializeField] float progress;
        float rewardClock;
        public float Progress => progress;
        public bool Complete => progress >= 1;
        public Vector2Int Cell { get { board.TryWorldToCell(transform.position, out var cell); return cell; } }
        public void ResetActivity() { progress = rewardClock = 0; RefreshView(); }

        public int Advance(float dt, GridActor worker, bool threatened)
        {
            if (Complete || !worker || !worker.Alive || !worker.IsOnBoard || worker.kind != requiredKind || worker.CurrentCell != Cell || threatened) return 0;
            // Require the center, so merely crossing the cell boundary does not start work.
            if (Vector3.Distance(worker.transform.position, transform.position) > .15f * board.cellSize) return 0;
            float work = Mathf.Min(dt, (1 - progress) * duration);
            progress = Mathf.Min(1, progress + work / duration);
            worker.State = ActorState.Working;
            rewardClock += work;
            int cash = Mathf.FloorToInt(rewardClock);
            rewardClock -= cash;
            RefreshView();
            return cash;
        }

        public void RefreshView()
        {
            if (marker) marker.color = Complete ? new Color(.25f, 1, .65f) : new Color(.87f, .73f, .36f);
        }
    }
}
