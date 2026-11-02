using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        public bool DebugStopEnemySpawns { get; set; }
        public bool DebugStopStationSpawns { get; set; }
        public bool DebugBoardOverride => stage && stage.HasDebugSize;
        public bool DebugCanEdit => Application.isPlaying && runLoop &&
            runLoop.State != RunState.Defeated && runLoop.State != RunState.RunComplete;

        public bool DebugSpawnEnemy(EnemyArchetype? kind = null)
        {
            if (!DebugCanEdit) return DebugReport(false, "Restart the session before spawning.");
            CleanupRuntimeObjects();
            RefreshBoardOccupancy();
            var settings = kind.HasValue ? System.Array.Find(enemySettings, e => e != null && e.archetype == kind.Value) : ChooseEnemySettings();
            GridActor enemy = SpawnEnemy(settings, true);
            RefreshBoardOccupancy();
            return DebugReport(enemy, enemy ? $"Spawned {enemy.name}." : "No free cell or enemy configuration.");
        }

        public bool DebugSpawnStation(ActivityKind kind)
        {
            if (!DebugCanEdit) return DebugReport(false, "Restart the session before spawning.");
            if (kind == ActivityKind.Casino) return DebugReport(false, "The permanent Casino is already on the board.");
            RefreshBoardOccupancy();
            var settings = System.Array.Find(activitySettings, a => a != null && a.kind == kind);
            bool spawned = SpawnActivity(settings);
            CleanupRuntimeObjects();
            RefreshBoardOccupancy();
            if (spawned) DebugReport(true, $"Spawned {kind}; normal worker and scoring rules apply.");
            return spawned;
        }

        public void DebugKillEnemies(bool awardScore = true)
        {
            foreach (GridActor enemy in enemies)
            {
                if (!enemy) continue;
                if (!awardScore) awardedKills.Add(enemy);
                pendingKillIsUltimate[enemy] = false;
                enemy.Eliminate();
            }
            ClearCombatRuntimeObjects();
            CleanupRuntimeObjects();
            RefreshBoardOccupancy();
            DebugReport(true, awardScore ? "Killed all enemies; normal kill points awarded." : "Cleared enemies and projectiles without score.");
        }

        public void DebugClearWorld()
        {
            DebugKillEnemies(false);
            foreach (var station in stations)
                if (station && station.CurrentWorker && station.CurrentWorker.State == ActorState.Working) station.CurrentWorker.State = ActorState.Idle;
            ClearTransientStations(); ClearSpawned(fieldItems);
            RefreshBoardOccupancy();
            DebugReport(true, "Cleared enemies, stations, drops and projectiles. Coins and Score kept.");
        }

        public bool DebugSetBoardSize(int side)
        {
            if (!DebugCanEdit) return DebugReport(false, "Restart the session before resizing.");
            side = Mathf.Clamp(side, 1, Mathf.Min(board.width, board.height));
            int living = 0;
            foreach (GridActor actor in runLoop.Survivors) if (actor && actor.Alive) living++;
            if (side * side < living + (casinoMachine ? 1 : 0))
                return DebugReport(false, "Board needs room for the living team and the permanent Casino.");
            stage.SetDebugSize(side, side);
            ReconcileDebugBoard();
            return DebugReport(true, $"Manual board: {side}x{side}. Auto or Restart restores timed expansion.");
        }

        public void DebugUseAutomaticBoard()
        {
            stage.ClearDebugSize();
            ReconcileDebugBoard();
            DebugReport(true, "Restored timed board expansion.");
        }

        void ReconcileDebugBoard(bool clearProjectiles = true)
        {
            var occupied = new HashSet<Vector2Int>();
            var displaced = new List<GridActor>();
            foreach (GridActor actor in runLoop.Survivors)
            {
                if (!actor || !actor.Alive) continue;
                actor.RefreshCell();
                if (!actor.IsOnBoard || !occupied.Add(actor.CurrentCell)) displaced.Add(actor);
            }
            if (casinoMachine && !board.Contains(casinoMachine.Cell))
            {
                foreach (Vector2Int candidate in stage.ProcessSpawnCandidates)
                {
                    if (occupied.Contains(candidate)) continue;
                    casinoMachine.transform.position = board.CellCenter(candidate);
                    casinoMachine.RefreshView();
                    occupied.Add(candidate);
                    break;
                }
            }
            foreach (GridActor actor in displaced)
            {
                float nearest = float.PositiveInfinity;
                Vector2Int target = board.ActiveMinCell;
                foreach (Vector2Int candidate in stage.ProcessSpawnCandidates)
                {
                    if (occupied.Contains(candidate) || IsCasinoCell(candidate)) continue;
                    float distance = (board.CellCenter(candidate) - actor.transform.position).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; target = candidate;
                }
                occupied.Add(target);
                actor.transform.position = board.CellCenter(target); actor.RefreshCell();
            }
            for (int i = stations.Count - 1; i >= 0; i--)
            {
                if (stations[i] && board.Contains(stations[i].Cell)) continue;
                if (stations[i] == casinoMachine) continue;
                if (stations[i])
                {
                    if (stations[i].CurrentWorker && stations[i].CurrentWorker.State == ActorState.Working) stations[i].CurrentWorker.State = ActorState.Idle;
                    Destroy(stations[i].gameObject);
                }
                stations.RemoveAt(i);
            }
            for (int i = fieldItems.Count - 1; i >= 0; i--)
            {
                if (fieldItems[i] && board.TryWorldToCell(fieldItems[i].transform.position, out _)) continue;
                if (fieldItems[i]) Destroy(fieldItems[i].gameObject);
                fieldItems.RemoveAt(i);
            }
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || board.TryWorldToCell(enemy.transform.position, out _)) continue;
                awardedKills.Add(enemy); enemy.Eliminate();
            }
            if (clearProjectiles) ClearCombatRuntimeObjects();
            CleanupRuntimeObjects(); RefreshBoardOccupancy();
        }

        public void DebugRestoreTeam()
        {
            if (!DebugCanEdit) { DebugReport(false, "Use Restart to restore a finished session."); return; }
            foreach (GridActor actor in runLoop.Survivors) if (actor) actor.DebugRestoreHealth();
            // A small manual board may not fit revived members; expand it before placing them.
            if (board.ActiveWidth * board.ActiveHeight < runLoop.Survivors.Count)
                stage.SetDebugSize(Mathf.CeilToInt(Mathf.Sqrt(runLoop.Survivors.Count)), Mathf.CeilToInt(Mathf.Sqrt(runLoop.Survivors.Count)));
            ReconcileDebugBoard(false);
            DebugReport(true, "Team healed/revived; upgrades preserved.");
        }

        public void DebugFillUltimates()
        {
            foreach (GridActor actor in runLoop.Survivors) if (actor && actor.Alive) actor.AddUltimateCharge(1f);
            DebugReport(true, "All living members have full Ultimate gauges.");
        }

        public void DebugGrantCash(int amount = 100) { coins += Mathf.Max(0, amount); DebugReport(true, $"Coins +{Mathf.Max(0, amount)}."); }
        public void DebugGrantItems(int amount = 5)
        {
            for (int i = 0; i < itemInventory.Length; i++) itemInventory[i] += Mathf.Max(0, amount);
            DebugReport(true, $"Added {Mathf.Max(0, amount)} of each item.");
        }
        public void DebugGrantChips(int amount = 5)
        {
            chips += Mathf.Max(0, amount);
            DebugReport(true, $"Chips +{Mathf.Max(0, amount)}.");
        }
        public void DebugGrantScore(int amount = 1000) { score += Mathf.Max(0, amount); DebugReport(true, $"Score +{Mathf.Max(0, amount)}."); }

        bool DebugReport(bool success, string message)
        {
            statusMessage = "DEBUG / " + message; RunDataChanged?.Invoke(); return success;
        }
    }
}
