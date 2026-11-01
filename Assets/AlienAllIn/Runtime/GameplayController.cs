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
        [Header("Prototype balance (seconds and world units)")]
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
        public StagePhase Phase => phase;
        public bool Paused => paused;
        public int Cash => cash;
        public int Trampolines => trampolines;
        public int DefeatedEnemies => defeatedEnemies;
        public float Elapsed => elapsed;
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
            duck.transform.position = duckStart;
            seal.transform.position = sealStart;
            duck.ResetActor(); seal.ResetActor();
            foreach (var activity in activities) activity.ResetActivity();
            foreach (var pickup in pickups) pickup.ResetPickup();
            cash = trampolines = defeatedEnemies = spawnIndex = 0;
            elapsed = 0; nextSpawn = firstEnemyDelay;
            trampolineReady = paused = false;
            duckDestination = null;
            selected = duck;
            phase = StagePhase.Placement;
            message = "Select a character, then a cell. Press Start when ready.";
            RefreshBoard();
            RefreshPresentation(0);
        }

        public void StartShift()
        {
            RefreshBoard();
            if (phase != StagePhase.Placement || !duck.IsOnBoard || !seal.IsOnBoard || duck.CurrentCell == seal.CurrentCell)
            { message = "Place duck and seal in different cells inside the board."; return; }
            phase = StagePhase.Running;
            selected = null;
            message = "Complete both activities. Keep the duck safe.";
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

        // The same deterministic step is used by live play and scene integration tests.
        public void Advance(float dt)
        {
            if (phase == StagePhase.Running && !paused && dt > 0)
            {
                elapsed += dt;
                RefreshBoard();
                if (elapsed >= nextSpawn)
                {
                    if (enemies.FindAll(e => e && e.Alive).Count < maxEnemies) SpawnEnemy();
                    nextSpawn = elapsed + spawnInterval;
                }
                TickSeal(dt);
                TickDuck(dt);
                foreach (var enemy in enemies) if (enemy && enemy.Alive) TickEnemy(enemy, dt);
                RefreshBoard();
                foreach (var activity in activities)
                {
                    GridActor worker = activity.requiredKind == ActorKind.Duck ? duck : seal;
                    bool threatened = FindNearestEnemy(activity.transform.position, 1f) != null;
                    cash += activity.Advance(dt, worker, threatened);
                }
                if (!duck.Alive && !seal.Alive) { phase = StagePhase.Lost; message = "TEAM WIPED. Press R or RESTART to try a new placement."; }
                else
                {
                    bool complete = activities.Length > 0;
                    foreach (var activity in activities) complete &= activity.Complete;
                    if (complete) { phase = StagePhase.Won; message = "SHIFT COMPLETE! Both activities finished. Press R to play again."; }
                }
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
            seal.ShowAttack(range);
            foreach (var enemy in enemies)
            {
                if (!enemy || !enemy.Alive || Vector3.Distance(enemy.transform.position, seal.transform.position) > range) continue;
                float dealt = Mathf.Min(damage, enemy.Health);
                enemy.Hurt(damage); seal.AddCharge(dealt);
                if (!enemy.Alive) { defeatedEnemies++; continue; }
                Vector3 push = (enemy.transform.position - seal.transform.position).normalized * knockbackDistance;
                Vector3 proposed = enemy.transform.position + push;
                if (board.TryWorldToCell(proposed, out _)) enemy.transform.position = proposed;
            }
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
                local.x = Mathf.Clamp(local.x, .5f * board.cellSize, (board.width - .5f) * board.cellSize);
                local.y = Mathf.Clamp(local.y, .5f * board.cellSize, (board.height - .5f) * board.cellSize);
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
            enemy.AttackTimer -= dt;
            if (Vector3.Distance(enemy.transform.position, target.transform.position) <= enemy.attackRange)
            {
                enemy.State = ActorState.Defending;
                if (enemy.AttackTimer <= 0) { target.Hurt(enemy.damage); enemy.AttackTimer = enemy.attackInterval; }
            }
            else { enemy.State = ActorState.Moving; enemy.MoveTowards(target.transform.position, dt); }
        }

        public GridActor SpawnEnemy()
        {
            Vector2Int[] edges = { new Vector2Int(0, 5), new Vector2Int(6, board.height - 1), new Vector2Int(board.width - 1, 4), new Vector2Int(4, 0) };
            GridActor enemy = Instantiate(enemyPrefab, board.CellCenter(edges[spawnIndex++ % edges.Length]), Quaternion.identity, enemyContainer);
            enemy.name = $"Enemy {spawnIndex:00}";
            enemy.board = board; enemy.ResetActor(); enemies.Add(enemy);
            return enemy;
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
