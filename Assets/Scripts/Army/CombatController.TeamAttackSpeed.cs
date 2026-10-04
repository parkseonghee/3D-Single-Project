using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("방패병 공격속도 오라")]
        [SerializeField] private GameObject teamAttackSpeedPrefab;
        [SerializeField, Min(0.1f)] private float teamAttackSpeedFirstDelay = 5f;
        [SerializeField, Min(0.1f)] private float teamAttackSpeedInterval = 10f;
        [SerializeField, Min(0.1f)] private float teamAttackSpeedDuration = 5f;
        [SerializeField, Min(0)] private float teamAttackSpeedBonus = 0.25f;
        private float teamAttackSpeedCooldown = -1f;
        private float teamAttackSpeedRemaining;
        private readonly Dictionary<UnitHealth, GameObject> teamAttackSpeedEffects = new Dictionary<UnitHealth, GameObject>();

        private void UpdateTeamAttackSpeed(float dt)
        {
            if (teamAttackSpeedRemaining > 0)
            {
                teamAttackSpeedRemaining = Mathf.Max(0, teamAttackSpeedRemaining - dt);
                foreach (var effect in teamAttackSpeedEffects)
                    if (effect.Value != null && (effect.Key == null || effect.Key.IsDead || !effect.Key.gameObject.activeSelf))
                        effect.Value.SetActive(false);
                if (teamAttackSpeedRemaining <= 0) ClearTeamAttackSpeed();
            }
            bool hasCaster = false;
            foreach (var entry in recruitment.Units)
                if (entry.Key != null && entry.Key.gameObject.activeSelf && experience != null &&
                    experience.HasTeamAttackSpeed(entry.Key) && allies.TryGetValue(entry.Key, out var health) && !health.IsDead)
                    hasCaster = true;
            if (!hasCaster) { teamAttackSpeedCooldown = -1f; return; }
            if (teamAttackSpeedCooldown < 0) teamAttackSpeedCooldown = teamAttackSpeedFirstDelay;
            teamAttackSpeedCooldown = Mathf.Max(0, teamAttackSpeedCooldown - dt);
            if (teamAttackSpeedCooldown > 0 || teamAttackSpeedRemaining > 0 || teamAttackRemaining > 0) return;
            // 두 스킬은 서로의 지속 시간이 끝날 때까지 대기한다.
            foreach (var ally in allies)
                if (ally.Value != null && !ally.Value.IsDead && ally.Value.Shield > 0) return;
            teamAttackSpeedRemaining = teamAttackSpeedDuration;
            teamAttackSpeedCooldown = teamAttackSpeedInterval;
            foreach (var ally in allies)
                if (ally.Key != null && ally.Key.gameObject.activeSelf && !ally.Value.IsDead && teamAttackSpeedPrefab != null)
                {
                    GameObject effect = Instantiate(teamAttackSpeedPrefab, ally.Key);
                    effect.transform.localPosition = Vector3.zero;
                    // 버프가 끝날 때까지 한 번 유지하고 중간에 다시 생성하지 않는다.
                    foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main = particle.main;
                        main.loop = false;
                        main.startLifetime = teamAttackSpeedDuration;
                        main.duration = teamAttackSpeedDuration;
                    }
                    foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                        particle.Play(false);
                    teamAttackSpeedEffects[ally.Value] = effect;
                }
        }

        private void ClearTeamAttackSpeed()
        {
            teamAttackSpeedRemaining = 0;
            foreach (var effect in teamAttackSpeedEffects.Values)
                if (effect != null) { effect.SetActive(false); Destroy(effect); }
            teamAttackSpeedEffects.Clear();
        }
    }
}
