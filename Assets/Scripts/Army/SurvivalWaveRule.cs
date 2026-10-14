using UnityEngine;

namespace ArmySurvivor.Army
{
    public interface IWaveRule
    {
        WaveState Evaluate(float elapsed, bool bossAlive);
        EnemyDefinition ChooseEnemy(float elapsed);
    }

    public struct WaveState
    {
        public int number;
        public float interval;
        public int batch;
        public int capacity;
    }

    public sealed class SurvivalWaveRule : IWaveRule
    {
        private readonly SurvivalSettings settings;
        private readonly EnemyDefinition[,] definitions;
        public SurvivalWaveRule(SurvivalSettings settings, EnemyDefinition[,] definitions)
        { this.settings = settings; this.definitions = definitions; }

        public WaveState Evaluate(float elapsed, bool bossAlive)
        {
            var band = settings.Band(elapsed);
            if (band == null) return new WaveState { number = 1, interval = 2.5f, batch = 1, capacity = 20 };
            float extraMinutes = Mathf.Max(0, elapsed - band.startSeconds) / 60f;
            int batch = band.batchSize + (settings.batchIncreaseEveryMinutes > 0
                ? Mathf.FloorToInt(extraMinutes / settings.batchIncreaseEveryMinutes) : 0);
            if (bossAlive) batch = Mathf.Max(1, Mathf.CeilToInt(batch * settings.spawnStrengthDuringBoss));
            return new WaveState {
                number = settings.Tier(elapsed),
                interval = Mathf.Max(settings.minimumSpawnInterval, band.interval / (1 + extraMinutes * settings.intervalDecreasePerMinute)),
                batch = Mathf.Max(1, batch),
                capacity = Mathf.Clamp(band.capacity + Mathf.FloorToInt(extraMinutes * settings.capacityIncreasePerMinute), 1, settings.absoluteCapacity)
            };
        }

        public EnemyDefinition ChooseEnemy(float elapsed)
        {
            int tier = settings.Tier(elapsed) - 1;
            float total = 0;
            for (int i = 0; i < settings.monsters.Length; i++)
                if (settings.monsters[i] != null && settings.monsters[i].firstAppearanceSeconds <= elapsed && definitions[i, tier] != null)
                    total += Mathf.Max(0, settings.monsters[i].weight);
            if (total <= 0) return definitions[0, tier];
            float roll = Random.value * total;
            for (int i = 0; i < settings.monsters.Length; i++)
                if (settings.monsters[i] != null && settings.monsters[i].firstAppearanceSeconds <= elapsed && definitions[i, tier] != null)
                {
                    roll -= Mathf.Max(0, settings.monsters[i].weight);
                    if (roll <= 0) return definitions[i, tier];
                }
            return definitions[0, tier];
        }
    }
}
