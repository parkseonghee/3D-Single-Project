using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("기마병 연쇄 돌격")]
        [SerializeField, Min(1)] private float chainChargeFirstMultiplier = 2f;
        [SerializeField, Range(0.01f, 0.99f)] private float chainChargeDecay = 0.8f;
        [Header("기마병 추진")]
        [SerializeField, Min(0)] private float momentumSpeedConversion = 0.5f;
        [SerializeField, Min(0)] private float momentumMaximumBonus = 0.5f;

        private float MomentumDamageMultiplier(Transform unit)
        {
            if (experience == null || !experience.HasMomentum(unit)) return 1f;
            float extraSpeed = Mathf.Max(0, run.TeamMoveMultiplier - 1f);
            return 1f + Mathf.Min(momentumMaximumBonus, extraSpeed * momentumSpeedConversion);
        }

        private EnemyState FindChainChargeTarget(Transform unit, AttackDefinition attack)
        {
            EnemyState nearest = null;
            float nearestDistance = attack.detectionRange;
            float formationLimit = attack.detectionRange + run.CurrentTactic.radius;
            foreach (EnemyState enemy in enemies)
            {
                if (enemy.root == null || enemy.health <= 0) continue;
                // 연쇄 도중에도 기존 진형 이탈 한계를 지킨다.
                if (FlatDistance(enemy.root.position, run.Commander.position) > formationLimit) continue;
                float distance = FlatDistance(unit.position, enemy.root.position);
                if (distance > nearestDistance) continue;
                nearestDistance = distance;
                nearest = enemy;
            }
            return nearest;
        }
    }
}
