using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    /// <summary>
    /// Main-scene run simulation. RunLoopController remains the only owner of the
    /// clock, pause state, stage progression, and run outcome.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class MainRunGameplaySystems : MonoBehaviour
    {
        [Header("Run and stage references")]
        [SerializeField] RunLoopController runLoop;
        [SerializeField] StageGridProgression stage;
        [SerializeField] BoardGrid board;
        [SerializeField] GridActor duck;
        [SerializeField] GridActor seal;
        [SerializeField] GridActor cat;
        [SerializeField] GridActor pufferfish;
        [SerializeField] GridActor enemyPrefab;
        [SerializeField] ActivityStation casinoPrefab;
        [SerializeField] Sprite itemSprite;
        [SerializeField] Sprite projectileSprite;
        [SerializeField] Transform enemyContainer;
        [SerializeField] Transform processContainer;
        [SerializeField] Transform pickupContainer;

        [Header("Prototype Process spawning")]
        [SerializeField, Min(1f)] float processSpawnIntervalSeconds = 60f;
        [SerializeField, Range(0f, 1f)] float processSpawnChance = .25f;
        [SerializeField] ActivityPrototypeSettings[] activitySettings =
        {
            new ActivityPrototypeSettings { kind = ActivityKind.Casino, owner = ActorKind.Duck, durationSeconds = 36f, cashTickSeconds = 1f, processScoreTickSeconds = 2f, markerColor = new Color(.92f, .67f, .28f, 1f) },
            new ActivityPrototypeSettings { kind = ActivityKind.Darts, owner = ActorKind.Duck, durationSeconds = 32f, cashTickSeconds = 1f, processScoreTickSeconds = 2f, markerColor = new Color(.45f, .78f, .98f, 1f) },
            new ActivityPrototypeSettings { kind = ActivityKind.JumpRope, owner = ActorKind.Duck, durationSeconds = 40f, cashTickSeconds = 1f, processScoreTickSeconds = 2f, markerColor = new Color(.63f, .93f, .64f, 1f) }
        };
        [SerializeField, Range(1f, 2f)] float duckProcessingMultiplier = 1.1f;
        [SerializeField, Range(0f, 1f)] float duckUltimateProcessBonus = .15f;
        [SerializeField, Min(0f)] float processInterruptionSeconds = 2f;
        [SerializeField] int cashPerTick = 1;
        [SerializeField, Min(0f)] float pointsPerProcessingTick = 5f;

        [Header("Prototype Duck behaviour")]
        [SerializeField, Min(0f), Tooltip("Duck resumes Casino work when no living enemy is within this many cells.")]
        float duckWorkSafetyRadiusCells = 2f;
        [SerializeField, Min(0f)] float duckPanicChargePerSecond = .04f;
        [SerializeField, Min(0f)] float duckProcessChargePerSecond = .025f;
        [SerializeField, Min(0f)] float duckItemCharge = .15f;
        [SerializeField, Min(.1f)] float duckUltimateDurationSeconds = 7f;
        [SerializeField, Min(1f)] float duckUltimateMoveMultiplier = 1.35f;

        [Header("Prototype item drops")]
        [SerializeField, Min(.1f)] float firstItemSpawnDelaySeconds = 8f;
        [SerializeField, Min(.1f)] float itemSpawnIntervalSeconds = 28f;
        [SerializeField, Min(1)] int maxFieldItems = 4;

        [Header("Prototype combat tuning")]
        [SerializeField, Min(0f)] float sealKnockbackDistance = .45f;
        [SerializeField, Min(.1f)] float sealRippleSpeed = 5f;
        [SerializeField, Min(0f)] float sealRippleDamageMultiplier = 2f;
        [SerializeField, Min(0f)] float sealRippleKnockback = 1.4f;
        [SerializeField, Min(0f)] float catStunSeconds = 1f;
        [SerializeField, Min(.1f)] float catLaserDurationSeconds = 4f;
        [SerializeField, Min(.1f)] float catLaserHitIntervalSeconds = .5f;
        [SerializeField, Min(0f)] float catLaserDamage = 1.5f;
        [SerializeField, Min(.1f)] float pufferInflateSeconds = .55f;
        [SerializeField, Min(.1f)] float pufferFloatSeconds = 3.5f;
        [SerializeField, Min(.1f)] float pufferFloatSpeed = 1.12f;
        [SerializeField, Min(0f)] float pufferCollisionRadius = .34f;
        [SerializeField, Min(0f)] float pufferDamageMultiplier = .5f;
        [SerializeField, Min(1f)] float pufferUltimateSpeedMultiplier = 1.6f;
        [SerializeField, Min(1f)] float pufferUltimateDamageMultiplier = 2f;

        [Header("Prototype enemy spawning")]
        [SerializeField, Min(.1f)] float firstEnemyDelaySeconds = 7f;
        [SerializeField, Min(.1f)] float enemySpawnIntervalSeconds = 4.5f;
        [SerializeField, Min(1)] int maxEnemies = 8;
        [SerializeField] EnemyPrototypeSettings[] enemySettings =
        {
            new EnemyPrototypeSettings { archetype = EnemyArchetype.RoasterMelee, spawnWeight = 1f, health = 10f, moveSpeed = .8f, damage = 3f, attackRange = .65f, attackInterval = 1.2f, specialValue = 5f, projectileSpeed = 5f, shieldedDamageReduction = .2f, displayColor = new Color(1f, .72f, .55f) },
            new EnemyPrototypeSettings { archetype = EnemyArchetype.ToasterRanged, spawnWeight = 1f, health = 10f, moveSpeed = .65f, damage = 2f, attackRange = 5f, attackInterval = 2.2f, specialValue = 3.1f, projectileSpeed = 5f, shieldedDamageReduction = .2f, displayColor = new Color(.79f, .72f, 1f) },
            new EnemyPrototypeSettings { archetype = EnemyArchetype.PanShield, spawnWeight = 1f, health = 10f, moveSpeed = .45f, damage = 3f, attackRange = .8f, attackInterval = 2.6f, specialValue = 1.2f, projectileSpeed = 5f, shieldedDamageReduction = .2f, displayColor = new Color(.77f, .85f, .88f) }
        };

        [Header("Prototype Score values (GDD points)")]
        [SerializeField, Min(0)] int pointsPerHit = 10;
        [SerializeField, Min(0)] int pointsPerProcessed = 100;
        [SerializeField, Min(0)] int pointsPerNormalKill = 50;
        [SerializeField, Min(0)] int pointsPerUltimateKill = 100;
        [SerializeField] bool ultimateKillAlsoScoresNormalKill;

        [Header("Coin ATK upgrades (cost doubles per fighter)")]
        [SerializeField, Min(1)] int damageUpgradeBaseCost = 10;
        [SerializeField, Min(.01f)] float damageUpgradeAmount = .1f;

        [Header("Live run data")]
        [UnityEngine.Serialization.FormerlySerializedAs("cash")]
        [SerializeField] int coins;
        [SerializeField] int score;
        [SerializeField] int processedStations;
        [SerializeField] string statusMessage = "No Process station yet.";

        readonly List<GridActor> enemies = new List<GridActor>();
        readonly List<ActivityStation> stations = new List<ActivityStation>();
        readonly List<FieldPickup> fieldItems = new List<FieldPickup>();
        readonly List<GridActor> boardActors = new List<GridActor>();
        readonly List<Vector2Int> validSpawnCells = new List<Vector2Int>(100);
        readonly HashSet<Vector2Int> blockedSpawnCells = new HashSet<Vector2Int>();
        float processSpawnClock;
        float itemSpawnClock;

        public RunLoopController RunLoop => runLoop;
        public BoardGrid Board => board;
        public GridActor Duck => duck;
        public GridActor Seal => seal;
        public GridActor Cat => cat;
        public GridActor Pufferfish => pufferfish;
        public int Cash => coins;
        public int Coins => coins;
        public int Score => score;
        public int ProcessedStations => processedStations;
        public string StatusMessage => statusMessage;
        public IReadOnlyList<GridActor> Enemies => enemies;
        public IReadOnlyList<ActivityStation> Stations => stations;
        public IReadOnlyList<FieldPickup> FieldItems => fieldItems;
        public event Action RunDataChanged;

        void OnEnable()
        {
            if (runLoop)
            {
                runLoop.RunRestarted += ResetForNewRun;
                runLoop.RunEnded += HandleRunEnded;
            }
        }

        void OnDisable()
        {
            if (runLoop)
            {
                runLoop.RunRestarted -= ResetForNewRun;
                runLoop.RunEnded -= HandleRunEnded;
            }
        }

        void HandleRunEnded(RunState result)
        {
            statusMessage = result == RunState.RunComplete ? "Run complete." : "Team defeated.";
            RunDataChanged?.Invoke();
        }

        void Awake()
        {
            if (!runLoop || !stage || !board || !duck || !seal || !cat || !pufferfish || !enemyPrefab || !casinoPrefab)
            {
                Debug.LogError("MainRunGameplaySystems requires the RunLoop, Stage, Board, four team members, and prototype prefabs.", this);
                enabled = false;
                return;
            }

            if (!enemyContainer) enemyContainer = transform;
            if (!processContainer) processContainer = transform;
            if (!pickupContainer) pickupContainer = processContainer;
            seal.damageForFullCharge = cat.damageForFullCharge = pufferfish.damageForFullCharge = ultimateChargePerDamage;
            seal.attackInterval = cat.attackInterval = 2f;
            ResetForNewRun();
        }

        void Start() => InitializePersistentCasino();

        void Update()
        {
            if (!enabled || !runLoop || runLoop.State != RunState.Running) return;
            float dt = runLoop.GameplayDeltaTime;
            if (dt <= 0f) return;

            AdvanceTeamRuntime(dt);
            AdvanceOrdersAndRescue(dt);
            if (runLoop.State != RunState.Running) return;
            RefreshBoardOccupancy();
            AdvanceProcessSpawn(dt);
            AdvanceItemSpawn(dt);
            TickCombat(dt);
            TickDuck(dt);
            AdvanceProcesses(dt);
            RefreshBoardOccupancy();
            CleanupRuntimeObjects();
            RefreshBoardOccupancy();
        }

        void AdvanceTeamRuntime(float dt)
        {
            foreach (GridActor actor in runLoop.Survivors)
                if (actor && actor.Alive) actor.AdvanceRuntime(dt);
        }

        void ResetForNewRun()
        {
            DebugStopEnemySpawns = DebugStopStationSpawns = false;
            ResetStationsForNewRun();
            ClearSpawned(fieldItems);
            coins = score = processedStations = 0;
            processSpawnClock = 0f;
            itemSpawnClock = Mathf.Max(.1f, firstItemSpawnDelaySeconds);
            statusMessage = "Run ready. Start with empty inventories.";
            ResetEconomyForNewRun();
            ResetCasinoForNewRun();
            ResetOrdersAndRescue();
            ResetCombatForNewRun();
            ReconcileDebugBoard();
            // A formation captured on a larger debug board must remain restartable on 5x5.
            foreach (GridActor actor in runLoop.Survivors)
                if (actor && actor.Alive && actor.IsOnBoard) actor.CaptureStartingPosition();
            RunDataChanged?.Invoke();
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

        void AdvanceProcessSpawn(float dt)
        {
            if (DebugStopStationSpawns) return;
            float interval = Mathf.Max(1f, processSpawnIntervalSeconds);
            processSpawnClock += dt;
            while (processSpawnClock >= interval)
            {
                processSpawnClock -= interval;
                if (UnityEngine.Random.value <= Mathf.Clamp01(processSpawnChance)) TrySpawnActivity();
                else statusMessage = "No activity this Process roll.";
            }
        }

        bool TrySpawnActivity()
        {
            return SpawnActivity(ChooseActivityPrototype());
        }

        bool SpawnActivity(ActivityPrototypeSettings settings)
        {
            if (settings == null)
            {
                statusMessage = "No configured Process activity.";
                return false;
            }

            if (settings.kind == ActivityKind.Casino)
            {
                statusMessage = "The Casino is permanent and cannot be spawned again.";
                RunDataChanged?.Invoke();
                return false;
            }

            if (!TryChooseOpenCell(true, out Vector2Int selectedCell))
            {
                statusMessage = "Process roll skipped: no valid cell; try next interval.";
                return false;
            }

            BoardCell boardCell = board.GetCell(selectedCell);
            foreach (GridActor actor in boardCell.Occupants)
            {
                if (!actor || !actor.Alive || actor.kind != ActorKind.Enemy) continue;
                RemoveEnemyByCrush(actor);
            }

            ActivityStation station = Instantiate(settings.prefab ? settings.prefab : casinoPrefab, board.CellCenter(selectedCell), Quaternion.identity, processContainer);
            station.name = $"{settings.kind.ToString().ToUpperInvariant()} {stations.Count + 1:00}";
            station.activityKind = settings.kind;
            station.activityName = ActivityLabel(settings.kind);
            station.requiredKind = ActorKind.Duck;
            station.board = board;
            station.duration = Mathf.Max(1f, settings.durationSeconds);
            station.cashTickSeconds = Mathf.Max(.05f, settings.cashTickSeconds);
            station.scoreTickSeconds = Mathf.Max(0f, settings.processScoreTickSeconds);
            station.allowUniversalDuckWorker = true;
            station.interruptionSeconds = Mathf.Max(0f, processInterruptionSeconds);
            station.workColor = settings.markerColor;
            station.ResetActivity();
            if (station.marker) station.marker.color = settings.markerColor;
            stations.Add(station);
            SuspendCasinoForStations();
            statusMessage = $"{station.activityName} spawned at {boardCell.Address}.";
            RunDataChanged?.Invoke();
            return true;
        }

        ActivityPrototypeSettings ChooseActivityPrototype()
        {
            if (activitySettings == null || activitySettings.Length == 0) return null;
            int validCount = 0;
            for (int i = 0; i < activitySettings.Length; i++)
                if (activitySettings[i] != null && activitySettings[i].kind != ActivityKind.Casino) validCount++;
            if (validCount == 0) return null;
            int chosen = UnityEngine.Random.Range(0, validCount);
            for (int i = 0; i < activitySettings.Length; i++)
            {
                if (activitySettings[i] == null || activitySettings[i].kind == ActivityKind.Casino) continue;
                if (chosen-- == 0) return activitySettings[i];
            }
            return null;
        }

        bool TryChooseOpenCell(bool allowEnemy, out Vector2Int cell)
        {
            validSpawnCells.Clear();
            foreach (Vector2Int candidate in stage.ProcessSpawnCandidates)
            {
                BoardCell boardCell = board.GetCell(candidate);
                if (!boardCell || HasStationAt(candidate) || HasItemAt(candidate)) continue;
                bool blocked = false;
                foreach (GridActor actor in boardCell.Occupants)
                {
                    if (!actor || !actor.Alive) continue;
                    if (actor.kind != ActorKind.Enemy || !allowEnemy) { blocked = true; break; }
                }
                if (!blocked) validSpawnCells.Add(candidate);
            }

            if (validSpawnCells.Count == 0)
            {
                cell = default;
                return false;
            }

            cell = validSpawnCells[UnityEngine.Random.Range(0, validSpawnCells.Count)];
            return true;
        }

        bool HasStationAt(Vector2Int cell)
        {
            foreach (ActivityStation station in stations)
                if (station && !station.Complete && station.Cell == cell) return true;
            return false;
        }

        bool HasItemAt(Vector2Int cell)
        {
            foreach (FieldPickup item in fieldItems)
            {
                if (!item || item.Collected) continue;
                if (board.TryWorldToCell(item.transform.position, out Vector2Int itemCell) && itemCell == cell) return true;
            }
            return false;
        }

        void AdvanceItemSpawn(float dt)
        {
            if (fieldItems.Count >= Mathf.Max(1, maxFieldItems)) return;
            itemSpawnClock -= dt;
            if (itemSpawnClock > 0f) return;

            itemSpawnClock = Mathf.Max(.1f, itemSpawnIntervalSeconds);
            if (!TryChooseOpenCell(false, out Vector2Int cell))
            {
                statusMessage = "Item drop skipped: no open cell.";
                return;
            }

            FieldItemKind itemKind = (FieldItemKind)UnityEngine.Random.Range(0, 3);
            var itemObject = new GameObject($"Item {itemKind}");
            itemObject.transform.SetParent(pickupContainer, false);
            itemObject.transform.position = board.CellCenter(cell);
            var renderer = itemObject.AddComponent<SpriteRenderer>();
            renderer.sprite = itemSprite;
            renderer.color = ItemColor(itemKind);
            renderer.sortingOrder = 3;
            itemObject.transform.localScale = Vector3.one * .36f;
            var pickup = itemObject.AddComponent<FieldPickup>();
            pickup.itemKind = itemKind;
            pickup.quantity = 1;
            pickup.trampolineCount = 1;
            fieldItems.Add(pickup);
            statusMessage = $"{itemKind} drop appeared at {board.GetCell(cell).Address}.";
            RunDataChanged?.Invoke();
        }

        void TickDuck(float dt)
        {
            if (!duck || !duck.Alive) return;
            bool stationsPending = CasinoNeedsStationWork;
            if (stationsPending) SuspendCasinoForStations();
            if (DuckCaptured || manualDestinations.ContainsKey(duck)) return;

            duck.RefreshCell();
            if (stationsPending && duck.State == ActorState.Panic && !IsEnemyNearDuckForWork()) duck.State = ActorState.Idle;
            if (casinoMachine && casinoWaitingForStake && !stationsPending && duck.IsOnBoard && duck.CurrentCell == casinoMachine.Cell)
            {
                duck.State = IsEnemyNearDuckForWork() ? ActorState.Panic : ActorState.Idle;
                return;
            }
            bool onCasino = casinoMachine && casinoActive && !stationsPending && duck.IsOnBoard && duck.CurrentCell == casinoMachine.Cell;
            bool returningToCasino = casinoActive && casinoReturnAfterStations && !stationsPending;
            if ((onCasino || returningToCasino) && (!casinoWorker || casinoWorker == duck))
            {
                if (IsEnemyNearDuckForWork())
                {
                    duck.State = ActorState.Panic;
                    duck.AddUltimateCharge(duckPanicChargePerSecond * dt);
                    return;
                }
                bool recovering = duck.State == ActorState.Panic;
                casinoWorker = duck;
                if (!IsStandingAt(duck, casinoMachine))
                {
                    duck.State = ActorState.Moving;
                    MoveActorAlongGrid(duck, casinoMachine.transform.position, dt, 1f);
                    return;
                }
                duck.State = ActorState.Working;
                casinoReturnAfterStations = false;
                if (recovering) SetStatusFromUI("Duck is safe and resumed Casino work.");
                return;
            }
            if (casinoWorker == duck) casinoWorker = null;

            if (duck.UltimateFull && !duck.IsUltimateActive)
            {
                duck.BeginDuckOverdrive(duckUltimateDurationSeconds);
                statusMessage = "Duck triggered Auto Overdrive.";
            }

            bool targeted = IsDuckTargetedByEnemy();
            if (duck.IsUltimateActive)
            {
                TickDuckMovementAndWork(dt);
                return;
            }

            if (duck.State == ActorState.Hiding)
            {
                if (targeted) return;
                duck.State = ActorState.Idle;
            }
            else if (targeted)
            {
                duck.State = ActorState.Panic;
                duck.AddUltimateCharge(duckPanicChargePerSecond * dt);
                return;
            }
            else if (duck.State == ActorState.Panic)
            {
                // Panic persists until the player places Duck behind another team member.
                duck.AddUltimateCharge(duckPanicChargePerSecond * dt);
                return;
            }

            TickDuckMovementAndWork(dt);
        }

        bool IsEnemyNearDuckForWork()
        {
            Vector3 origin = board.transform.InverseTransformPoint(duck.transform.position);
            float radius = duckWorkSafetyRadiusCells * board.cellSize;
            foreach (var enemy in enemies)
            {
                if (!enemy || !enemy.Alive || !enemy.gameObject.activeInHierarchy) continue;
                Vector3 position = board.transform.InverseTransformPoint(enemy.transform.position);
                if ((position - origin).sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        void TickDuckMovementAndWork(float dt)
        {
            ActivityStation station = FindNearestIncompleteStation(duck.transform.position);
            FieldPickup item = station ? null : FindNearestAvailableItem(duck.transform.position);
            if (item)
            {
                duck.State = ActorState.Collecting;
                MoveActorAlongGrid(duck, item.transform.position, dt, duck.IsUltimateActive ? duckUltimateMoveMultiplier : 1f);
                if (Vector3.Distance(duck.transform.position, item.transform.position) <= .2f * board.cellSize)
                {
                    if (item.TryCollect(out FieldItemKind kind, out int quantity))
                    {
                        AddItem(kind, quantity);
                        duck.AddUltimateCharge(duckItemCharge);
                        statusMessage = $"Duck collected {kind} +{quantity}.";
                        RunDataChanged?.Invoke();
                    }
                }
                return;
            }

            if (!station)
            {
                duck.State = duck.IsUltimateActive ? ActorState.UsingUltimate : ActorState.Idle;
                return;
            }

            if (Vector3.Distance(duck.transform.position, station.transform.position) > .15f * board.cellSize)
            {
                duck.State = duck.IsUltimateActive ? ActorState.UsingUltimate : ActorState.Moving;
                MoveActorAlongGrid(duck, station.transform.position, dt, duck.IsUltimateActive ? duckUltimateMoveMultiplier : 1f);
            }
            else
            {
                duck.State = duck.IsUltimateActive ? ActorState.UsingUltimate : ActorState.Working;
            }
        }

        void MoveActorAlongGrid(GridActor actor, Vector3 destination, float dt, float speedMultiplier)
        {
            Vector3 currentLocal = board.transform.InverseTransformPoint(actor.transform.position);
            Vector3 targetLocal = board.transform.InverseTransformPoint(destination);
            Vector3 target = Mathf.Abs(targetLocal.x - currentLocal.x) > .03f
                ? board.transform.TransformPoint(new Vector3(targetLocal.x, currentLocal.y, currentLocal.z))
                : board.transform.TransformPoint(new Vector3(currentLocal.x, targetLocal.y, targetLocal.z));
            actor.MoveTowards(target, dt * Mathf.Max(.01f, speedMultiplier));
            if (Vector3.Distance(actor.transform.position, destination) <= .03f * board.cellSize)
                actor.MoveTowards(destination, dt * Mathf.Max(.01f, speedMultiplier));
        }

        FieldPickup FindNearestAvailableItem(Vector3 position)
        {
            FieldPickup nearest = null;
            float bestDistance = float.PositiveInfinity;
            foreach (FieldPickup pickup in fieldItems)
            {
                if (!pickup || pickup.Collected) continue;
                float distance = (pickup.transform.position - position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = pickup;
            }
            return nearest;
        }

        ActivityStation FindNearestIncompleteStation(Vector3 position)
        {
            ActivityStation nearest = null;
            float bestDistance = float.PositiveInfinity;
            foreach (ActivityStation station in stations)
            {
                if (!station || station == casinoMachine || station.Complete) continue;
                float distance = (station.transform.position - position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = station;
            }
            return nearest;
        }

        GridActor FindStationWorker(ActivityStation station)
        {
            if (!station) return null;
            if (station == casinoMachine)
                return casinoActive && !CasinoNeedsStationWork && !casinoFaulted && !(casinoWorker == duck && DuckCaptured) && IsStandingAt(casinoWorker, station) &&
                    casinoWorker.State != ActorState.Panic ? casinoWorker : null;
            if (!DuckCaptured && IsStandingAt(duck, station) && duck.State != ActorState.Panic)
                return duck;
            return null;
        }

        bool IsStandingAt(GridActor actor, ActivityStation station) =>
            actor && actor.Alive && actor.IsOnBoard && actor.CurrentCell == station.Cell &&
            Vector3.Distance(actor.transform.position, station.transform.position) <= .15f * board.cellSize;

        float GetProcessingRate(GridActor worker)
        {
            if (!worker || worker.kind != ActorKind.Duck) return 1f;
            float rate = Mathf.Max(1f, duckProcessingMultiplier);
            if (duck.IsUltimateActive) rate += Mathf.Max(0f, duckUltimateProcessBonus);
            return rate;
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

                if (station == casinoMachine)
                {
                    AdvanceCasino(dt);
                    continue;
                }

                GridActor worker = FindStationWorker(station);
                if (!worker) continue;
                int scoreTicks;
                int cashTicks = station.Advance(dt, worker, false, GetProcessingRate(worker), station.scoreTickSeconds, out scoreTicks);
                if (cashTicks > 0) AddCash(cashTicks * Mathf.Max(0, cashPerTick), $"Coins +{cashTicks * cashPerTick}");
                if (scoreTicks > 0) AddScore(Mathf.RoundToInt(scoreTicks * Mathf.Max(0f, pointsPerProcessingTick)), "Processing");
                if (worker == duck) duck.AddUltimateCharge(duckProcessChargePerSecond * dt);
                if (!station.Complete) continue;

                processedStations++;
                AddScore(pointsPerProcessed, $"{station.activityName} Processed");
                statusMessage = $"{station.activityName} solved and removed.";
                if (worker == duck && !duck.IsUltimateActive) duck.State = ActorState.Idle;
                Destroy(station.gameObject);
                stations.RemoveAt(i);
                RunDataChanged?.Invoke();
            }
        }

        void RemoveEnemyByCrush(GridActor enemy)
        {
            if (!enemy || !enemy.Alive) return;
            enemy.Eliminate();
            RegisterCrushKill(enemy);
            statusMessage = "Enemy was crushed by a Process station.";
        }

        void CleanupRuntimeObjects()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                GridActor enemy = enemies[i];
                if (enemy && enemy.Alive && enemy.gameObject.activeInHierarchy) continue;
                if (enemy) Destroy(enemy.gameObject);
                OnEnemyRemoved(enemy);
                enemies.RemoveAt(i);
            }

            for (int i = fieldItems.Count - 1; i >= 0; i--)
            {
                FieldPickup pickup = fieldItems[i];
                if (pickup && !pickup.Collected && pickup.gameObject.activeInHierarchy) continue;
                if (pickup) Destroy(pickup.gameObject);
                fieldItems.RemoveAt(i);
            }
        }

        void RefreshBoardOccupancy()
        {
            boardActors.Clear();
            foreach (GridActor actor in runLoop.Survivors) if (actor) boardActors.Add(actor);
            foreach (GridActor enemy in enemies) if (enemy) boardActors.Add(enemy);
            board.RefreshOccupancy(boardActors);
        }

        GridActor GetActor(ActorKind kind)
        {
            switch (kind)
            {
                case ActorKind.Seal: return seal;
                case ActorKind.Cat: return cat;
                case ActorKind.Pufferfish: return pufferfish;
                case ActorKind.Duck: return duck;
                default: return null;
            }
        }

        public bool IsCellReserved(Vector2Int cell)
        {
            if (HasStationAt(cell) || HasItemAt(cell)) return true;
            BoardCell target = board.GetCell(cell);
            if (!target) return true;
            foreach (GridActor actor in target.Occupants)
                if (actor && actor.Alive) return true;
            return false;
        }

        public bool IsStationCell(Vector2Int cell)
        {
            foreach (ActivityStation station in stations)
                if (station && station.Cell == cell) return true;
            return false;
        }

        bool IsDuckTargetedByEnemy()
        {
            foreach (GridActor enemy in enemies)
                if (enemy && enemy.Alive && IsEnemyTargetingDuck(enemy)) return true;
            return false;
        }

        bool IsBehindLivingAlien(Vector3 position, GridActor duckActor)
        {
            GridActor ally = null;
            float allyDistance = float.PositiveInfinity;
            foreach (GridActor actor in runLoop.Survivors)
            {
                if (!actor || actor == duckActor || !actor.Alive || actor.kind == ActorKind.Duck) continue;
                float distance = (actor.transform.position - position).sqrMagnitude;
                if (distance >= allyDistance) continue;
                allyDistance = distance;
                ally = actor;
            }
            if (!ally || allyDistance > 1.15f * 1.15f) return false;

            GridActor threat = FindNearestEnemy(position, float.PositiveInfinity);
            if (!threat) return true;
            Vector2 behind = (Vector2)(position - ally.transform.position);
            Vector2 fromThreat = (Vector2)(ally.transform.position - threat.transform.position);
            return Vector2.Dot(behind, fromThreat) > 0f &&
                   Vector3.Distance(position, threat.transform.position) >= Vector3.Distance(ally.transform.position, threat.transform.position);
        }

        GridActor FindNearestEnemy(Vector3 position, float maxDistance)
        {
            GridActor nearest = null;
            float bestSqr = maxDistance * maxDistance;
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || !enemy.Alive) continue;
                float distance = (enemy.transform.position - position).sqrMagnitude;
                if (distance >= bestSqr) continue;
                bestSqr = distance;
                nearest = enemy;
            }
            return nearest;
        }

        static string ActivityLabel(ActivityKind kind)
        {
            switch (kind)
            {
                case ActivityKind.Darts: return "DARTS";
                case ActivityKind.JumpRope: return "JUMP ROPE";
                default: return "CASINO";
            }
        }

        static Color ItemColor(FieldItemKind kind)
        {
            switch (kind)
            {
                case FieldItemKind.AirPump: return new Color(.42f, .82f, 1f, 1f);
                case FieldItemKind.LaserGun: return new Color(.85f, .63f, 1f, 1f);
                default: return new Color(1f, .82f, .34f, 1f);
            }
        }

        void OnDestroy()
        {
            if (!Application.isPlaying) return;
            ClearSpawned(stations);
            ClearSpawned(fieldItems);
            ClearCombatRuntimeObjects();
        }
    }
}
