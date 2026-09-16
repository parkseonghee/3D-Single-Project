using ArmySurvivor.Building;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        public GameObject prefab;
        public AttackDefinition attack;
        public BuildingDefinition requiredBuilding;
        [Min(0)] public int riceCost;
        public Vector3 modelOffset;
        public Vector3 modelRotation;
        public Vector3 modelScale = Vector3.one;
    }
}
