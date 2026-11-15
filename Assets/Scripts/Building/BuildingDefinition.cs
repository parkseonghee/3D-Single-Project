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
        [Tooltip("건설 목록 카드에 보이는 짧은 효과 설명 (1~2줄)")]
        [TextArea(2, 3)] public string cardDescription;
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
        [Header("편성 상한 기여 (기획서 9 막사) · 기여하지 않는 건물은 0")]
        [Min(0)] public int formationSlots;
        [Min(0)] public int formationSlotsPerUpgrade;
        [Header("전투 능력치 보너스 (기획서 9) · 0.1 = 10%, 모험 모드에만 적용")]
        [Tooltip("켜면 모든 병력, 끄면 이 건물에서 고용하는 클래스(UnitDefinition.requiredBuilding)에만 적용")]
        public bool boostsAllUnits;
        [Min(0)] public float unitDamageBonus;
        [Min(0)] public float unitDamageBonusPerUpgrade;
        [Min(0)] public float unitHealthBonus;
        [Min(0)] public float unitHealthBonusPerUpgrade;
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

        // 이 건물 1채가 해당 레벨에서 늘려 주는 편성 칸 수.
        public int FormationSlotsAt(int level) =>
            level < 1 ? 0 : (int)System.Math.Min(int.MaxValue, formationSlots + (long)(level - 1) * formationSlotsPerUpgrade);

        // 건설(레벨 1) 보너스 + 강화 레벨마다 추가 보너스.
        public float DamageBonusAt(int level) => level < 1 ? 0 : unitDamageBonus + (level - 1) * unitDamageBonusPerUpgrade;
        public float HealthBonusAt(int level) => level < 1 ? 0 : unitHealthBonus + (level - 1) * unitHealthBonusPerUpgrade;

        public int UpgradeCostAt(int level)
        {
            double cost = upgradeGoldCost * System.Math.Pow(upgradeCostMultiplier, level - 1);
            return (int)System.Math.Min(System.Math.Round(cost, System.MidpointRounding.AwayFromZero), int.MaxValue);
        }

        public int DemolitionRefund => (int)System.Math.Round(riceCost * (double)demolitionRefundRatio,
            System.MidpointRounding.AwayFromZero);
    }
}
