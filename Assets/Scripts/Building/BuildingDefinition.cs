using UnityEngine;

namespace ArmySurvivor.Building
{
    [CreateAssetMenu(menuName = "Army Survivor/Building Definition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [Header("건물 정보")]
        public string displayName;
        public GameObject prefab;
        public Sprite icon;
        [Header("건설 비용")]
        [Min(0)] public int riceCost = 120;
        [Min(1)] public float costMultiplier = 1.5f;
        [Header("점유 크기와 모델 보정")]
        public Vector3 footprint = Vector3.one;
        public Vector3 modelOffset;
        public Vector3 modelRotation;
        public Vector3 modelScale = Vector3.one;
        public bool IsValid => prefab != null && footprint.x > 0 && footprint.y > 0 && footprint.z > 0;
        public int CostAt(int count)
        {
            double cost = riceCost * System.Math.Pow(costMultiplier, count);
            double roundedCost = System.Math.Round(cost, System.MidpointRounding.AwayFromZero);
            return (int)System.Math.Min(roundedCost, int.MaxValue);
        }
    }
}
