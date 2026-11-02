using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    public sealed partial class MainRunGameplaySystems
    {
        sealed class EnemyRuntime
        {
            public EnemyPrototypeSettings settings;
            public bool hasEaten;
            public bool eating;
            public float specialClock;
            public float stunRemaining;
            public GridActor target;
        }

        sealed class ProjectileRuntime
        {
            public GameObject gameObject;
            public GridActor target;
            public float speed;
            public float damage;
            public GridActor owner;
            public bool ultimate;
        }

        sealed class RippleRuntime
        {
            public Vector3 origin;
            public float radius;
            public readonly HashSet<GridActor> hit = new HashSet<GridActor>();
        }

        readonly Dictionary<GridActor, EnemyRuntime> enemyRuntime = new Dictionary<GridActor, EnemyRuntime>();
        readonly Dictionary<GridActor, bool> pendingKillIsUltimate = new Dictionary<GridActor, bool>();
        readonly HashSet<GridActor> awardedKills = new HashSet<GridActor>();
        readonly List<ProjectileRuntime> projectiles = new List<ProjectileRuntime>();
        readonly List<RippleRuntime> sealRipples = new List<RippleRuntime>();
        float enemySpawnClock;
        int enemySpawnIndex;
        int nextSpawnEdge;
        float sealAttackClock;
        float catAttackClock;
        float pufferLockClock, pufferRestClock;
        GridActor pufferTarget;
        Object pufferLastContact;
        int pufferBounces;
        public PufferAttackPhase PufferPhase { get; private set; }
        public GridActor PufferTarget => pufferTarget;
        public int PufferBounceCount => pufferBounces;
        [SerializeField] AttackVfx attackVfx;
        float sealTelegraphClock;
        bool sealAttackPending;
        Vector2 pufferDirection;
        float catLaserClock;
        int catShotsRemaining;
        const int CatUltimateShotCount = 5;

        [Header("Prototype Ultimate and combat movement")]
        [SerializeField, Min(.1f)] float ultimateChargePerDamage = 30f;
        [SerializeField, Min(.1f)] float sealUltimateDuration = 3f;

        // Shared by the selection preview; combat keeps using these same source values.
        public float GetAttackPreviewRadius(GridActor actor)
        {
            if (!actor || actor.kind == ActorKind.Duck || actor.kind == ActorKind.Seal || actor.kind == ActorKind.Cat) return 0f;
            return actor.attackRange;
        }

        public string GetAttackRangeDescription(GridActor actor)
        {
            if (!actor || actor.kind == ActorKind.Duck) return "No attack";
            if (actor.kind == ActorKind.Seal)
                return actor.IsUltimateActive ? "Ultimate: 5x5, corners excluded" : "Attack: 3x3 cells";
            if (actor.kind == ActorKind.Cat)
                return actor.IsUltimateActive ? "Ultimate: whole active board" : "Attack: 5x5 cells, corners excluded";
            string label = actor.kind == ActorKind.Pufferfish ? "Lock-on range" : actor.IsUltimateActive ? "Ultimate sweep reach" : "Attack range";
            return $"{label}: {GetAttackPreviewRadius(actor):0.0} units";
        }
        [SerializeField, Min(.01f)] float toasterProjectileRadius = .12f;
        [SerializeField, Range(.01f, .5f)] float roasterEatAtHealthFraction = .2f;
        [SerializeField, Min(.1f)] float roasterEatDurationSeconds = .75f;
        [SerializeField, Min(0f)] float roasterRetreatBuffer = .25f;

        void ResetCombatForNewRun()
        {
            ClearCombatRuntimeObjects();
            foreach (GridActor enemy in enemies)
                if (enemy) { if (Application.isPlaying) Destroy(enemy.gameObject); else DestroyImmediate(enemy.gameObject); }
            enemies.Clear();
            enemyRuntime.Clear();
            pendingKillIsUltimate.Clear();
            awardedKills.Clear();
            projectiles.Clear();
            sealRipples.Clear();
            enemySpawnClock = Mathf.Max(.1f, firstEnemyDelaySeconds);
            enemySpawnIndex = nextSpawnEdge = 0;
            sealAttackClock = catAttackClock = 0f;
            ResetPufferCycle();
            sealAttackPending = false; sealTelegraphClock = 0;
            if (attackVfx) attackVfx.Clear();
            pufferDirection = Vector2.zero;
            catLaserClock = 0f;
            catShotsRemaining = 0;
        }

        void TickCombat(float dt)
        {
            AdvanceEnemySpawner(dt);
            TickEnemyActors(dt);
            TickTeamCombat(dt);
            TickProjectiles(dt);
            TickSealRipples(dt);
            TickPufferMovement(dt);
            CleanupCombatRuntimeObjects();
        }

        void AdvanceEnemySpawner(float dt)
        {
            if (DebugStopEnemySpawns) return;
            enemySpawnClock -= dt;
            if (enemySpawnClock > 0f) return;
            enemySpawnClock = Mathf.Max(.1f, enemySpawnIntervalSeconds);
            int aliveCount = 0;
            foreach (GridActor existingEnemy in enemies) if (existingEnemy && existingEnemy.Alive) aliveCount++;
            if (aliveCount >= Mathf.Max(1, maxEnemies)) return;

            EnemyPrototypeSettings settings = ChooseEnemySettings();
            SpawnEnemy(settings, false);
        }

        GridActor SpawnEnemy(EnemyPrototypeSettings settings, bool allowOpenCellFallback)
        {
            if (settings == null) return null;
            blockedSpawnCells.Clear();
            foreach (ActivityStation station in stations) if (station) blockedSpawnCells.Add(station.Cell);
            foreach (FieldPickup pickup in fieldItems)
                if (pickup && board.TryWorldToCell(pickup.transform.position, out Vector2Int occupied)) blockedSpawnCells.Add(occupied);
            GridActor enemy = GridEnemySpawner.SpawnAtActiveEdges(board, settings.prefab ? settings.prefab : enemyPrefab, enemyContainer,
                ref enemySpawnIndex, ref nextSpawnEdge, blockedSpawnCells);
            if (!enemy && allowOpenCellFallback && TryChooseOpenCell(false, out Vector2Int cell))
            {
                enemy = Instantiate(settings.prefab ? settings.prefab : enemyPrefab, board.CellCenter(cell), Quaternion.identity, enemyContainer);
                enemySpawnIndex++;
                enemy.board = board;
            }
            if (!enemy) return null;
            enemy.name = $"{settings.archetype} {enemySpawnIndex:00}";
            ConfigureEnemy(enemy, settings);
            enemies.Add(enemy);
            enemyRuntime[enemy] = new EnemyRuntime { settings = settings };
            RunDataChanged?.Invoke();
            return enemy;
        }

        EnemyPrototypeSettings ChooseEnemySettings()
        {
            if (enemySettings == null || enemySettings.Length == 0) return null;
            float total = 0f;
            foreach (EnemyPrototypeSettings setting in enemySettings)
                if (setting != null) total += Mathf.Max(0f, setting.spawnWeight);
            if (total <= 0f) return null;
            float value = Random.value * total;
            foreach (EnemyPrototypeSettings setting in enemySettings)
            {
                if (setting == null) continue;
                value -= Mathf.Max(0f, setting.spawnWeight);
                if (value <= 0f) return setting;
            }
            return enemySettings[enemySettings.Length - 1];
        }

        void ConfigureEnemy(GridActor enemy, EnemyPrototypeSettings settings)
        {
            enemy.kind = ActorKind.Enemy;
            enemy.countsAsActiveRunMember = false;
            enemy.maxHealth = Mathf.Max(1f, settings.health);
            enemy.moveSpeed = Mathf.Max(.01f, settings.moveSpeed);
            enemy.damage = Mathf.Max(0f, settings.damage);
            enemy.attackRange = Mathf.Max(.1f, settings.attackRange);
            enemy.attackInterval = Mathf.Max(.05f, settings.attackInterval);
            enemy.damageForFullCharge = ultimateChargePerDamage;
            enemy.SetDamageReduction(settings.archetype == EnemyArchetype.PanShield
                ? settings.shieldedDamageReduction : 0f);
            enemy.ResetActor();
            // ResetActor clears runtime damage mitigation; apply the Pan's prototype block afterwards.
            enemy.SetDamageReduction(settings.archetype == EnemyArchetype.PanShield
                ? settings.shieldedDamageReduction : 0f);
            enemy.SetPresentationColor(settings.prefab ? Color.white : settings.displayColor);
            enemy.State = settings.archetype == EnemyArchetype.PanShield ? ActorState.Shielding : ActorState.Idle;
        }

        void TickEnemyActors(float dt)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                GridActor enemy = enemies[i];
                if (!enemy || !enemy.Alive || !enemy.gameObject.activeInHierarchy || !enemyRuntime.TryGetValue(enemy, out EnemyRuntime runtime)) continue;
                enemy.AdvanceRuntime(dt);
                if (DuckCaptured && enemy == duckCaptor) { enemy.State = ActorState.Idle; continue; }
                runtime.specialClock -= dt;
                runtime.stunRemaining = Mathf.Max(0f, runtime.stunRemaining - dt);
                enemy.AttackTimer = Mathf.Max(0f, enemy.AttackTimer - dt);
                if (runtime.stunRemaining > 0f) { enemy.State = ActorState.Stunned; continue; }
                switch (runtime.settings.archetype)
                {
                    case EnemyArchetype.ToasterRanged: TickToaster(enemy, runtime, dt); break;
                    case EnemyArchetype.PanShield: TickPan(enemy, runtime, dt); break;
                    default: TickRoaster(enemy, runtime, dt); break;
                }
            }
        }

        void TickRoaster(GridActor enemy, EnemyRuntime runtime, float dt)
        {
            if (!runtime.hasEaten && enemy.Health <= enemy.maxHealth * roasterEatAtHealthFraction)
            {
                GridActor threat = FindEnemyTarget(enemy.transform.position, false);
                runtime.target = threat;
                float escapeRange = threat ? threat.attackRange + roasterRetreatBuffer : 0f;
                if (threat && Vector3.Distance(enemy.transform.position, threat.transform.position) <= escapeRange)
                {
                    runtime.eating = false;
                    runtime.specialClock = 0f;
                    if (TryRetreatFromThreat(enemy, threat, dt)) return;
                }

                enemy.State = ActorState.Eating;
                if (!runtime.eating)
                {
                    runtime.eating = true;
                    runtime.specialClock = Mathf.Max(.1f, roasterEatDurationSeconds);
                    return;
                }

                if (runtime.specialClock <= 0f)
                {
                    enemy.Heal(Mathf.Max(0f, runtime.settings.specialValue));
                    runtime.hasEaten = true;
                    runtime.eating = false;
                    statusMessage = "Roaster ate once and recovered HP.";
                    RunDataChanged?.Invoke();
                }
                return;
            }

            runtime.eating = false;
            GridActor target = FindEnemyTarget(enemy.transform.position, false);
            runtime.target = target;
            if (!target) return;
            if (Vector3.Distance(enemy.transform.position, target.transform.position) <= enemy.attackRange)
            {
                enemy.State = ActorState.Defending;
                if (enemy.AttackTimer > 0f) return;
                enemy.AttackTimer = enemy.attackInterval;
                EnemyHitActor(enemy, target, enemy.damage);
                return;
            }
            enemy.State = ActorState.Moving;
            enemy.MoveTowards(target.transform.position, dt);
        }

        bool TryRetreatFromThreat(GridActor enemy, GridActor threat, float dt)
        {
            Vector2 away = (enemy.transform.position - threat.transform.position).normalized;
            if (away.sqrMagnitude < .01f) away = Vector2.right;
            float[] offsets = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, offsets[i]) * away;
                Vector3 destination = enemy.transform.position + (Vector3)(direction * enemy.moveSpeed * dt);
                if (!board.TryWorldToCell(destination, out _)) continue;
                enemy.State = ActorState.Fleeing;
                enemy.MoveTowards(destination, dt);
                return true;
            }
            return false;
        }

        void TickToaster(GridActor enemy, EnemyRuntime runtime, float dt)
        {
            GridActor target = FindEnemyTarget(enemy.transform.position, false);
            runtime.target = target;
            if (!target) return;
            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
            bool targetDuck = target.kind == ActorKind.Duck;
            float preferredRange = Mathf.Max(.3f, runtime.settings.specialValue);
            if (distance > (targetDuck ? .65f : enemy.attackRange))
            {
                enemy.State = ActorState.Moving;
                enemy.MoveTowards(target.transform.position, dt);
            }
            else if (!targetDuck && distance < preferredRange)
            {
                enemy.State = ActorState.Fleeing;
                Vector3 away = (enemy.transform.position - target.transform.position).normalized;
                Vector3 retreat = enemy.transform.position + away * enemy.moveSpeed;
                if (board.TryWorldToCell(retreat, out _)) enemy.MoveTowards(retreat, dt);
            }
            else enemy.State = ActorState.Idle;

            if (distance <= (targetDuck ? .65f : enemy.attackRange) && enemy.AttackTimer <= 0f)
            {
                enemy.AttackTimer = enemy.attackInterval;
                SpawnProjectile(enemy, target, runtime.settings);
            }
        }

        void TickPan(GridActor enemy, EnemyRuntime runtime, float dt)
        {
            GridActor target = FindEnemyTarget(enemy.transform.position, false);
            runtime.target = target;
            if (!target) return;
            float distance = Vector3.Distance(enemy.transform.position, target.transform.position);
            if (distance > enemy.attackRange)
            {
                enemy.State = ActorState.Moving;
                enemy.MoveTowards(target.transform.position, dt);
                return;
            }
            enemy.State = ActorState.Shielding;
            if (enemy.AttackTimer > 0f) return;
            enemy.AttackTimer = enemy.attackInterval;
            float radius = Mathf.Max(.25f, runtime.settings.specialValue);
            InterruptStationsInRadius(enemy.transform.position, radius);
            foreach (GridActor actor in runLoop.Survivors)
            {
                if (!actor || !actor.Alive || Vector3.Distance(actor.transform.position, enemy.transform.position) > radius) continue;
                EnemyHitActor(enemy, actor, enemy.damage);
                InterruptWorkerAt(actor);
            }
        }

        void InterruptStationsInRadius(Vector3 position, float radius)
        {
            foreach (ActivityStation station in stations)
            {
                if (!station || station.Complete || Vector3.Distance(station.transform.position, position) > radius) continue;
                station.Interrupt();
                statusMessage = $"{station.activityName} interrupted by Pan.";
                RunDataChanged?.Invoke();
            }
        }

        /*
         * Runtime combat remains explicit rather than using Rigidbody/Collider events,
         * matching the existing grid actors and edge spawner.
         */

        GridActor FindEnemyTarget(Vector3 from, bool duckOnly)
        {
            GridActor nearest = null;
            GridActor hiddenDuck = null;
            float distance = float.PositiveInfinity;
            foreach (GridActor actor in runLoop.Survivors)
            {
                if (!actor || !actor.Alive || (actor == duck && (DuckCaptured || duckGraceRemaining > 0)) || (duckOnly && actor.kind != ActorKind.Duck)) continue;
                if (!duckOnly && actor.kind == ActorKind.Duck && actor.State == ActorState.Hiding)
                { hiddenDuck = actor; continue; }
                float candidate = (actor.transform.position - from).sqrMagnitude;
                if (candidate >= distance) continue;
                nearest = actor;
                distance = candidate;
            }
            if (!duckOnly)
                foreach (ActivityStation station in stations)
                {
                    if (!station || station.Complete || !station.CurrentWorker) continue;
                    float candidate = (station.transform.position - from).sqrMagnitude;
                    if (candidate < distance && Vector3.Distance(station.transform.position, from) < .5f)
                    { nearest = station.CurrentWorker; distance = candidate; }
                }
            return nearest ? nearest : hiddenDuck;
        }

        bool IsEnemyTargetingDuck(GridActor enemy)
        {
            return enemy && enemyRuntime.TryGetValue(enemy, out EnemyRuntime runtime) && runtime.target == duck;
        }

        void SpawnProjectile(GridActor enemy, GridActor target, EnemyPrototypeSettings settings)
        {
            if (!target || !target.Alive) return;
            GameObject shot = new GameObject("Toaster projectile");
            shot.transform.SetParent(enemyContainer, false);
            shot.transform.position = enemy.transform.position;
            SpriteRenderer renderer = shot.AddComponent<SpriteRenderer>();
            renderer.sprite = projectileSprite ? projectileSprite : itemSprite;
            renderer.color = new Color(1f, .48f, .24f, 1f);
            renderer.sortingOrder = 6;
            shot.transform.localScale = Vector3.one * toasterProjectileRadius * 2f;
            projectiles.Add(new ProjectileRuntime { gameObject = shot, target = target, speed = settings.projectileSpeed, damage = settings.damage, owner = enemy });
        }

        void TickTeamCombat(float dt)
        {
            if (seal && seal.Alive && !manualDestinations.ContainsKey(seal)) TickSeal(dt);
            if (cat && cat.Alive && !manualDestinations.ContainsKey(cat)) TickCat(dt);
            if (pufferfish && pufferfish.Alive && !manualDestinations.ContainsKey(pufferfish)) TickPufferCombatState(dt);
        }

        void TickSeal(float dt)
        {
            if (seal.IsUltimateActive) return;
            sealAttackClock -= dt;
            if (sealAttackPending)
            {
                sealTelegraphClock -= dt;
                if (sealTelegraphClock > 0) return;
                sealAttackPending = false;
                if (seal.TryGetComponent(out AlienAnimationDriver animation)) animation.PlayAttack();
                seal.RefreshCell();
                foreach (var victim in enemies)
                {
                    if (!victim || !victim.Alive) continue;
                    victim.RefreshCell();
                    if (!SealGridRange.Contains(victim.CurrentCell - seal.CurrentCell)) continue;
                    float dealt = victim.Hurt(seal.EffectiveDamage);
                    RegisterEnemyHit(seal, victim, dealt, false);
                    Vector3 pushed = victim.transform.position + (victim.transform.position - seal.transform.position).normalized * sealKnockbackDistance;
                    if (victim.Alive && board.TryWorldToCell(pushed, out _)) { victim.transform.position = pushed; victim.RefreshCell(); }
                }
                return;
            }
            seal.RefreshCell(); bool inRange = false;
            foreach (var enemy in enemies)
            {
                if (!enemy || !enemy.Alive) continue; enemy.RefreshCell();
                if (SealGridRange.Contains(enemy.CurrentCell - seal.CurrentCell)) { inRange = true; break; }
            }
            if (!inRange || sealAttackClock > 0) return;
            sealAttackClock = seal.EffectiveAttackInterval;
            sealAttackPending = true; sealTelegraphClock = .16f;
            if (attackVfx) attackVfx.SealGrid(seal.transform.position, false, .25f);
        }

        GridActor FindCatNormalTarget()
        {
            cat.RefreshCell(); GridActor nearest=null; float best=float.PositiveInfinity;
            foreach(var enemy in enemies)
            {
                if(!enemy || !enemy.Alive) continue;
                enemy.RefreshCell();
                if(!board.Contains(enemy.CurrentCell) || !CatAttackRange.Contains(enemy.CurrentCell-cat.CurrentCell)) continue;
                float distance=(enemy.transform.position-cat.transform.position).sqrMagnitude;
                if(distance<best) {nearest=enemy;best=distance;}
            }
            return nearest;
        }

        void TickCat(float dt)
        {
            if (cat.IsUltimateActive) { TickCatLasers(dt); return; }
            catAttackClock -= dt;
            GridActor target = FindCatNormalTarget();
            if (!target || catAttackClock > 0f) return;
            catAttackClock = cat.EffectiveAttackInterval;
            if (cat.TryGetComponent(out AlienAnimationDriver animation)) animation.PlayAttack();
            if (attackVfx) attackVfx.CatLaser(cat.transform.position, target.transform.position);
            float dealt = target.Hurt(cat.EffectiveDamage);
            RegisterEnemyHit(cat, target, dealt, false);
            if (target.Alive && catStunSeconds > 0f && enemyRuntime.TryGetValue(target, out EnemyRuntime runtime))
                runtime.stunRemaining = Mathf.Max(runtime.stunRemaining, catStunSeconds);
            InterruptWorkerAt(target);
        }

        void ResetPufferCycle()
        {
            ResetPufferPresentation();
            PufferPhase = PufferAttackPhase.Stationary; pufferTarget = null;
            pufferLockClock = pufferRestClock = 0; pufferBounces = 0;
            pufferLastContact = null; pufferDirection = Vector2.zero;
        }

        void StopPuffer()
        {
            PufferPhase = PufferAttackPhase.Stationary; pufferTarget = null;
            pufferDirection = Vector2.zero; pufferLastContact = null;
            pufferRestClock = pufferfish.EffectiveAttackInterval;
            pufferfish.State = ActorState.Idle;
        }

        void TickPufferCombatState(float dt)
        {
            if (PufferPhase == PufferAttackPhase.Launched || PufferPhase == PufferAttackPhase.Bouncing) return;
            if (PufferPhase == PufferAttackPhase.Stationary)
            {
                pufferfish.State = ActorState.Idle;
                pufferRestClock -= dt;
                if (pufferRestClock > 0) return;
                pufferTarget = FindNearestEnemy(pufferfish.transform.position, pufferfish.attackRange);
                PufferPhase = PufferAttackPhase.Locked;
                pufferLockClock = pufferInflateSeconds; pufferBounces = 0;
            }
            pufferfish.State = ActorState.Inflating;
            pufferLockClock -= dt;
            if (pufferLockClock > 0) return;
            pufferDirection = pufferTarget && pufferTarget.Alive
                ? (Vector2)(pufferTarget.transform.position - pufferfish.transform.position).normalized
                : new Vector2(.8f, .6f);
            if (pufferDirection.sqrMagnitude < .01f) pufferDirection = Vector2.right;
            PufferPhase = PufferAttackPhase.Launched; pufferfish.State = ActorState.Moving;
            if (pufferfish.TryGetComponent(out AlienAnimationDriver animation)) animation.PlayAttack();
        }

        void TickPufferMovement(float dt)
        {
            if (dt <= 0) return;
            if (!pufferfish || !pufferfish.Alive || manualDestinations.ContainsKey(pufferfish) ||
                (PufferPhase != PufferAttackPhase.Launched && PufferPhase != PufferAttackPhase.Bouncing)) return;
            float speed = pufferFloatSpeed * (pufferfish.IsUltimateActive ? pufferUltimateSpeedMultiplier : 1f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(speed * Mathf.Min(dt, .25f) / .08f));
            float step = speed * Mathf.Min(dt, .25f) / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector3 current = pufferfish.transform.position;
                Vector3 proposed = current + (Vector3)(pufferDirection * step);
                Object contact = null; Vector2 normal = Vector2.zero;
                GridActor victim = null;
                if (!board.TryWorldToCell(proposed, out Vector2Int outside))
                {
                    Vector2Int min = board.ActiveMinCell;
                    bool xWall = outside.x < min.x || outside.x >= min.x + board.ActiveWidth;
                    bool yWall = outside.y < min.y || outside.y >= min.y + board.ActiveHeight;
                    if (xWall) pufferDirection.x = -pufferDirection.x;
                    if (yWall) pufferDirection.y = -pufferDirection.y;
                    normal = xWall ? new Vector2(-Mathf.Sign(pufferDirection.x), 0) : new Vector2(0, -Mathf.Sign(pufferDirection.y));
                    contact = board;
                }
                else
                {
                    foreach (var enemy in enemies)
                    {
                        if (!enemy || !enemy.Alive || Vector3.Distance(proposed, enemy.transform.position) > pufferCollisionRadius + .18f) continue;
                        if (enemy == pufferLastContact) continue;
                        if (Vector2.Dot(pufferDirection, current - enemy.transform.position) >= 0) continue;
                        contact = victim = enemy; normal = current - enemy.transform.position; break;
                    }
                    if (!contact) foreach (var station in stations)
                    {
                        if (!station || station == pufferLastContact || Vector3.Distance(proposed, station.transform.position) > pufferCollisionRadius + .2f) continue;
                        if (Vector2.Dot(pufferDirection, current - station.transform.position) >= 0) continue;
                        contact = station; normal = current - station.transform.position; break;
                    }
                    if (!contact) foreach (var ally in runLoop.Survivors)
                    {
                        if (!ally || ally == pufferfish || !ally.Alive || ally == pufferLastContact || Vector3.Distance(proposed, ally.transform.position) > pufferCollisionRadius + .2f) continue;
                        if (Vector2.Dot(pufferDirection, current - ally.transform.position) >= 0) continue;
                        contact = ally; normal = current - ally.transform.position; break;
                    }
                }
                if (contact)
                {
                    if (normal.sqrMagnitude < .001f) normal = -pufferDirection;
                    if (contact != board) pufferDirection = Vector2.Reflect(pufferDirection, normal.normalized).normalized;
                    pufferLastContact = contact; pufferBounces++;
                    PlayPufferCollisionSound();
                    if (victim)
                    {
                        float multiplier = pufferfish.IsUltimateActive ? pufferUltimateDamageMultiplier : 1;
                        float dealt = victim.Hurt(pufferfish.EffectiveDamage * pufferDamageMultiplier * multiplier);
                        RegisterEnemyHit(pufferfish, victim, dealt, pufferfish.IsUltimateActive);
                    }
                    PufferPhase = PufferAttackPhase.Bouncing;
                    proposed = current + (Vector3)(pufferDirection * step);
                }
                else if (pufferLastContact == board) pufferLastContact = null;
                else if (!pufferLastContact) pufferLastContact = null;
                else if (pufferLastContact is Component previous && Vector3.Distance(current, previous.transform.position) > pufferCollisionRadius + .32f) pufferLastContact = null;
                if (board.TryWorldToCell(proposed, out _)) pufferfish.transform.position = proposed;
            }
            pufferfish.RefreshCell();
        }

        void TickProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileRuntime projectile = projectiles[i];
                if (!projectile.gameObject || !projectile.target || !projectile.target.Alive)
                { if (projectile.gameObject) Destroy(projectile.gameObject); projectiles.RemoveAt(i); continue; }
                Transform shot = projectile.gameObject.transform;
                shot.position = Vector3.MoveTowards(shot.position, projectile.target.transform.position, projectile.speed * dt);
                if (Vector3.Distance(shot.position, projectile.target.transform.position) > toasterProjectileRadius + .1f) continue;
                EnemyHitActor(projectile.owner, projectile.target, projectile.damage);
                Destroy(projectile.gameObject);
                projectiles.RemoveAt(i);
            }
        }

        void TickSealRipples(float dt)
        {
            for (int i = sealRipples.Count - 1; i >= 0; i--)
            {
                RippleRuntime ripple = sealRipples[i];
                float previous = ripple.radius;
                ripple.radius = Mathf.Min(3 * board.cellSize, previous + sealRippleSpeed * dt);
                board.TryWorldToCell(ripple.origin, out var originCell);
                foreach (GridActor enemy in enemies)
                {
                    if (!enemy || !enemy.Alive || ripple.hit.Contains(enemy)) continue;
                    enemy.RefreshCell();
                    if (!SealGridRange.Contains(enemy.CurrentCell - originCell, true)) continue;
                    Vector2Int offset = enemy.CurrentCell - originCell;
                    float distance = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y)) * board.cellSize;
                    if (distance > ripple.radius || distance < previous) continue;
                    ripple.hit.Add(enemy);
                    float maximumRadius = 3 * board.cellSize;
                    float falloff = Mathf.Lerp(1f, .25f, Mathf.Clamp01(distance / Mathf.Max(.1f, maximumRadius)));
                    float dealt = enemy.Hurt(seal.EffectiveDamage * sealRippleDamageMultiplier * falloff);
                    RegisterEnemyHit(seal, enemy, dealt, true);
                    Vector3 push = (enemy.transform.position - ripple.origin).normalized * sealRippleKnockback * falloff;
                    Vector3 destination = enemy.transform.position + push;
                    if (board.TryWorldToCell(destination, out _)) enemy.transform.position = destination;
                    InterruptWorkerAt(enemy);
                }
                if (ripple.radius >= 3 * board.cellSize) sealRipples.RemoveAt(i);
            }
        }

        void TickCatLasers(float dt)
        {
            if (!cat.IsUltimateActive) return;
            catLaserClock -= dt;
            if (catLaserClock > 0f) return;
            if (catShotsRemaining <= 0)
            {
                cat.EndUltimate();
                catAttackClock = cat.EffectiveAttackInterval;
                return;
            }
            catShotsRemaining--;
            catLaserClock = catShotsRemaining > 0 ? catLaserHitIntervalSeconds : AttackVfx.CatShotSeconds;
            if (attackVfx) attackVfx.CatUltimateShot();
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || !enemy.Alive || !board.TryWorldToCell(enemy.transform.position,out _)) continue;
                float dealt = enemy.Hurt(catLaserDamage + cat.EffectiveDamage * .25f);
                RegisterEnemyHit(cat, enemy, dealt, true);
                InterruptWorkerAt(enemy);
            }
        }

        public bool ActivateUltimate(GridActor actor)
        {
            if (!runLoop || runLoop.State != RunState.Running || !actor || !actor.Alive || !actor.UltimateFull || actor.kind == ActorKind.Duck) return false;
            float duration = actor.kind == ActorKind.Seal ? sealUltimateDuration : catLaserDurationSeconds;
            if (actor.kind == ActorKind.Cat) duration = Mathf.Max(duration, catLaserHitIntervalSeconds * CatUltimateShotCount + 1f);
            if (actor.kind == ActorKind.Pufferfish) duration = pufferFloatSeconds;
            if (!actor.BeginUltimate(duration)) return false;
            if (actor.kind == ActorKind.Seal)
            {
                sealRipples.Add(new RippleRuntime { origin = seal.transform.position });
                if (attackVfx) attackVfx.SealGrid(seal.transform.position, true, .65f);
            }
            else if (actor.kind == ActorKind.Cat)
            {
                catLaserClock = 0f;
                catShotsRemaining = CatUltimateShotCount;
            }
            else if (actor.kind == ActorKind.Pufferfish)
            {
                ResetPufferCycle();
            }
            statusMessage = $"{actor.kind} Ultimate activated.";
            RunDataChanged?.Invoke();
            return true;
        }

        void RegisterEnemyHit(GridActor attacker, GridActor enemy, float dealt, bool ultimate)
        {
            if (!enemy || dealt <= 0f) return;
            if (attacker && attacker.kind != ActorKind.Duck)
                attacker.AddCharge(dealt);
            AddScore(pointsPerHit, "Hit");
            if (!enemy.Alive) pendingKillIsUltimate[enemy] = ultimate;
            InterruptWorkerAt(enemy);
        }

        void InterruptWorkerAt(GridActor actor)
        {
            if (!actor) return;
            bool duckOverdrive = actor.kind == ActorKind.Duck && actor.IsUltimateActive;
            foreach (ActivityStation station in stations)
            {
                if (!station) continue;
                if (station.CurrentWorker == actor || station.Cell == actor.CurrentCell)
                {
                    if (!duckOverdrive) station.Interrupt();
                    if (actor.kind == ActorKind.Duck && !duckOverdrive && actor.State != ActorState.Panic) actor.State = ActorState.Stunned;
                }
            }
        }

        void OnEnemyRemoved(GridActor enemy)
        {
            if (!enemy) return;
            if (enemy.Health <= 0f && !awardedKills.Contains(enemy))
            {
                awardedKills.Add(enemy);
                bool ultimate = pendingKillIsUltimate.TryGetValue(enemy, out bool byUltimate) && byUltimate;
                chips += Random.Range(1, 11);
                AwardKill(null, ultimate, false);
            }
            if (enemy == duckCaptor) ReleaseDuck();
            enemyRuntime.Remove(enemy);
            pendingKillIsUltimate.Remove(enemy);
        }

        void AwardKill(GridActor killer, bool byUltimate, bool ignored)
        {
            AddScore(byUltimate ? pointsPerUltimateKill : pointsPerNormalKill, byUltimate ? "Ultimate Kill" : "Kill");
            if (byUltimate && ultimateKillAlsoScoresNormalKill) AddScore(pointsPerNormalKill, "Ultimate Kill bonus");
        }

        void CleanupCombatRuntimeObjects()
        {
            // MainRunGameplaySystems.CleanupRuntimeObjects owns enemy-list removal and score attribution.
            for (int i = projectiles.Count - 1; i >= 0; i--)
                if (!projectiles[i].gameObject) projectiles.RemoveAt(i);
        }

        void ClearCombatRuntimeObjects()
        {
            foreach (ProjectileRuntime projectile in projectiles)
                if (projectile.gameObject) { if (Application.isPlaying) Destroy(projectile.gameObject); else DestroyImmediate(projectile.gameObject); }
            projectiles.Clear();
            sealRipples.Clear();
            if (attackVfx) attackVfx.Clear();
        }

        public void RegisterCrushKill(GridActor enemy)
        {
            if (!enemy) return;
            pendingKillIsUltimate[enemy] = false;
        }
    }
}
