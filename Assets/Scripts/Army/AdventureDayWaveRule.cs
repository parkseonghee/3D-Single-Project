using UnityEngine;

namespace ArmySurvivor.Army
{
    // The original Day wave thresholds and weighted draft, moved behind IWaveRule unchanged.
    public sealed class AdventureDayWaveRule : IWaveRule
    {
        private readonly EnemyDefinition fallback;
        private readonly DaySettings.EnemySpawn[] pool;
        private readonly DaySettings.Wave[] waves;
        private readonly float interval, duration;
        private readonly int limit, safetyLimit, waveLimit;

        public AdventureDayWaveRule(EnemyDefinition fallback, float interval, int limit,
            DaySettings.EnemySpawn[] pool, float duration, DaySettings.Wave[] waves, int safetyLimit, int waveLimit)
        {
            this.fallback = fallback;
            this.interval = interval;
            this.limit = limit;
            this.pool = pool;
            this.duration = Mathf.Max(1, duration);
            this.waves = waves;
            this.safetyLimit = Mathf.Max(1, safetyLimit);
            this.waveLimit = Mathf.Max(1, waveLimit);
        }

        public WaveState Evaluate(float elapsed, bool bossAlive)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            DaySettings.Wave selected = null;
            int number = 1;
            if (waves != null)
                for (int i = 0; i < Mathf.Min(waves.Length, waveLimit); i++)
                {
                    var wave = waves[i];
                    if (wave == null || wave.startProgress > progress) continue;
                    if (selected != null && wave.startProgress < selected.startProgress) continue;
                    selected = wave;
                    number = i + 1;
                }
            return new WaveState {
                number = number,
                interval = Mathf.Max(0.1f, interval * (selected == null ? 1 : selected.intervalMultiplier)),
                batch = Mathf.Max(1, selected == null ? 1 : selected.batchSize),
                capacity = Mathf.Clamp(Mathf.CeilToInt(limit *
                    (selected == null ? 1 : selected.capacityMultiplier)), 1, safetyLimit)
            };
        }

        public EnemyDefinition ChooseEnemy(float elapsed)
        {
            if (pool == null) return fallback;
            float total = 0;
            foreach (var entry in pool)
                if (entry != null && entry.enemy != null && entry.enemy.prefab != null)
                    total += Mathf.Max(0, entry.weight);
            if (total <= 0) return fallback;
            float roll = Random.value * total;
            foreach (var entry in pool)
            {
                if (entry == null || entry.enemy == null || entry.enemy.prefab == null || entry.weight <= 0) continue;
                roll -= entry.weight;
                if (roll <= 0) return entry.enemy;
            }
            return fallback;
        }
    }
}
