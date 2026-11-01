using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlienAllIn
{
    /// <summary>Shared combat and edge-spawn rules used by both gameplay scenes.</summary>
    public static class CombatAndSpawnRules
    {
        public static void ResolveSealAoe(
            GridActor seal,
            IEnumerable<GridActor> enemies,
            BoardGrid board,
            float range,
            float damage,
            float knockbackDistance) =>
            ResolveSealAoe(seal, enemies, board, range, damage, knockbackDistance, null);

        public static void ResolveSealAoe(
            GridActor seal,
            IEnumerable<GridActor> enemies,
            BoardGrid board,
            float range,
            float damage,
            float knockbackDistance,
            Action<GridActor, float> onHit)
        {
            if (!seal || !seal.Alive || enemies == null) return;

            seal.ShowAttack(range);
            foreach (GridActor enemy in enemies)
            {
                if (!enemy || !enemy.Alive || Vector3.Distance(enemy.transform.position, seal.transform.position) > range)
                    continue;

                float dealt = enemy.Hurt(damage);
                if (dealt <= 0f) continue;
                seal.AddCharge(dealt);
                onHit?.Invoke(enemy, dealt);
                if (!enemy.Alive || !board) continue;

                Vector3 push = (enemy.transform.position - seal.transform.position).normalized * knockbackDistance;
                Vector3 proposed = enemy.transform.position + push;
                if (board.TryWorldToCell(proposed, out _)) enemy.transform.position = proposed;
            }
        }

        /// <summary>Moves a melee actor to its target or invokes the hit callback on its cadence.</summary>
        public static bool TickMelee(GridActor attacker, Vector3 targetPosition, float dt, Action onHit)
        {
            if (!attacker || !attacker.Alive || dt <= 0f) return false;

            attacker.AttackTimer -= dt;
            if (Vector3.Distance(attacker.transform.position, targetPosition) <= attacker.attackRange)
            {
                attacker.State = ActorState.Defending;
                if (attacker.AttackTimer > 0f) return false;

                onHit?.Invoke();
                attacker.AttackTimer = attacker.attackInterval;
                return true;
            }

            attacker.State = ActorState.Moving;
            attacker.MoveTowards(targetPosition, dt);
            return false;
        }
    }

    /// <summary>Common four-edge enemy placement, optionally excluding occupied system cells.</summary>
    public static class GridEnemySpawner
    {
        public static GridActor SpawnAtActiveEdges(
            BoardGrid board,
            GridActor enemyPrefab,
            Transform container,
            ref int spawnIndex,
            ref int nextSpawnEdge,
            ISet<Vector2Int> blockedCells = null)
        {
            if (!board || !enemyPrefab) return null;

            Vector2Int min = board.ActiveMinCell;
            int centerX = min.x + (board.ActiveWidth - 1) / 2;
            int centerY = min.y + (board.ActiveHeight - 1) / 2;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                int edge = (nextSpawnEdge + attempt) % 4;
                Vector2Int cell = edge switch
                {
                    0 => new Vector2Int(min.x, centerY),
                    1 => new Vector2Int(centerX, min.y + board.ActiveHeight - 1),
                    2 => new Vector2Int(min.x + board.ActiveWidth - 1, centerY),
                    _ => new Vector2Int(centerX, min.y)
                };

                BoardCell boardCell = board.GetCell(cell);
                if (blockedCells != null && blockedCells.Contains(cell)) continue;
                if (!boardCell || boardCell.Occupants.Count > 0) continue;

                nextSpawnEdge = (edge + 1) % 4;
                GridActor enemy = UnityEngine.Object.Instantiate(
                    enemyPrefab, board.CellCenter(cell), Quaternion.identity, container);
                enemy.name = $"Enemy {++spawnIndex:00}";
                enemy.board = board;
                enemy.ResetActor();
                return enemy;
            }

            nextSpawnEdge = (nextSpawnEdge + 1) % 4;
            return null;
        }
    }
}
