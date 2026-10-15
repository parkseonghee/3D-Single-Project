using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Relic Definition")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [Header("카드 표시")]
        public string displayName;
        [TextArea(2, 5)] public string description;
        public Sprite icon;

        [Header("이번 전투 시작 보상")]
        [Min(0)] public int experienceBottles;
        [Min(0)] public int bottlesPerLibraryUpgrade;
        [Min(0)] public float commanderShield;
        [Min(0)] public float commanderShieldPerLibraryUpgrade;
        [Min(0)] public float soldierShield;
        [Min(0)] public float soldierShieldPerLibraryUpgrade;
        [Min(0.1f)] public float shieldDuration = 120f;

        public string DescriptionAt(int libraryLevel) => string.Format(description,
            BottlesAt(libraryLevel), CommanderShieldAt(libraryLevel), SoldierShieldAt(libraryLevel));

        public void Apply(RunController run, BattleExperience experience, RecruitmentController recruitment,
            int libraryLevel)
        {
            int bottles = BottlesAt(libraryLevel);
            float commanderAmount = CommanderShieldAt(libraryLevel);
            float soldierAmount = SoldierShieldAt(libraryLevel);
            if (bottles > 0) experience.GrantBottles(bottles);
            if (commanderAmount > 0 && run.Commander.TryGetComponent(out UnitHealth commander))
                commander.GiveShield(commanderAmount, shieldDuration, null);
            if (soldierAmount <= 0) return;
            foreach (var soldier in recruitment.Units)
                if (soldier.Key != null && soldier.Key.TryGetComponent(out UnitHealth health))
                    health.GiveShield(soldierAmount, shieldDuration, null);
        }

        private int BottlesAt(int level) => (int)System.Math.Min(int.MaxValue,
            (long)experienceBottles + (long)Mathf.Max(0, level - 1) * bottlesPerLibraryUpgrade);
        private float CommanderShieldAt(int level) => commanderShield +
            Mathf.Max(0, level - 1) * commanderShieldPerLibraryUpgrade;
        private float SoldierShieldAt(int level) => soldierShield +
            Mathf.Max(0, level - 1) * soldierShieldPerLibraryUpgrade;
    }
}
