using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("방패병 팀 실드")]
        [SerializeField] private GameObject teamShieldPrefab;
        [SerializeField, Min(0.1f)] private float teamShieldInterval = 10f;
        [SerializeField, Min(0.1f)] private float teamShieldDuration = 5f;
        [SerializeField, Min(1)] private float teamShieldAmount = 50f;
        private readonly Dictionary<Transform, float> teamShieldCooldowns = new Dictionary<Transform, float>();

        private void UpdateTeamShields(float dt)
        {
            if (experience == null) return;
            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                if (unit == null || !unit.gameObject.activeSelf || !experience.HasTeamShield(unit) ||
                    !allies.TryGetValue(unit, out var health) || health.IsDead) continue;
                float remaining = teamShieldCooldowns.TryGetValue(unit, out float time) ? time : teamShieldInterval;
                remaining -= dt;
                if (remaining <= 0 && teamAttackRemaining <= 0 && teamAttackSpeedRemaining <= 0 && teamAttackSpeedCooldown != 0)
                {
                    foreach (var ally in allies)
                        if (ally.Key != null && ally.Key.gameObject.activeSelf && !ally.Value.IsDead)
                            ally.Value.GiveShield(teamShieldAmount, teamShieldDuration, teamShieldPrefab);
                    remaining = teamShieldInterval;
                }
                teamShieldCooldowns[unit] = remaining;
            }
        }
    }
}
