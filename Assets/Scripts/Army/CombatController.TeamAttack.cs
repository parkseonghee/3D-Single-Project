using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("방패병 공격력 오라")]
        [SerializeField] private GameObject teamAttackPrefab;
        [SerializeField, Min(0.1f)] private float teamAttackFirstDelay = 5f;
        [SerializeField, Min(0.1f)] private float teamAttackInterval = 10f;
        [SerializeField, Min(0.1f)] private float teamAttackDuration = 5f;
        [SerializeField, Min(0)] private float teamAttackBonus = 0.25f;
        private float teamAttackCooldown = -1f;
        private float teamAttackRemaining;
        private readonly Dictionary<UnitHealth, GameObject> teamAttackEffects = new Dictionary<UnitHealth, GameObject>();

        private void UpdateTeamAttack(float dt)
        {
            if (teamAttackRemaining > 0)
            {
                teamAttackRemaining = Mathf.Max(0, teamAttackRemaining - dt);
                foreach (var effect in teamAttackEffects)
                    if (effect.Value != null && (effect.Key == null || effect.Key.IsDead || !effect.Key.gameObject.activeSelf))
                        effect.Value.SetActive(false);
                if (teamAttackRemaining <= 0) ClearTeamAttack();
            }
            bool hasCaster = false;
            foreach (var entry in recruitment.Units)
                if (entry.Key != null && entry.Key.gameObject.activeSelf && experience != null &&
                    experience.HasTeamAttack(entry.Key) && allies.TryGetValue(entry.Key, out var health) && !health.IsDead)
                    hasCaster = true;
            if (!hasCaster) { teamAttackCooldown = -1f; return; }
            if (teamAttackCooldown < 0) teamAttackCooldown = teamAttackFirstDelay;
            teamAttackCooldown = Mathf.Max(0, teamAttackCooldown - dt);
            if (teamAttackCooldown > 0 || teamAttackRemaining > 0 || teamAttackSpeedRemaining > 0 || teamAttackSpeedCooldown == 0) return;
            // 두 스킬은 서로의 지속 시간이 끝날 때까지 대기한다.
            foreach (var ally in allies)
                if (ally.Value != null && !ally.Value.IsDead && ally.Value.Shield > 0) return;
            teamAttackRemaining = teamAttackDuration;
            teamAttackCooldown = teamAttackInterval;
            foreach (var ally in allies)
                if (ally.Key != null && ally.Key.gameObject.activeSelf && !ally.Value.IsDead && teamAttackPrefab != null)
                {
                    GameObject effect = Instantiate(teamAttackPrefab, ally.Key);
                    effect.transform.localPosition = Vector3.zero;
                    // 버프가 끝날 때까지 한 번 유지하고 중간에 다시 생성하지 않는다.
                    foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main = particle.main;
                        main.loop = false;
                        main.startLifetime = teamAttackDuration;
                        main.duration = teamAttackDuration;
                    }
                    foreach (var particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                        particle.Play(false);
                    teamAttackEffects[ally.Value] = effect;
                }
        }

        private void ClearTeamAttack()
        {
            teamAttackRemaining = 0;
            foreach (var effect in teamAttackEffects.Values)
                if (effect != null) { effect.SetActive(false); Destroy(effect); }
            teamAttackEffects.Clear();
        }
    }
}
