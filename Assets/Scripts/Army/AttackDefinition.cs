using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Attack Definition")]
    public class AttackDefinition : ScriptableObject
    {
        public enum AttackStyle { StraightProjectile, HomingProjectile, MeleeArc, MeleeLine }
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
    }
}
