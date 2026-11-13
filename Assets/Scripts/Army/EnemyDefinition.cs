using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        [Tooltip("출전 미리보기 등 UI에 표시하는 이름")]
        public string displayName;
        public GameObject prefab;
        [Min(1)] public float health;
        [Min(0)] public float armor;
        [Min(0)] public float damage = 10;
        [Min(0.1f)] public float attackInterval = 1;
        [Min(0)] public float attackWindup = 0.35f;
        [Min(0)] public float attackDuration = 0.87f;
        public string attackState = "Base Layer.Attack";
        [Min(0)] public float speed;
        [Min(0)] public float stoppingDistance;
        [Min(0)] public float turnSpeed;
        [Min(0)] public float deathDuration;
        public string movingParameter;
        public string deathTrigger;
        public string hitState;
        [Min(0)] public float hitReactionDuration = 0.3f;
        [Header("원거리 공격 (투사체가 없으면 근접 공격)")]
        public GameObject projectilePrefab;
        [Min(0.1f)] public float projectileSpeed = 12;
        [Min(0.1f)] public float projectileRange = 14;
        [Min(0.01f)] public float projectileHitRadius = 0.5f;
        public float projectileHeight = 0.8f;
    }
}
