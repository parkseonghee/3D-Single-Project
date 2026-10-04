using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("기마병 화염 발굽")]
        [SerializeField] private GameObject flameHoovesPrefab;
        [SerializeField, Min(0)] private float flameHoovesDamage = 15f;
        [SerializeField, Min(0)] private float flameHoovesDamagePerLevel = 10f;
        [SerializeField, Min(0.1f)] private float flameHoovesDuration = 3f;
        [SerializeField, Min(0)] private float flameHoovesDurationPerLevel = 1f;
        [SerializeField, Min(0.1f)] private float flameHoovesWidth = 1.2f;

        private class FlameTrail
        {
            public Transform owner;
            public Vector3 start, end;
            public float remaining, damagePerSecond;
            public GameObject visual;
        }
        private readonly List<FlameTrail> flameTrails = new List<FlameTrail>();
        private float flameTrailTickTime;

        private void LeaveFlameTrail(Transform unit, MeleeAttack state, bool finish)
        {
            int level = experience != null ? experience.FlameHoovesLevel(unit) : 0;
            if (level == 0 || flameHoovesPrefab == null)
            { state.flameTrailPosition = unit.position; return; }
            Vector3 offset = unit.position - state.flameTrailPosition;
            offset.y = 0;
            float distance = offset.magnitude;
            while (distance >= 1f || (finish && distance > 0.05f))
            {
                float length = Mathf.Min(1f, distance);
                Vector3 start = state.flameTrailPosition;
                Vector3 end = start + offset.normalized * length;
                var visual = Instantiate(flameHoovesPrefab, (start + end) * 0.5f,
                    Quaternion.LookRotation(offset) * Quaternion.Euler(0, 90, 0), combatRoot);
                // 프리팹의 가로 불벽을 돌진 경로를 따라 눕혀 배치한다.
                visual.transform.localScale = new Vector3(length / 3f, 0.5f, flameHoovesWidth);
                foreach (var particle in visual.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particle.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    main.loop = true;
                }
                flameTrails.Add(new FlameTrail { owner = unit, start = start, end = end, visual = visual,
                    remaining = flameHoovesDuration + (level - 1) * flameHoovesDurationPerLevel,
                    damagePerSecond = flameHoovesDamage + (level - 1) * flameHoovesDamagePerLevel });
                state.flameTrailPosition = end;
                distance -= length;
            }
        }

        private void UpdateFlameTrails(float dt)
        {
            if (flameTrails.Count == 0) { flameTrailTickTime = 0; return; }
            flameTrailTickTime += dt;
            if (flameTrailTickTime < 0.25f) return;
            dt = flameTrailTickTime;
            flameTrailTickTime = 0;
            foreach (EnemyState enemy in enemies)
            {
                if (enemy.root == null || enemy.health <= 0) continue;
                float damage = 0;
                Transform owner = null;
                foreach (FlameTrail trail in flameTrails)
                {
                    Vector3 point = enemy.root.position;
                    point.y = trail.start.y;
                    Vector3 segment = trail.end - trail.start;
                    float t = Mathf.Clamp01(Vector3.Dot(point - trail.start, segment) / Mathf.Max(0.0001f, segment.sqrMagnitude));
                    if (Vector3.Distance(point, trail.start + segment * t) > flameHoovesWidth * 0.5f) continue;
                    float tick = trail.damagePerSecond * Mathf.Min(dt, trail.remaining);
                    // 겹친 불길은 가장 강한 하나만 적용한다.
                    if (tick > damage) { damage = tick; owner = trail.owner; }
                }
                if (damage > 0) ApplyDamage(enemy, damage, Vector3.zero, 0, false, owner);
            }
            for (int i = flameTrails.Count - 1; i >= 0; i--)
            {
                FlameTrail trail = flameTrails[i];
                trail.remaining -= dt;
                if (trail.remaining > 0) continue;
                if (trail.visual != null) { trail.visual.SetActive(false); Destroy(trail.visual); }
                flameTrails.RemoveAt(i);
            }
        }
    }
}
