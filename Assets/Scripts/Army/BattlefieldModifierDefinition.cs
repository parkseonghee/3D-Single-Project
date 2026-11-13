using UnityEngine;

namespace ArmySurvivor.Army
{
    // 모험 모드 출전 미리보기에 표시하고 그날 전장에 적용하는 변수 1개(기획서 7.1).
    [CreateAssetMenu(menuName = "Army Survivor/Battlefield Modifier")]
    public class BattlefieldModifierDefinition : ScriptableObject
    {
        public enum Effect
        {
            EnemySpawnWeight,   // targets의 출현 가중치 × multiplier
            AllyRangedDamage,   // 아군 투사체 기본 공격 피해 × multiplier
            AllyMeleeDamage,    // 아군 근접·돌격 기본 공격 피해 × multiplier
            EnemyHealth,        // 일반 적 체력 × multiplier (보스 제외)
            EnemySpeed          // 일반 적 이동 속도 × multiplier (보스 제외)
        }

        public string displayName;
        [TextArea] public string description;
        public Effect effect;
        [Min(0)] public float multiplier = 1;
        [Tooltip("EnemySpawnWeight 전용. 구간별 Tier 정의를 모두 넣는다.")]
        public EnemyDefinition[] targets;
        [Min(1)] public int minimumDay = 1;

        public bool IsTarget(EnemyDefinition enemy)
        {
            if (enemy == null || targets == null) return false;
            foreach (EnemyDefinition target in targets)
                if (target == enemy) return true;
            return false;
        }

        public float WeightMultiplier(EnemyDefinition enemy) =>
            effect == Effect.EnemySpawnWeight && IsTarget(enemy) ? multiplier : 1f;

        public float AllyDamageMultiplier(AttackDefinition attack)
        {
            if (attack == null) return 1f;
            bool ranged = attack.style == AttackDefinition.AttackStyle.StraightProjectile ||
                          attack.style == AttackDefinition.AttackStyle.HomingProjectile;
            if (effect == Effect.AllyRangedDamage && ranged) return multiplier;
            if (effect == Effect.AllyMeleeDamage && !ranged) return multiplier;
            return 1f;
        }

        public float EnemyHealthMultiplier => effect == Effect.EnemyHealth ? multiplier : 1f;
        public float EnemySpeedMultiplier => effect == Effect.EnemySpeed ? multiplier : 1f;

        // 가중치 변수는 그날 풀에 대상 적이 실제로 있을 때만 의미가 있다.
        public bool IsAvailable(DaySettings.Stage stage, int day)
        {
            if (day < minimumDay) return false;
            if (effect != Effect.EnemySpawnWeight) return true;
            if (stage == null || stage.enemies == null) return false;
            foreach (DaySettings.EnemySpawn entry in stage.enemies)
                if (entry != null && entry.firstDay <= day && entry.weight > 0 && IsTarget(entry.enemy)) return true;
            return false;
        }
    }
}
