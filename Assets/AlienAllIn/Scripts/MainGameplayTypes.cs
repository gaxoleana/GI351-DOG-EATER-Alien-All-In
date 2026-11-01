using System;
using UnityEngine;

namespace AlienAllIn
{
    public enum ActivityKind { Casino, Darts, JumpRope }
    public enum EnemyArchetype { RoasterMelee, ToasterRanged, PanShield }
    public enum PufferAttackPhase { Stationary, Locked, Launched, Bouncing }

    [Serializable]
    public sealed class ActivityPrototypeSettings
    {
        public ActivityKind kind;
        public ActorKind owner;
        [Tooltip("Optional visual prefab for this activity. Uses the legacy prototype when empty.")]
        public ActivityStation prefab;
        [Min(1f)] public float durationSeconds = 36f;
        [Min(.05f)] public float cashTickSeconds = 1f;
        [Min(0f)] public float processScoreTickSeconds = 2f;
        public Color markerColor = new Color(.9f, .72f, .32f, 1f);
    }

    [Serializable]
    public sealed class EnemyPrototypeSettings
    {
        public EnemyArchetype archetype;
        [Tooltip("Optional visual prefab for this enemy archetype.")]
        public GridActor prefab;
        [Min(0f)] public float spawnWeight = 1f;
        [Min(1f)] public float health = 10f;
        [Min(.01f)] public float moveSpeed = .8f;
        [Min(0f)] public float damage = 3f;
        [Min(.1f)] public float attackRange = .65f;
        [Min(.05f)] public float attackInterval = 1.2f;
        [Min(0f)] public float specialValue = 1f;
        [Min(.01f)] public float projectileSpeed = 5f;
        [Min(0f)] public float shieldedDamageReduction = .2f;
        public Color displayColor = Color.white;
    }
}
