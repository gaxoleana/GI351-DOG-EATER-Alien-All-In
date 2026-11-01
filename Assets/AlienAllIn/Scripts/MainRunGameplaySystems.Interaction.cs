using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        [Header("Duck rescue")]
        [SerializeField, Min(1)] int duckRansomChips = 5;
        [SerializeField, Min(1)] float duckRescueSeconds = 15;
        [SerializeField, Min(0)] float duckRescueGraceSeconds = 3;
        readonly Dictionary<GridActor, Vector2Int> manualDestinations = new Dictionary<GridActor, Vector2Int>();
        readonly List<GridActor> finishedOrders = new List<GridActor>();
        GridActor duckCaptor;
        float duckRescueRemaining, duckGraceRemaining;
        public bool DuckCaptured => duckCaptor && duckCaptor.Alive;
        public float DuckRescueRemaining => duckRescueRemaining;
        public int DuckRansomChips => duckRansomChips;

        void ResetOrdersAndRescue()
        {
            manualDestinations.Clear(); duckCaptor = null;
            duckRescueRemaining = duckGraceRemaining = 0;
        }

        public bool CanOrderMove(GridActor actor, Vector2Int destination)
        {
            if (!actor || !actor.Alive || actor.kind == ActorKind.Enemy || !board.Contains(destination) ||
                runLoop.State == RunState.Defeated || runLoop.State == RunState.RunComplete ||
                (actor == duck && DuckCaptured)) return false;
            RefreshBoardOccupancy();
            var cell = board.GetCell(destination);
            foreach (var occupant in cell.Occupants)
                if (occupant && occupant.Alive && occupant != actor) return false;
            foreach (var order in manualDestinations)
                if (order.Key != actor && order.Value == destination) return false;
            // Matching station cells are valid destinations; incompatible stations are rejected.
            foreach (var station in stations)
                if (station && station.Cell == destination && actor != duck && station.requiredKind != actor.kind) return false;
            return true;
        }
        public bool TryOrderMove(GridActor actor, Vector2Int destination)
        {
            if (!CanOrderMove(actor, destination)) return false;
            var cell = board.GetCell(destination);
            if (runLoop.State == RunState.Placement)
            {
                actor.transform.position = board.CellCenter(destination); actor.RefreshCell();
            }
            else
            {
                manualDestinations[actor] = destination;
                if (actor == pufferfish) ResetPufferCycle();
                if (actor == seal) { sealAttackPending = false; sealTelegraphClock = 0; if (attackVfx) attackVfx.Clear(); }
                actor.State = ActorState.Moving;
            }
            RefreshBoardOccupancy();
            SetStatusFromUI($"{actor.kind} moving to {cell.Address}.");
            return true;
        }

        void AdvanceOrdersAndRescue(float dt)
        {
            duckGraceRemaining = Mathf.Max(0, duckGraceRemaining - dt);
            if (duckCaptor)
            {
                if (!duckCaptor.Alive) ReleaseDuck();
                else
                {
                    duck.State = ActorState.Kidnapped;
                    duck.transform.position = duckCaptor.transform.position + Vector3.up * .35f;
                    duck.RefreshCell();
                    duckRescueRemaining = Mathf.Max(0, duckRescueRemaining - dt);
                    if (duckRescueRemaining <= 0) { runLoop.ReportDefeat(); return; }
                }
            }
            finishedOrders.Clear();
            foreach (var order in manualDestinations)
            {
                var actor = order.Key;
                if (!actor || !actor.Alive || !board.Contains(order.Value)) { finishedOrders.Add(actor); continue; }
                Vector3 target = board.CellCenter(order.Value);
                MoveActorAlongGrid(actor, target, dt, 1);
                if (Vector3.Distance(actor.transform.position, target) <= .03f * board.cellSize)
                { actor.transform.position = target; actor.RefreshCell(); actor.State = ActorState.Idle; finishedOrders.Add(actor); }
            }
            foreach (var actor in finishedOrders) manualDestinations.Remove(actor);
        }

        void EnemyHitActor(GridActor enemy, GridActor target, float damage)
        {
            if (target == duck)
            {
                if (DuckCaptured || duckGraceRemaining > 0 || !enemy || !enemy.Alive) return;
                duckCaptor = enemy; duckRescueRemaining = duckRescueSeconds;
                manualDestinations.Remove(duck); duck.State = ActorState.Kidnapped;
                SetStatusFromUI($"Duck kidnapped! Pay {duckRansomChips} Chips or defeat its captor within {duckRescueSeconds:0}s.");
                return;
            }
            target.Hurt(damage); InterruptWorkerAt(target);
        }

        public bool TryFreeDuck()
        {
            if (!DuckCaptured || chips < duckRansomChips || runLoop.State == RunState.Defeated || runLoop.State == RunState.RunComplete) return false;
            chips -= duckRansomChips; ReleaseDuck(); return true;
        }

        void ReleaseDuck()
        {
            duckCaptor = null; duckRescueRemaining = 0; duckGraceRemaining = duckRescueGraceSeconds;
            duck.State = ActorState.Idle;
            float nearest = float.PositiveInfinity; Vector3 destination = duck.transform.position;
            foreach (var cell in stage.ProcessSpawnCandidates)
            {
                Vector3 point = board.CellCenter(cell); bool blocked = false;
                foreach (var actor in runLoop.Survivors)
                    if (actor && actor != duck && actor.Alive && Vector3.Distance(point, actor.transform.position) < .5f) blocked = true;
                if (blocked) continue;
                float distance = (point - duck.transform.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; destination = point; }
            }
            duck.transform.position = destination; duck.RefreshCell(); RefreshBoardOccupancy();
            SetStatusFromUI("Duck is free.");
        }
    }
}
