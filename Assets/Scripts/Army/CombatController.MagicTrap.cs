using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("마법사 마법 함정")]
        [SerializeField] private GameObject magicTrapPrefab;
        [SerializeField] private GameObject magicTrapSmokePrefab;
        [SerializeField] private GameObject trapFireballPrefab;
        [SerializeField] private GameObject trapPortalPrefab;
        [SerializeField] private GameObject trapImpactEffectPrefab;
        [SerializeField, Min(0.1f)] private float magicTrapInterval = 10f;
        [SerializeField, Min(0.1f)] private float magicTrapLifetime = 30f;
        [SerializeField, Min(0.1f)] private float magicTrapTriggerRadius = 1f;
        [SerializeField, Min(0.1f)] private float trapFireballHeight = 8f;
        [SerializeField, Min(0.1f)] private float trapFireballSpeed = 5f;
        [SerializeField] private float trapFireballRotationSpeed = 360f;
        [SerializeField, Min(0.01f)] private float trapFireballScale = 0.35f;
        [SerializeField, Min(0.1f)] private float trapDamageRadius = 3.75f;
        [SerializeField, Min(0)] private float trapDamagePerSecond = 20f;
        [SerializeField, Min(0)] private float trapImpactDamage = 50f;
        [SerializeField, Min(0.05f)] private float trapDamageTickInterval = 0.25f;

        private enum TrapPhase { Waiting, Falling }
        private class MagicTrapState
        {
            public Transform owner;
            public Vector3 position;
            public GameObject visual;
            public GameObject areaMarker;
            public TrapPhase phase;
            public float remaining;
            public float damageTimer;
            public float spinAngle;
        }
        private readonly Dictionary<Transform, float> magicTrapCooldowns = new Dictionary<Transform, float>();
        private readonly List<MagicTrapState> magicTraps = new List<MagicTrapState>();

        private void UpdateMagicTraps(float dt)
        {
            if (experience == null || magicTrapPrefab == null || magicTrapSmokePrefab == null ||
                trapFireballPrefab == null || trapPortalPrefab == null) return;
            int existingCount = magicTraps.Count;
            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                if (unit == null || !unit.gameObject.activeSelf || allies[unit].IsDead || !experience.HasMagicTrap(unit)) continue;
                float remaining = magicTrapCooldowns.TryGetValue(unit, out float time) ? time : magicTrapInterval;
                remaining -= dt / experience.SkillCooldownMultiplier(unit);
                if (remaining <= 0)
                {
                    PlaceMagicTrap(unit);
                    remaining = magicTrapInterval;
                }
                magicTrapCooldowns[unit] = remaining;
            }

            for (int i = existingCount - 1; i >= 0; i--)
            {
                MagicTrapState trap = magicTraps[i];
                if (trap.phase == TrapPhase.Waiting)
                {
                    trap.remaining -= dt;
                    if (trap.remaining <= 0)
                    {
                        RemoveTrapVisual(trap);
                        magicTraps.RemoveAt(i);
                    }
                    else if (NearestEnemy(trap.position, magicTrapTriggerRadius) != null)
                    {
                        RemoveTrapVisual(trap);
                        trap.phase = TrapPhase.Falling;
                        trap.visual = Instantiate(trapFireballPrefab, trap.position + Vector3.up * trapFireballHeight,
                            Quaternion.Euler(90, 0, 0), combatRoot);
                        trap.visual.transform.localScale *= trapFireballScale;
                        trap.areaMarker = Instantiate(trapPortalPrefab, trap.position,
                            trapPortalPrefab.transform.rotation, combatRoot);
                        // 포탈은 낙하 피해의 범위 표시다. 자체 피해나 착탄 후 장판은 없다.
                        trap.areaMarker.transform.localScale = Vector3.one * (trapDamageRadius / 0.75f);
                        foreach (ParticleSystem particle in trap.visual.GetComponentsInChildren<ParticleSystem>(true))
                        {
                            var main = particle.main;
                            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                            main.simulationSpace = ParticleSystemSimulationSpace.Local;
                        }
                    }
                    continue;
                }
                Transform fireball = trap.visual.transform;
                float height = Mathf.Max(0, fireball.position.y - trap.position.y);
                float fallingTime = Mathf.Min(dt, height / trapFireballSpeed);
                float nextHeight = Mathf.Max(0, height - trapFireballSpeed * fallingTime);
                fireball.position = trap.position + Vector3.up * nextHeight;
                trap.spinAngle = Mathf.Repeat(trap.spinAngle + trapFireballRotationSpeed * fallingTime, 360f);
                fireball.rotation = Quaternion.Euler(90, 0, trap.spinAngle);
                trap.damageTimer += fallingTime;
                while (trap.damageTimer >= trapDamageTickInterval)
                {
                    trap.damageTimer -= trapDamageTickInterval;
                    DamageTrapArea(trap, trapDamagePerSecond * trapDamageTickInterval);
                }
                if (nextHeight <= 0.0001f)
                {
                    // 마지막 프레임은 실제 낙하한 시간만 계산한다.
                    if (trap.damageTimer > 0) DamageTrapArea(trap, trapDamagePerSecond * trap.damageTimer);
                    DamageTrapArea(trap, trapImpactDamage);
                    PlayTrapImpact(trap.position);
                    RemoveTrapVisual(trap);
                    magicTraps.RemoveAt(i);
                }
            }
        }

        private void PlayTrapImpact(Vector3 position)
        {
            if (trapImpactEffectPrefab == null) return;
            GameObject effect = Instantiate(trapImpactEffectPrefab, position,
                trapImpactEffectPrefab.transform.rotation, combatRoot);
            float lifetime = 0;
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.loop = false;
                lifetime = Mathf.Max(lifetime, main.startDelay.constantMax + main.duration + main.startLifetime.constantMax);
            }
            Destroy(effect, Mathf.Max(1f, lifetime + 0.5f));
        }

        private void PlaceMagicTrap(Transform unit)
        {
            EnemyState target = NearestEnemy(unit.position, 12f);
            Vector3 direction = target != null ? target.root.position - unit.position : unit.forward;
            direction.y = 0;
            Vector3 position = unit.position + direction.normalized * 2f;
            Bounds ground = run.Ground.bounds;
            position.x = Mathf.Clamp(position.x, ground.min.x + 1f, ground.max.x - 1f);
            position.z = Mathf.Clamp(position.z, ground.min.z + 1f, ground.max.z - 1f);
            position.y = run.Commander.position.y + 0.05f;
            magicTraps.Add(new MagicTrapState
            {
                owner = unit, position = position, remaining = magicTrapLifetime,
                visual = Instantiate(magicTrapPrefab, position, magicTrapPrefab.transform.rotation, combatRoot)
            });
            var smoke = Instantiate(magicTrapSmokePrefab, position, magicTrapSmokePrefab.transform.rotation, combatRoot);
            Destroy(smoke, 3f);
        }

        private void DamageTrapArea(MagicTrapState trap, float damage)
        {
            foreach (EnemyState enemy in enemies)
                if (enemy.root != null && enemy.health > 0 && FlatDistance(trap.position, enemy.root.position) <= trapDamageRadius)
                    ApplyDamage(enemy, damage, (enemy.root.position - trap.position).normalized, 0, false, trap.owner);
        }

        private static void RemoveTrapVisual(MagicTrapState trap)
        {
            if (trap.visual != null)
            {
                trap.visual.SetActive(false);
                Destroy(trap.visual);
                trap.visual = null;
            }
            if (trap.areaMarker != null)
            {
                trap.areaMarker.SetActive(false);
                Destroy(trap.areaMarker);
                trap.areaMarker = null;
            }
        }
    }
}
