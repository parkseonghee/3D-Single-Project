using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("마법사 광역 레이저")]
        [SerializeField] private GameObject laserPrefab;
        [SerializeField, Min(0.1f)] private float laserInterval = 5f;
        [SerializeField, Min(0.1f)] private float laserDuration = 3f;
        [SerializeField, Min(0.1f)] private float laserRadius = 3f;
        [SerializeField, Min(0)] private float laserDamagePerSecond = 25f;
        [SerializeField, Min(0.05f)] private float laserTickInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float laserTargetRange = 15f;

        private class LaserState
        {
            public Transform owner;
            public Vector3 position;
            public GameObject visual;
            public float remaining;
            public float tickTime;
        }
        private readonly Dictionary<Transform, float> laserCooldowns = new Dictionary<Transform, float>();
        private readonly List<LaserState> lasers = new List<LaserState>();

        private void UpdateLasers(float dt)
        {
            if (experience == null || laserPrefab == null) return;
            for (int i = lasers.Count - 1; i >= 0; i--)
            {
                LaserState laser = lasers[i];
                float elapsed = Mathf.Min(dt, laser.remaining);
                laser.remaining -= elapsed;
                laser.tickTime += elapsed;
                while (laser.tickTime >= laserTickInterval)
                {
                    laser.tickTime -= laserTickInterval;
                    DamageLaserArea(laser, laserDamagePerSecond * laserTickInterval);
                }
                if (laser.remaining > 0) continue;
                if (laser.tickTime > 0) DamageLaserArea(laser, laserDamagePerSecond * laser.tickTime);
                if (laser.visual != null)
                {
                    laser.visual.SetActive(false);
                    Destroy(laser.visual);
                }
                lasers.RemoveAt(i);
            }
            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                if (unit == null || !unit.gameObject.activeSelf || !experience.HasLaserAOE(unit) ||
                    !allies.TryGetValue(unit, out var health) || health.IsDead) continue;
                float remaining = laserCooldowns.TryGetValue(unit, out float time) ? time : laserInterval;
                remaining = Mathf.Max(0, remaining - dt / experience.SkillCooldownMultiplier(unit));
                if (remaining <= 0)
                {
                    EnemyState target = NearestEnemy(unit.position, laserTargetRange);
                    if (target != null)
                    {
                        // 머리 위에서 내리쬐는 레이저는 발동 당시 적의 지면 위치에 고정한다.
                        Vector3 position = target.root.position;
                        GameObject visual = Instantiate(laserPrefab, position, laserPrefab.transform.rotation, combatRoot);
                        visual.transform.localScale = Vector3.one;
                        lasers.Add(new LaserState { owner = unit, position = position,
                            visual = visual, remaining = laserDuration });
                        remaining = laserInterval;
                    }
                }
                laserCooldowns[unit] = remaining;
            }
        }

        private void DamageLaserArea(LaserState laser, float damage)
        {
            foreach (EnemyState enemy in enemies)
                if (enemy.root != null && enemy.health > 0 && FlatDistance(laser.position, enemy.root.position) <= laserRadius)
                    ApplyDamage(enemy, damage, (enemy.root.position - laser.position).normalized, 0, false, laser.owner);
        }
    }
}
