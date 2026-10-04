using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public partial class CombatController
    {
        [Header("방패병 위압의 함성")]
        [SerializeField] private GameObject shoutPrefab;
        [SerializeField] private GameObject brokenHeartPrefab;
        [SerializeField, Min(0.1f)] private float shoutInterval = 10f;
        [SerializeField, Min(0.1f)] private float shoutRadius = 7.8f;
        [SerializeField, Min(0.1f)] private float shoutAreaDuration = 3f;
        [SerializeField, Min(0.1f)] private float shoutDebuffDuration = 5f;
        [SerializeField, Min(0)] private float shoutArmorReduction = 20f;
        [SerializeField, Min(0)] private float brokenHeartFreezeTime = 1.15f;

        private class ShoutArea
        {
            public Vector3 position;
            public float remaining;
            public readonly HashSet<EnemyState> affected = new HashSet<EnemyState>();
        }
        private class ShoutDebuff
        {
            public float remaining;
            public float effectTime;
            public GameObject visual;
            public ParticleSystem particles;
        }
        private readonly Dictionary<Transform, float> shoutCooldowns = new Dictionary<Transform, float>();
        private readonly List<ShoutArea> shoutAreas = new List<ShoutArea>();
        private readonly Dictionary<EnemyState, ShoutDebuff> shoutDebuffs = new Dictionary<EnemyState, ShoutDebuff>();
        private readonly List<EnemyState> expiredShoutDebuffs = new List<EnemyState>();

        private void UpdateShouts(float dt)
        {
            int existingAreaCount = shoutAreas.Count;
            expiredShoutDebuffs.Clear();
            foreach (var entry in shoutDebuffs)
            {
                ShoutDebuff debuff = entry.Value;
                debuff.remaining -= dt;
                if (entry.Key.root == null || entry.Key.health <= 0 || debuff.remaining <= 0)
                { expiredShoutDebuffs.Add(entry.Key); continue; }
                if (debuff.particles != null && debuff.effectTime < brokenHeartFreezeTime)
                {
                    float step = Mathf.Min(dt, brokenHeartFreezeTime - debuff.effectTime);
                    debuff.effectTime += step;
                    // Simulate는 실행 후 일시정지 상태를 유지한다. 1.15초를 넘기지 않는다.
                    debuff.particles.Simulate(step, true, false, false);
                }
            }
            foreach (var enemy in expiredShoutDebuffs) ClearShoutDebuff(enemy);

            foreach (var entry in recruitment.Units)
            {
                Transform unit = entry.Key;
                if (unit == null || !unit.gameObject.activeSelf || experience == null ||
                    !experience.HasIntimidatingShout(unit) || !allies.TryGetValue(unit, out var health) || health.IsDead) continue;
                float remaining = shoutCooldowns.TryGetValue(unit, out float time) ? time : shoutInterval;
                remaining -= dt;
                if (remaining <= 0)
                {
                    shoutAreas.Add(new ShoutArea { position = unit.position, remaining = shoutAreaDuration });
                    if (shoutPrefab != null)
                    {
                        var effect = Instantiate(shoutPrefab, unit.position, shoutPrefab.transform.rotation, combatRoot);
                        effect.transform.localScale = Vector3.one * 3f;
                        Destroy(effect, 5f);
                    }
                    remaining = shoutInterval;
                }
                shoutCooldowns[unit] = remaining;
            }
            for (int i = shoutAreas.Count - 1; i >= 0; i--)
            {
                ShoutArea area = shoutAreas[i];
                foreach (EnemyState enemy in enemies)
                    if (enemy.root != null && enemy.health > 0 &&
                        FlatDistance(area.position, enemy.root.position) <= shoutRadius && area.affected.Add(enemy))
                        ApplyShoutDebuff(enemy);
                if (i < existingAreaCount) area.remaining -= dt;
                if (area.remaining <= 0) shoutAreas.RemoveAt(i);
            }
        }

        private void ApplyShoutDebuff(EnemyState enemy)
        {
            if (shoutDebuffs.TryGetValue(enemy, out var existing))
            { existing.remaining = shoutDebuffDuration; return; }
            var debuff = new ShoutDebuff { remaining = shoutDebuffDuration };
            if (brokenHeartPrefab != null)
            {
                debuff.visual = Instantiate(brokenHeartPrefab, enemy.root);
                debuff.visual.transform.localScale *= 0.5f;
                float height = 2f;
                foreach (var renderer in enemy.root.GetComponentsInChildren<Renderer>())
                    if (!renderer.transform.IsChildOf(debuff.visual.transform))
                        height = Mathf.Max(height, renderer.bounds.max.y - enemy.root.position.y + 0.4f);
                debuff.visual.transform.localPosition = Vector3.up * height;
                // 이펙트 자동 재생과 삭제를 끄고 전투 시간으로 제어한다.
                foreach (var script in debuff.visual.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
                debuff.particles = debuff.visual.GetComponent<ParticleSystem>();
                foreach (var particle in debuff.visual.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particle.main;
                    main.loop = false;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                    main.simulationSpace = ParticleSystemSimulationSpace.Local;
                    main.stopAction = ParticleSystemStopAction.None;
                }
                debuff.particles.Simulate(0, true, true, false);
            }
            shoutDebuffs.Add(enemy, debuff);
        }

        private float ShoutArmorMultiplier(EnemyState enemy)
        {
            float armor = enemy.definition.armor - (shoutDebuffs.ContainsKey(enemy) ? shoutArmorReduction : 0);
            return 100f / (100f + Mathf.Max(-80f, armor));
        }
        private void ClearShoutDebuff(EnemyState enemy)
        {
            if (!shoutDebuffs.TryGetValue(enemy, out var debuff)) return;
            if (debuff.visual != null) { debuff.visual.SetActive(false); Destroy(debuff.visual); }
            shoutDebuffs.Remove(enemy);
        }
        private void ClearShouts()
        {
            foreach (var debuff in shoutDebuffs.Values)
                if (debuff.visual != null) { debuff.visual.SetActive(false); Destroy(debuff.visual); }
            shoutDebuffs.Clear(); shoutAreas.Clear(); shoutCooldowns.Clear(); expiredShoutDebuffs.Clear();
        }
    }
}
