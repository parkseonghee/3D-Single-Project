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
        [Header("강화와 철거")]
        [Min(0)] public int upgradeGoldCost = 80;
        [Min(1)] public float upgradeCostMultiplier = 1.5f;
        [Range(0, 1)] public float demolitionRefundRatio = 0.5f;
        [Header("일일 벼 채취 기여량 · 생산하지 않는 건물은 0")]
        [Min(0)] public int initialDailyRice;
        [Min(0)] public int additionalDailyRice;
        [Min(0)] public int dailyRicePerUpgrade;
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

        public int UpgradeCostAt(int level)
        {
            double cost = upgradeGoldCost * System.Math.Pow(upgradeCostMultiplier, level - 1);
            return (int)System.Math.Min(System.Math.Round(cost, System.MidpointRounding.AwayFromZero), int.MaxValue);
        }

        public int DemolitionRefund => (int)System.Math.Round(riceCost * (double)demolitionRefundRatio,
            System.MidpointRounding.AwayFromZero);
    }
}
