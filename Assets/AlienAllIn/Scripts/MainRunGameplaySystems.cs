using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    /// <summary>
    /// Main-scene gameplay slice. RunLoopController remains the only owner of the
    /// clock, pause state, and run outcome; this component advances combat, Process,
    /// Cash, and spawning only from its GameplayDeltaTime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainRunGameplaySystems : MonoBehaviour
    {
        [Header("Run and stage references")]
        [SerializeField] RunLoopController runLoop;
        [SerializeField] StageGridProgression stage;
        [SerializeField] BoardGrid board;
        [SerializeField] GridActor duck;
        [SerializeField] GridActor seal;
        [SerializeField] GridActor enemyPrefab;
        [SerializeField] ActivityStation casinoPrefab;
        [SerializeField] Transform enemyContainer;
        [SerializeField] Transform processContainer;

        [Header("Prototype Process spawning")]
        [SerializeField, Min(1f)] float processSpawnIntervalSeconds = 60f;
        [SerializeField, Range(0f, 1f)] float processSpawnChance = .25f;
        [SerializeField, Min(1f)] float casinoDurationSeconds = 36f;
        [SerializeField, Min(.05f)] float cashTickSeconds = 1f;
        [SerializeField, Range(1f, 2f)] float duckProcessingMultiplier = 1.1f;
        [SerializeField, Min(0f)] float processInterruptionSeconds = 2f;

        [Header("Prototype enemy spawning")]
        [SerializeField, Min(.1f)] float firstEnemyDelaySeconds = 7f;
        [SerializeField, Min(.1f)] float enemySpawnIntervalSeconds = 4.5f;
        [SerializeField, Min(1)] int maxEnemies = 8;
        [SerializeField, Min(0f)] float sealKnockbackDistance = .45f;

        [Header("Live run resources")]
        [SerializeField] int cash;
        [SerializeField] int processedStations;
        [SerializeField] string statusMessage = "No Process station yet.";

        readonly List<GridActor> enemies = new List<GridActor>();
        readonly List<ActivityStation> stations = new List<ActivityStation>();
        readonly List<GridActor> boardActors = new List<GridActor>();
        readonly List<Vector2Int> validSpawnCells = new List<Vector2Int>();
        readonly HashSet<Vector2Int> processCells = new HashSet<Vector2Int>();
        float processSpawnClock;
        float enemySpawnClock;
        int stationSpawnIndex;
        int enemySpawnIndex;
        int nextEnemySpawnEdge;

        public int Cash => cash;
        public int ProcessedStations => processedStations;
        public IReadOnlyList<GridActor> Enemies => enemies;
        public IReadOnlyList<ActivityStation> Stations => stations;

        void OnEnable()
        {
            if (runLoop) runLoop.RunRestarted += ResetForNewRun;
        }

        void OnDisable()
        {
            if (runLoop) runLoop.RunRestarted -= ResetForNewRun;
        }

        void Awake()
        {
            if (!runLoop || !stage || !board || !duck || !seal || !enemyPrefab || !casinoPrefab)
            {
                Debug.LogError("MainRunGameplaySystems requires RunLoop, Stage, Board, Duck, Seal, and both prototype prefabs.", this);
                enabled = false;
                return;
            }

            if (!enemyContainer) enemyContainer = transform;
            if (!processContainer) processContainer = transform;
            ResetForNewRun();
        }

        void Update()
        {
            if (!enabled || !runLoop || runLoop.State != RunState.Running) return;
            float dt = runLoop.GameplayDeltaTime;
            if (dt <= 0f) return;

            RefreshBoardOccupancy();
            AdvanceSpawnClocks(dt);
            TickSeal(dt);
            TickDuck(dt);
            AdvanceProcesses(dt);
            RefreshBoardOccupancy();
            TickEnemies(dt);
            CleanupDeadEnemies();
            RefreshBoardOccupancy();
        }

        void ResetForNewRun()
        {
            ClearSpawned(enemies);
            ClearSpawned(stations);
            cash = 0;
            processedStations = 0;
            processSpawnClock = 0f;
            enemySpawnClock = Mathf.Max(.1f, firstEnemyDelaySeconds);
            stationSpawnIndex = 0;
            enemySpawnIndex = 0;
            nextEnemySpawnEdge = 0;
            statusMessage = "No Process station yet.";
        }

        void ClearSpawned<T>(List<T> spawned) where T : Component
        {
            foreach (T item in spawned)
            {
                if (!item) continue;
                if (Application.isPlaying) Destroy(item.gameObject);
                else DestroyImmediate(item.gameObject);
            }
            spawned.Clear();
        }

        void AdvanceSpawnClocks(float dt)
        {
            float processInterval = Mathf.Max(1f, processSpawnIntervalSeconds);
            processSpawnClock += dt;
            while (processSpawnClock >= processInterval)
            {
                processSpawnClock -= processInterval;
                if (Random.value < Mathf.Clamp01(processSpawnChance)) TrySpawnCasino();
            }

            enemySpawnClock -= dt;
            if (enemySpawnClock > 0f) return;

            if (CountLivingEnemies() < Mathf.Max(1, maxEnemies)) TrySpawnEnemy();
            enemySpawnClock = Mathf.Max(.1f, enemySpawnIntervalSeconds);
        }

        bool TrySpawnCasino()
        {
            validSpawnCells.Clear();
            foreach (Vector2Int cell in stage.ProcessSpawnCandidates)
            {
                BoardCell boardCell = board.GetCell(cell);
                if (!boardCell || HasStationAt(cell)) continue;

                bool blockedByAlien = false;
                foreach (GridActor actor in boardCell.Occupants)
                {
                    if (actor && actor.Alive && actor.kind != ActorKind.Enemy)
                    {
                        blockedByAlien = true;
                        break;
                    }
                }
                if (!blockedByAlien) validSpawnCells.Add(cell);
            }

            if (validSpawnCells.Count == 0)
            {
                statusMessage = "Casino roll skipped: no valid cell. Try again next interval.";
                return false;
            }

            Vector2Int selectedCell = validSpawnCells[Random.Range(0, validSpawnCells.Count)];
            BoardCell selectedBoardCell = board.GetCell(selectedCell);
            foreach (GridActor actor in selectedBoardCell.Occupants)
                if (actor && actor.Alive && actor.kind == ActorKind.Enemy) actor.Hurt(actor.Health);

            ActivityStation stationInstance = Instantiate(
                casinoPrefab, board.CellCenter(selectedCell), Quaternion.identity, processContainer);
            stationInstance.name = $"CASINO {++stationSpawnIndex:00}";
            stationInstance.activityName = "CASINO";
            stationInstance.requiredKind = ActorKind.Seal;
            stationInstance.board = board;
            stationInstance.duration = Mathf.Max(1f, casinoDurationSeconds);
            stationInstance.cashTickSeconds = Mathf.Max(.05f, cashTickSeconds);
            stationInstance.allowUniversalDuckWorker = true;
            stationInstance.interruptionSeconds = Mathf.Max(0f, processInterruptionSeconds);
            stationInstance.ResetActivity();
            stations.Add(stationInstance);
            statusMessage = $"CASINO spawned at {selectedBoardCell.Address}.";
            return true;
        }

        bool HasStationAt(Vector2Int cell)
        {
            foreach (ActivityStation station in stations)
                if (station && !station.Complete && station.Cell == cell) return true;
            return false;
        }

        void TrySpawnEnemy()
        {
            processCells.Clear();
            foreach (ActivityStation station in stations)
                if (station && board.TryWorldToCell(station.transform.position, out Vector2Int cell)) processCells.Add(cell);

            GridActor enemy = GridEnemySpawner.SpawnAtActiveEdges(
                board, enemyPrefab, enemyContainer, ref enemySpawnIndex, ref nextEnemySpawnEdge, processCells);
            if (enemy) enemies.Add(enemy);
        }

        void TickSeal(float dt)
        {
            if (!seal || !seal.Alive || !seal.CountsAsActiveRunMember) return;

            seal.State = ActorState.Defending;
            seal.AttackTimer -= dt;
            bool inRange = false;
            foreach (GridActor enemy in enemies)
            {
                if (enemy && enemy.Alive && Vector3.Distance(enemy.transform.position, seal.transform.position) <= seal.attackRange)
                {
                    inRange = true;
                    break;
                }
            }
            if (!inRange || seal.AttackTimer > 0f) return;

            CombatAndSpawnRules.ResolveSealAoe(
                seal, enemies, board, seal.attackRange, seal.damage, sealKnockbackDistance);
            seal.AttackTimer = seal.attackInterval;
        }

        void TickDuck(float dt)
        {
            if (!duck || !duck.Alive || !duck.CountsAsActiveRunMember) return;

            ActivityStation target = FindNearestIncompleteStation(duck.transform.position);
            if (!target)
            {
                duck.State = ActorState.Idle;
                return;
            }

            float workDistance = .15f * board.cellSize;
            if (Vector3.Distance(duck.transform.position, target.transform.position) <= workDistance)
            {
                duck.State = ActorState.Working;
                return;
            }

            duck.State = ActorState.Moving;
            duck.MoveTowards(target.transform.position, dt);
        }

        ActivityStation FindNearestIncompleteStation(Vector3 position)
        {
            ActivityStation nearest = null;
            float bestDistance = float.PositiveInfinity;
            foreach (ActivityStation station in stations)
            {
                if (!station || station.Complete) continue;
                float distance = Vector3.Distance(position, station.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = station;
            }
            return nearest;
        }

        void AdvanceProcesses(float dt)
        {
            for (int i = stations.Count - 1; i >= 0; i--)
            {
                ActivityStation station = stations[i];
                if (!station)
                {
                    stations.RemoveAt(i);
                    continue;
                }

                cash += station.Advance(dt, duck, false, duckProcessingMultiplier);
                if (!station.Complete) continue;

                processedStations++;
                statusMessage = $"{station.activityName} processed.";
                if (duck) duck.State = ActorState.Idle;
                Destroy(station.gameObject);
                stations.RemoveAt(i);
            }
        }

        void TickEnemies(float dt)
        {
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || !enemy.Alive) continue;
                ChooseEnemyTarget(enemy.transform.position, out GridActor alienTarget, out ActivityStation stationTarget);
                if (alienTarget)
                {
                    CombatAndSpawnRules.TickMelee(enemy, alienTarget.transform.position, dt, () =>
                    {
                        alienTarget.Hurt(enemy.damage);
                        InterruptStationWorkedBy(alienTarget);
                    });
                }
                else if (stationTarget)
                {
                    CombatAndSpawnRules.TickMelee(enemy, stationTarget.transform.position, dt, stationTarget.Interrupt);
                }
            }
        }

        void ChooseEnemyTarget(Vector3 enemyPosition, out GridActor alienTarget, out ActivityStation stationTarget)
        {
            alienTarget = null;
            stationTarget = null;
            float bestDistance = float.PositiveInfinity;

            foreach (GridActor alien in runLoop.Survivors)
            {
                if (!alien || !alien.Alive || !alien.CountsAsActiveRunMember) continue;
                float distance = Vector3.Distance(enemyPosition, alien.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                alienTarget = alien;
                stationTarget = null;
            }

            foreach (ActivityStation station in stations)
            {
                if (!station || station.Complete) continue;
                float distance = Vector3.Distance(enemyPosition, station.transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                stationTarget = station;
                alienTarget = null;
            }
        }

        void InterruptStationWorkedBy(GridActor worker)
        {
            foreach (ActivityStation station in stations)
                if (station && station.CurrentWorker == worker) station.Interrupt();
        }

        void CleanupDeadEnemies()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                GridActor enemy = enemies[i];
                if (enemy && enemy.Alive && enemy.gameObject.activeInHierarchy) continue;
                if (enemy) Destroy(enemy.gameObject);
                enemies.RemoveAt(i);
            }
        }

        int CountLivingEnemies()
        {
            int count = 0;
            foreach (GridActor enemy in enemies) if (enemy && enemy.Alive) count++;
            return count;
        }

        void RefreshBoardOccupancy()
        {
            boardActors.Clear();
            foreach (GridActor actor in runLoop.Survivors) if (actor) boardActors.Add(actor);
            foreach (GridActor enemy in enemies) if (enemy) boardActors.Add(enemy);
            board.RefreshOccupancy(boardActors);
        }

        void OnGUI()
        {
            const float x = 16f;
            const float y = 164f;
            const float width = 196f;
            int stationRows = Mathf.Min(4, stations.Count);
            int enemyRows = Mathf.Min(5, CountLivingEnemies());
            float height = 58f + stationRows * 20f + enemyRows * 18f + 20f;
            GUI.Box(new Rect(x, y, width, height), string.Empty);
            GUI.Label(new Rect(x + 8f, y + 6f, width - 16f, 20f), "RUN SYSTEMS");
            GUI.Label(new Rect(x + 8f, y + 26f, width - 16f, 20f), $"CASH: {cash}   PROCESSED: {processedStations}");

            float rowY = y + 48f;
            for (int i = 0; i < stationRows; i++)
            {
                ActivityStation station = stations[i];
                if (!station) continue;
                string state = station.IsInterrupted
                    ? $"PAUSED {station.InterruptionRemaining:0.0}s"
                    : $"{Mathf.RoundToInt(station.Progress * 100f)}%";
                GUI.Label(new Rect(x + 8f, rowY, width - 16f, 18f), $"{station.activityName}: {state}");
                rowY += 20f;
            }
            if (stations.Count > stationRows)
            {
                GUI.Label(new Rect(x + 8f, rowY, width - 16f, 18f), $"+ {stations.Count - stationRows} more stations");
                rowY += 20f;
            }

            int shownEnemies = 0;
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || !enemy.Alive) continue;
                if (shownEnemies >= enemyRows) break;
                GUI.Label(new Rect(x + 8f, rowY, width - 16f, 18f), $"{enemy.name} HP {Mathf.CeilToInt(enemy.Health)}/{enemy.maxHealth}");
                rowY += 18f;
                shownEnemies++;
            }
            int remainingEnemies = CountLivingEnemies() - shownEnemies;
            if (remainingEnemies > 0)
                GUI.Label(new Rect(x + 8f, rowY, width - 16f, 18f), $"+ {remainingEnemies} more enemies");
            else if (shownEnemies == 0)
                GUI.Label(new Rect(x + 8f, rowY, width - 16f, 18f), statusMessage);
        }
    }
}
