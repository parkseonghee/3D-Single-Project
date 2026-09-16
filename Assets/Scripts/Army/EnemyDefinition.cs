using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public GameObject prefab;
        [Min(1)] public float health;
        [Min(0)] public float speed;
        [Min(0)] public float stoppingDistance;
        [Min(0)] public float turnSpeed;
        [Min(0)] public float deathDuration;
        public string movingParameter;
        public string deathTrigger;
    }
}
