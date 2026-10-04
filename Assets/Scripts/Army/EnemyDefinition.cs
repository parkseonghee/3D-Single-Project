using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
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
    }
}
