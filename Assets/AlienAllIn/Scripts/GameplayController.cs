using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AlienAllIn
{
    public enum StagePhase { Placement, Running, Won, Lost }

    public sealed class GameplayController : MonoBehaviour
    {
        [Header("Scene references - all editable in the hierarchy")]
        public BoardGrid board;
        public Camera gameCamera;
        public GridActor duck;
        public GridActor seal;
        public GridActor enemyPrefab;
        public Transform enemyContainer;
        public ActivityStation[] activities;
        public FieldPickup[] pickups;
        public GameplayHUD hud;
        [Header("One-shot run")]
        [Min(60)] public float runDuration = 300;
        [Min(1)] public int openingBoardSize = 3;
        [Min(1)] public int middleBoardSize = 5;
        [Min(1)] public int lateBoardSize = 7;
        [Min(0.1f)] public float lateSpawnInterval = 2.75f;
        [Min(0.1f)] public float cameraScaleSpeed = 2.5f;
        [Min(0.1f)] public float maxBoardCameraSize = 5.65f;
        [Header("Combat balance (seconds and world units)")]
        [Min(.1f)] public float firstEnemyDelay = 7;
        [Min(.1f)] public float spawnInterval = 4.5f;
        [Min(1)] public int maxEnemies = 8;
        [Min(.1f)] public float duckFearRadius = 1.8f;
        [Min(.1f)] public float duckSafeRadius = 2.6f;
        [Min(0)] public float knockbackDistance = .45f;
        [Header("Live state")]
        [SerializeField] StagePhase phase;
        [SerializeField] bool paused;
        [SerializeField] int cash;
        [SerializeField] int trampolines;
        [SerializeField] int defeatedEnemies;
        [SerializeField] float elapsed;
        [SerializeField] GridActor selected;
        [SerializeField] string message;
        readonly List<GridActor> enemies = new List<GridActor>();
        readonly List<GridActor> actors = new List<GridActor>();
        Vector3 duckStart, sealStart;
        Vector3? duckDestination;
        float nextSpawn;
        bool initialized, trampolineReady;
        int spawnIndex;
        int nextSpawnEdge;
        public StagePhase Phase => phase;
        public bool Paused => paused;
        public int Cash => cash;
        public int Trampolines => trampolines;
        public int DefeatedEnemies => defeatedEnemies;
        public float Elapsed => elapsed;
        public float TimeRemaining => Mathf.Max(0, runDuration - elapsed);
        public float StageProgress => Mathf.Clamp01(elapsed / Mathf.Max(.01f, runDuration));
        public int ActiveBoardWidth => board ? board.ActiveWidth : 0;
        public int ActiveBoardHeight => board ? board.ActiveHeight : 0;
        public GridActor Selected => selected;
        public IReadOnlyList<GridActor> Enemies => enemies;
        public string Message => message;

        void Awake() => ResetStage();

        public void ResetStage()
        {
            if (!initialized)
            {
                duckStart = duck.transform.position;
                sealStart = seal.transform.position;
                initialized = true;
            }
            foreach (var enemy in enemies)
                if (enemy) { if (Application.isPlaying) Destroy(enemy.gameObject); else DestroyImmediate(enemy.gameObject); }
            enemies.Clear();
            int openingWidth = Mathf.Min(openingBoardSize, board.width);
            int openingHeight = Mathf.Min(openingBoardSize, board.height);
            board.SetActiveSize(openingWidth, openingHeight);
            duck.transform.position = duckStart;
            seal.transform.position = sealStart;
            duck.ResetActor(); seal.ResetActor();
            foreach (var activity in activities) activity.ResetActivity();
            foreach (var pickup in pickups) pickup.ResetPickup();
            cash = trampolines = defeatedEnemies = spawnIndex = nextSpawnEdge = 0;
            elapsed = 0; nextSpawn = firstEnemyDelay;
            trampolineReady = paused = false;
            duckDestination = null;
            selected = duck;
            phase = StagePhase.Placement;
            message = "ONE SHOT: survive until 00:00. The arena opens as time passes.";
            RefreshBoard();
            UpdateCameraFrame(0, true);
            RefreshPresentation(0);
        }

        public void StartShift()
        {
            RefreshBoard();
            if (phase != StagePhase.Placement || !duck.IsOnBoard || !seal.IsOnBoard || duck.CurrentCell == seal.CurrentCell)
            { message = "Place duck and seal in different cells inside the board."; return; }
            phase = StagePhase.Running;
            selected = null;
            message = "Survive the five-minute run. Activities earn bonus cash.";
        }

        public void TogglePause() { if (phase == StagePhase.Running) paused = !paused; }

        public void Select(GridActor actor)
        {
            if (phase == StagePhase.Won || phase == StagePhase.Lost || paused) return;
            if (!actor || !actor.Alive || actor.kind == ActorKind.Enemy) return;
            if (!actor.CanPickUp) { message = "Duck is fleeing or being hit. Wait until it is safe."; return; }
            selected = actor;
            message = actor == duck ? "Choose a destination." : "Choose a cell. Moving needs full Switch.";
        }

        public bool CommandCell(Vector2Int cell)
        {
            if (paused || !selected || !selected.Alive || !board.Contains(cell) || phase == StagePhase.Won || phase == StagePhase.Lost) return false;
            GridActor other = selected == duck ? seal : duck;
            if (other.Alive && other.CurrentCell == cell) { message = "That cell contains your teammate. Choose another cell."; return false; }
            if (phase == StagePhase.Placement)
            {
                selected.transform.position = board.CellCenter(cell);
                RefreshBoard();
                return true;
            }
            if (!selected.CanPickUp) { message = "Duck cannot be moved while fleeing or taking a hit."; return false; }
            if (selected == seal)
            {
                if (seal.SwitchCharge < 1) { message = "Seal needs 100% Switch. Deal damage to charge it."; return false; }
                seal.transform.position = board.CellCenter(cell);
                seal.SpendCharge();
                SealAttack(seal.attackRange * 1.5f, seal.damage * 2);
                message = "SWITCH! Seal moved and released a shockwave.";
            }
            else { duckDestination = board.CellCenter(cell); message = "Duck destination set. It resumes automatic work on arrival."; }
            RefreshBoard();
            return true;
        }

        public bool UseTrampoline()
        {
            if (phase != StagePhase.Running || paused || !seal.Alive || trampolines <= 0 || trampolineReady) return false;
            trampolines--; trampolineReady = true;
            message = "Trampoline armed: next seal slam has a larger radius and a slower recovery.";
            return true;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) ResetStage();
                if (keyboard.spaceKey.wasPressedThisFrame) { if (phase == StagePhase.Placement) StartShift(); else TogglePause(); }
                if (keyboard.escapeKey.wasPressedThisFrame) selected = null;
                if (keyboard.tKey.wasPressedThisFrame) UseTrampoline();
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject()))
            {
                Vector2 screen = mouse.position.ReadValue();
                if (gameCamera.pixelRect.Contains(screen))
                {
                    Vector3 world = gameCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -gameCamera.transform.position.z));
                    if (board.TryWorldToCell(world, out var cell))
                    {
                        RefreshBoard();
                        GridActor clicked = null;
                        foreach (var actor in board.GetCell(cell).Occupants)
                            if (actor.kind != ActorKind.Enemy) { clicked = actor; break; }
                        if (clicked) Select(clicked); else CommandCell(cell);
                    }
                }
            }
            Advance(Time.deltaTime);
        }

        public void Advance(float dt)
        {
            if (phase == StagePhase.Running && !paused && dt > 0)
            {
                float step = Mathf.Min(dt, Mathf.Max(0, runDuration - elapsed));
                elapsed += step;
                UpdateTimedBoardSize();
                RefreshBoard();
                if (elapsed < runDuration && elapsed >= nextSpawn)
                {
                    if (CountLivingEnemies() < maxEnemies) SpawnEnemy();
                    nextSpawn = elapsed + CurrentSpawnInterval();
                }
                TickSeal(step);
                TickDuck(step);
                foreach (var enemy in enemies) if (enemy && enemy.Alive) TickEnemy(enemy, step);
                RefreshBoard();
                foreach (var activity in activities)
                {
                    GridActor worker = activity.requiredKind == ActorKind.Duck ? duck : seal;
                    bool threatened = FindNearestEnemy(activity.transform.position, 1f) != null;
                    cash += activity.Advance(step, worker, threatened);
                }
                UpdateCameraFrame(step, false);
                if (!duck.Alive && !seal.Alive) { phase = StagePhase.Lost; message = "RUN OVER — the team was wiped. Restart for another one-shot."; }
                else if (elapsed >= runDuration) { phase = StagePhase.Won; message = "RUN COMPLETE — five minutes survived. Restart to play another run."; }
            }
            RefreshBoard();
            RefreshPresentation(paused || phase != StagePhase.Running ? 0 : dt);
        }

        void TickSeal(float dt)
        {
            if (!seal.Alive) return;
            seal.State = ActorState.Defending;
            seal.AttackTimer -= dt;
            float range = trampolineReady ? seal.attackRange * 1.55f : seal.attackRange;
            if (seal.AttackTimer > 0 || !FindNearestEnemy(seal.transform.position, range)) return;
            SealAttack(range, trampolineReady ? seal.damage * 2 : seal.damage);
            seal.AttackTimer = seal.attackInterval * (trampolineReady ? 1.7f : 1);
            trampolineReady = false;
        }

        void SealAttack(float range, float damage)
        {
            int livingBefore = CountLivingEnemies();
            CombatAndSpawnRules.ResolveSealAoe(seal, enemies, board, range, damage, knockbackDistance);
            defeatedEnemies += Mathf.Max(0, livingBefore - CountLivingEnemies());
        }

        void TickDuck(float dt)
        {
            if (!duck.Alive) return;
            float fear = duck.State == ActorState.Fleeing || duck.State == ActorState.Hiding ? duckSafeRadius : duckFearRadius;
            GridActor danger = FindNearestEnemy(duck.transform.position, fear);
            if (danger)
            {
                duckDestination = null;
                Vector3 away = seal.Alive ? (seal.transform.position - danger.transform.position).normalized : (duck.transform.position - danger.transform.position).normalized;
                Vector3 shelter = seal.Alive ? seal.transform.position + away * board.cellSize : duck.transform.position + away * board.cellSize;
                Vector3 local = board.transform.InverseTransformPoint(shelter);
                Vector2Int minCell = board.ActiveMinCell;
                local.x = Mathf.Clamp(local.x, (minCell.x + .5f) * board.cellSize, (minCell.x + board.ActiveWidth - .5f) * board.cellSize);
                local.y = Mathf.Clamp(local.y, (minCell.y + .5f) * board.cellSize, (minCell.y + board.ActiveHeight - .5f) * board.cellSize);
                shelter = board.transform.TransformPoint(local);
                duck.State = Vector3.Distance(duck.transform.position, shelter) < .15f ? ActorState.Hiding : ActorState.Fleeing;
                duck.MoveTowards(shelter, dt);
                return;
            }
            FieldPickup nearest = null;
            foreach (var pickup in pickups)
            {
                if (!pickup || pickup.Collected) continue;
                if (!nearest || Vector3.Distance(duck.transform.position, pickup.transform.position) < Vector3.Distance(duck.transform.position, nearest.transform.position)) nearest = pickup;
            }
            ActivityStation casino = System.Array.Find(activities, a => a.requiredKind == ActorKind.Duck && !a.Complete);
            Vector3 destination = duckDestination ?? (nearest ? nearest.transform.position : casino ? casino.transform.position : duck.transform.position);
            duck.State = nearest && !duckDestination.HasValue ? ActorState.Collecting : ActorState.Moving;
            // Move between orthogonal cell centers. Allies are passable, as in the GDD.
            Vector3 currentLocal = board.transform.InverseTransformPoint(duck.transform.position);
            Vector3 targetLocal = board.transform.InverseTransformPoint(destination);
            Vector3 step = Mathf.Abs(targetLocal.x - currentLocal.x) > .01f
                ? board.transform.TransformPoint(new Vector3(targetLocal.x, currentLocal.y, currentLocal.z)) : destination;
            duck.MoveTowards(step, dt);
            if (Vector3.Distance(duck.transform.position, destination) < .02f)
            {
                duckDestination = null;
                if (nearest && Vector3.Distance(duck.transform.position, nearest.transform.position) < .1f)
                { trampolines += nearest.Collect(); message = "Duck collected a trampoline. Press T to empower the next seal slam."; }
                duck.State = ActorState.Idle;
            }
        }

        void TickEnemy(GridActor enemy, float dt)
        {
            GridActor target = !duck.Alive ? seal : !seal.Alive ? duck :
                Vector3.Distance(enemy.transform.position, duck.transform.position) < Vector3.Distance(enemy.transform.position, seal.transform.position) ? duck : seal;
            if (!target.Alive) return;
            CombatAndSpawnRules.TickMelee(enemy, target.transform.position, dt, () => target.Hurt(enemy.damage));
        }

        public GridActor SpawnEnemy()
        {
            GridActor enemy = GridEnemySpawner.SpawnAtActiveEdges(
                board, enemyPrefab, enemyContainer, ref spawnIndex, ref nextSpawnEdge);
            if (enemy) enemies.Add(enemy);
            return enemy;
        }

        void UpdateTimedBoardSize()
        {
            int maxSize = Mathf.Min(board.width, board.height);
            int targetSize = StageProgress < .20f ? openingBoardSize :
                StageProgress < .50f ? middleBoardSize :
                StageProgress < .80f ? lateBoardSize : maxSize;
            targetSize = Mathf.Clamp(targetSize, 1, maxSize);
            if (board.SetActiveSize(targetSize, targetSize))
                message = $"ARENA EXPANDED — {board.ActiveWidth} × {board.ActiveHeight}.";
        }

        float CurrentSpawnInterval() => Mathf.Lerp(spawnInterval, lateSpawnInterval, StageProgress);

        int CountLivingEnemies()
        {
            int count = 0;
            foreach (GridActor enemy in enemies) if (enemy && enemy.Alive) count++;
            return count;
        }

        void UpdateCameraFrame(float dt, bool immediate)
        {
            if (!gameCamera || !board) return;
            float targetSize = maxBoardCameraSize * Mathf.Max(board.ActiveWidth, board.ActiveHeight) / Mathf.Max(board.width, board.height);
            Vector3 center = board.ActiveCenter;
            Vector3 targetPosition = new Vector3(center.x, center.y, gameCamera.transform.position.z);
            if (immediate)
            {
                gameCamera.orthographicSize = targetSize;
                gameCamera.transform.position = targetPosition;
                return;
            }

            gameCamera.orthographicSize = Mathf.MoveTowards(gameCamera.orthographicSize, targetSize, cameraScaleSpeed * dt);
            gameCamera.transform.position = Vector3.MoveTowards(gameCamera.transform.position, targetPosition, cameraScaleSpeed * dt);
        }

        GridActor FindNearestEnemy(Vector3 point, float radius)
        {
            GridActor nearest = null;
            float distance = radius;
            foreach (var enemy in enemies)
            {
                if (!enemy || !enemy.Alive) continue;
                float candidate = Vector3.Distance(point, enemy.transform.position);
                if (candidate <= distance) { distance = candidate; nearest = enemy; }
            }
            return nearest;
        }

        public void RefreshBoard()
        {
            actors.Clear(); actors.Add(duck); actors.Add(seal); actors.AddRange(enemies);
            board.RefreshOccupancy(actors);
        }

        void RefreshPresentation(float dt)
        {
            foreach (var actor in actors) if (actor && actor.Alive) actor.Present(dt, actor == selected);
            if (hud) hud.Refresh();
        }
    }
}
