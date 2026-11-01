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
        readonly Dictionary<GridActor, float> pufferHitClocks = new Dictionary<GridActor, float>();
        float enemySpawnClock;
        int enemySpawnIndex;
        int nextSpawnEdge;
        float sealAttackClock;
        float catAttackClock;
        float pufferInflateClock;
        float pufferFloatClock;
        Vector2 pufferDirection;
        float catLaserClock;

        [Header("Prototype Ultimate and combat movement")]
        [SerializeField, Min(.1f)] float ultimateChargePerDamage = 30f;
        [SerializeField, Min(.1f)] float sealUltimateDuration = 3f;
        [SerializeField, Min(.1f)] float catLaserReach = 10f;
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
            pufferHitClocks.Clear();
            enemySpawnClock = Mathf.Max(.1f, firstEnemyDelaySeconds);
            enemySpawnIndex = nextSpawnEdge = 0;
            sealAttackClock = catAttackClock = 0f;
            pufferInflateClock = pufferFloatClock = 0f;
            pufferDirection = Vector2.zero;
            catLaserClock = 0f;
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
            enemySpawnClock -= dt;
            if (enemySpawnClock > 0f) return;
            enemySpawnClock = Mathf.Max(.1f, enemySpawnIntervalSeconds);
            int aliveCount = 0;
            foreach (GridActor existingEnemy in enemies) if (existingEnemy && existingEnemy.Alive) aliveCount++;
            if (aliveCount >= Mathf.Max(1, maxEnemies)) return;

            blockedSpawnCells.Clear();
            foreach (ActivityStation station in stations) if (station) blockedSpawnCells.Add(station.Cell);
            foreach (FieldPickup pickup in fieldItems)
                if (pickup && board.TryWorldToCell(pickup.transform.position, out Vector2Int pickupCell)) blockedSpawnCells.Add(pickupCell);

            GridActor enemy = GridEnemySpawner.SpawnAtActiveEdges(board, enemyPrefab, enemyContainer,
                ref enemySpawnIndex, ref nextSpawnEdge, blockedSpawnCells);
            if (!enemy) return;
            EnemyPrototypeSettings settings = ChooseEnemySettings();
            if (settings == null) { Destroy(enemy.gameObject); return; }
            enemy.name = $"{settings.archetype} {enemySpawnIndex:00}";
            ConfigureEnemy(enemy, settings);
            enemies.Add(enemy);
            enemyRuntime[enemy] = new EnemyRuntime { settings = settings };
            RunDataChanged?.Invoke();
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
            enemy.SetPresentationColor(settings.displayColor);
            enemy.State = settings.archetype == EnemyArchetype.PanShield ? ActorState.Shielding : ActorState.Idle;
        }

        void TickEnemyActors(float dt)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                GridActor enemy = enemies[i];
                if (!enemy || !enemy.Alive || !enemy.gameObject.activeInHierarchy || !enemyRuntime.TryGetValue(enemy, out EnemyRuntime runtime)) continue;
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
                target.Hurt(enemy.damage);
                InterruptWorkerAt(target);
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
            if (distance > enemy.attackRange)
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

            if (distance <= enemy.attackRange && enemy.AttackTimer <= 0f)
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
                actor.Hurt(enemy.damage);
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
                if (!actor || !actor.Alive || (duckOnly && actor.kind != ActorKind.Duck)) continue;
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
            if (seal && seal.Alive && !TickOwnerProcess(seal, dt)) TickSeal(dt);
            if (cat && cat.Alive && !TickOwnerProcess(cat, dt)) TickCat(dt);
            if (pufferfish && pufferfish.Alive && !TickOwnerProcess(pufferfish, dt)) TickPufferCombatState(dt);
        }

        bool TickOwnerProcess(GridActor actor, float dt)
        {
            ActivityStation assigned = null;
            float nearest = float.PositiveInfinity;
            foreach (ActivityStation station in stations)
            {
                if (!station || station.Complete || station.requiredKind != actor.kind) continue;
                float distance = (station.transform.position - actor.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                assigned = station;
            }
            if (!assigned) return false;
            if (actor.IsUltimateActive) return false;
            if (Vector3.Distance(actor.transform.position, assigned.transform.position) > .15f * board.cellSize)
            {
                actor.State = ActorState.Moving;
                MoveActorAlongGrid(actor, assigned.transform.position, dt, 1f);
            }
            else actor.State = ActorState.Working;
            return true;
        }

        void TickSeal(float dt)
        {
            if (seal.IsUltimateActive) return;
            sealAttackClock -= dt;
            GridActor target = FindNearestEnemy(seal.transform.position, seal.attackRange);
            if (!target || sealAttackClock > 0f) return;
            sealAttackClock = seal.EffectiveAttackInterval;
            if (seal.TryGetComponent(out AlienAnimationDriver animation)) animation.PlayAttack();
            CombatAndSpawnRules.ResolveSealAoe(seal, enemies, board, seal.attackRange, seal.EffectiveDamage,
                sealKnockbackDistance, (victim, dealt) => RegisterEnemyHit(seal, victim, dealt, false));
        }

        void TickCat(float dt)
        {
            if (cat.IsUltimateActive) { TickCatLasers(dt); return; }
            catAttackClock -= dt;
            GridActor target = FindNearestEnemy(cat.transform.position, cat.attackRange);
            if (!target || catAttackClock > 0f) return;
            catAttackClock = cat.EffectiveAttackInterval;
            if (cat.TryGetComponent(out AlienAnimationDriver animation)) animation.PlayAttack();
            float dealt = target.Hurt(cat.EffectiveDamage);
            RegisterEnemyHit(cat, target, dealt, false);
            if (target.Alive && catStunSeconds > 0f && enemyRuntime.TryGetValue(target, out EnemyRuntime runtime))
                runtime.stunRemaining = Mathf.Max(runtime.stunRemaining, catStunSeconds);
            InterruptWorkerAt(target);
        }

        void TickPufferCombatState(float dt)
        {
            if (pufferfish.IsUltimateActive) return;
            if (pufferFloatClock > 0f) return;
            pufferInflateClock += dt;
            pufferfish.State = ActorState.Inflating;
            if (pufferInflateClock < pufferInflateSeconds) return;
            pufferInflateClock = 0f;
            pufferFloatClock = pufferFloatSeconds;
            pufferDirection = Random.insideUnitCircle.normalized;
            if (pufferDirection.sqrMagnitude < .1f) pufferDirection = Vector2.right;
            pufferfish.State = ActorState.Moving;
            if (pufferfish.TryGetComponent(out AlienAnimationDriver animation)) animation.PlayAttack();
        }

        void TickPufferMovement(float dt)
        {
            if (!pufferfish || !pufferfish.Alive || (!pufferfish.IsUltimateActive && pufferFloatClock <= 0f)) return;
            pufferFloatClock = Mathf.Max(0f, pufferFloatClock - dt);
            if (!pufferfish.IsUltimateActive && pufferFloatClock <= 0f) { pufferfish.State = ActorState.Idle; return; }
            float speed = pufferFloatSpeed * (pufferfish.IsUltimateActive ? pufferUltimateSpeedMultiplier : 1f);
            float damageMultiplier = pufferfish.IsUltimateActive ? pufferUltimateDamageMultiplier : 1f;
            Vector3 current = pufferfish.transform.position;
            Vector3 proposed = current + (Vector3)(pufferDirection * speed * dt);
            if (!board.TryWorldToCell(proposed, out _))
            {
                Vector2Int outsideCell;
                board.TryWorldToCell(proposed, out outsideCell);
                Vector2Int min = board.ActiveMinCell;
                if (outsideCell.x < min.x || outsideCell.x >= min.x + board.ActiveWidth) pufferDirection.x = -pufferDirection.x;
                if (outsideCell.y < min.y || outsideCell.y >= min.y + board.ActiveHeight) pufferDirection.y = -pufferDirection.y;
                pufferDirection = pufferDirection.normalized;
                proposed = current + (Vector3)(pufferDirection * speed * dt);
            }

            bool bounced = false;
            GridActor collision = FindNearestEnemy(proposed, pufferCollisionRadius + .08f);
            if (collision)
            {
                pufferDirection = (current - collision.transform.position).normalized;
                if (pufferDirection.sqrMagnitude < .01f) pufferDirection = Vector2.right;
                if (!pufferHitClocks.TryGetValue(collision, out float hitClock) || hitClock <= 0f)
                {
                    pufferHitClocks[collision] = pufferRepeatHitSeconds;
                    float dealt = collision.Hurt(pufferfish.EffectiveDamage * pufferDamageMultiplier * damageMultiplier);
                    RegisterEnemyHit(pufferfish, collision, dealt, pufferfish.IsUltimateActive);
                    InterruptWorkerAt(collision);
                }
                proposed = current + (Vector3)(pufferDirection * speed * dt);
                bounced = true;
            }

            if (!bounced)
            {
                foreach (ActivityStation station in stations)
                {
                    if (!station || Vector3.Distance(proposed, station.transform.position) >= pufferCollisionRadius + .2f) continue;
                    pufferDirection = (current - station.transform.position).normalized;
                    if (pufferDirection.sqrMagnitude < .01f) pufferDirection = Vector2.right;
                    proposed = current + (Vector3)(pufferDirection * speed * dt);
                    bounced = true;
                    break;
                }
            }

            if (!bounced)
            {
                foreach (GridActor ally in runLoop.Survivors)
                {
                    if (!ally || ally == pufferfish || !ally.Alive || Vector3.Distance(proposed, ally.transform.position) >= pufferCollisionRadius + .2f) continue;
                    pufferDirection = (current - ally.transform.position).normalized;
                    if (pufferDirection.sqrMagnitude < .01f) pufferDirection = Vector2.right;
                    proposed = current + (Vector3)(pufferDirection * speed * dt);
                    bounced = true;
                    break;
                }
            }

            if (!board.TryWorldToCell(proposed, out _)) proposed = current;
            pufferfish.transform.position = proposed;
            pufferfish.RefreshCell();
            pufferfish.State = ActorState.Moving;
        }

        void TickProjectiles(float dt)
        {
            var hitEnemies = new List<GridActor>(pufferHitClocks.Keys);
            foreach (GridActor enemy in hitEnemies)
            {
                if (!enemy) { pufferHitClocks.Remove(enemy); continue; }
                pufferHitClocks[enemy] = Mathf.Max(0f, pufferHitClocks[enemy] - dt);
            }
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                ProjectileRuntime projectile = projectiles[i];
                if (!projectile.gameObject || !projectile.target || !projectile.target.Alive)
                { if (projectile.gameObject) Destroy(projectile.gameObject); projectiles.RemoveAt(i); continue; }
                Transform shot = projectile.gameObject.transform;
                shot.position = Vector3.MoveTowards(shot.position, projectile.target.transform.position, projectile.speed * dt);
                if (Vector3.Distance(shot.position, projectile.target.transform.position) > toasterProjectileRadius + .1f) continue;
                projectile.target.Hurt(projectile.damage);
                InterruptWorkerAt(projectile.target);
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
                ripple.radius = Mathf.Min(Mathf.Max(board.width, board.height) * board.cellSize, previous + sealRippleSpeed * dt);
                foreach (GridActor enemy in enemies)
                {
                    if (!enemy || !enemy.Alive || ripple.hit.Contains(enemy)) continue;
                    float distance = Vector3.Distance(ripple.origin, enemy.transform.position);
                    if (distance > ripple.radius || distance < previous) continue;
                    ripple.hit.Add(enemy);
                    float maximumRadius = Mathf.Max(board.width, board.height) * board.cellSize;
                    float falloff = Mathf.Lerp(1f, .25f, Mathf.Clamp01(distance / Mathf.Max(.1f, maximumRadius)));
                    float dealt = enemy.Hurt(seal.EffectiveDamage * sealRippleDamageMultiplier * falloff);
                    RegisterEnemyHit(seal, enemy, dealt, true);
                    Vector3 push = (enemy.transform.position - ripple.origin).normalized * sealRippleKnockback * falloff;
                    Vector3 destination = enemy.transform.position + push;
                    if (board.TryWorldToCell(destination, out _)) enemy.transform.position = destination;
                    InterruptWorkerAt(enemy);
                }
                if (ripple.radius >= Mathf.Max(board.width, board.height) * board.cellSize) sealRipples.RemoveAt(i);
            }
        }

        void TickCatLasers(float dt)
        {
            catLaserClock -= dt;
            if (catLaserClock > 0f) return;
            catLaserClock = catLaserHitIntervalSeconds;
            Vector2 center = cat.transform.position;
            float phase = (catLaserDurationSeconds - cat.UltimateRemaining) *
                          (360f * Mathf.Max(.1f, catLaserRevolutions) / Mathf.Max(.1f, catLaserDurationSeconds));
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || !enemy.Alive) continue;
                Vector2 offset = (Vector2)enemy.transform.position - center;
                float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
                float nearest = 180f;
                for (int beam = 0; beam < 8; beam++)
                {
                    float beamAngle = phase + beam * 45f;
                    float difference = Mathf.Abs(Mathf.DeltaAngle(angle, beamAngle));
                    nearest = Mathf.Min(nearest, Mathf.Min(difference, Mathf.Abs(difference - 180f)));
                }
                if (offset.magnitude > catLaserReach || nearest > catLaserArcDegrees * .5f) continue;
                float dealt = enemy.Hurt(catLaserDamage + cat.EffectiveDamage * .25f);
                RegisterEnemyHit(cat, enemy, dealt, true);
                InterruptWorkerAt(enemy);
            }
        }

        public bool ActivateUltimate(GridActor actor)
        {
            if (!runLoop || runLoop.State != RunState.Running || !actor || !actor.Alive || !actor.UltimateFull || actor.kind == ActorKind.Duck) return false;
            float duration = actor.kind == ActorKind.Seal ? sealUltimateDuration : catLaserDurationSeconds;
            if (actor.kind == ActorKind.Pufferfish) duration = pufferFloatSeconds;
            if (!actor.BeginUltimate(duration)) return false;
            if (actor.kind == ActorKind.Seal)
            {
                sealRipples.Add(new RippleRuntime { origin = seal.transform.position });
                seal.ShowAttack(Mathf.Max(board.width, board.height) * board.cellSize);
            }
            else if (actor.kind == ActorKind.Cat) catLaserClock = 0f;
            else if (actor.kind == ActorKind.Pufferfish)
            {
                pufferDirection = Random.insideUnitCircle.normalized;
                if (pufferDirection.sqrMagnitude < .1f) pufferDirection = Vector2.right;
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
                AwardKill(null, ultimate, false);
            }
            enemyRuntime.Remove(enemy);
            pendingKillIsUltimate.Remove(enemy);
            pufferHitClocks.Remove(enemy);
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
        }

        public void RegisterCrushKill(GridActor enemy)
        {
            if (!enemy) return;
            pendingKillIsUltimate[enemy] = false;
        }
    }
}
