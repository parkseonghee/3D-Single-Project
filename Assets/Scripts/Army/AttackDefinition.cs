using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Attack Definition")]
    public class AttackDefinition : ScriptableObject
    {
        public enum AttackStyle { StraightProjectile, HomingProjectile, MeleeArc, MeleeLine, Charge, ApproachArc }
        public AttackStyle style;
        [Min(0.1f)] public float range;
        [Min(0.01f)] public float interval;
        [Min(0)] public float damage;
        [Min(0.1f)] public float speed;
        [Min(0.01f)] public float hitRadius;
        public float height;
        public GameObject projectilePrefab;
        public string animationTrigger;
        [Min(0)] public float windup;
        [Range(1, 180)] public float arcAngle = 100;
        [Min(0)] public float knockback;
        [Header("접근 공격")]
        [Min(1)] public float detectionRange = 10;
        [Min(0.1f)] public float approachSpeed = 6;
        [Min(0.1f)] public float returnSpeed = 8;
        [Min(0.1f)] public float stoppingDistance = 1.5f;
        [Min(0.1f)] public float effectScale = 1;
    }
}
