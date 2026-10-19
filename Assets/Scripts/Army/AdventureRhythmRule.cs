using UnityEngine;

namespace ArmySurvivor.Army
{
    public sealed class AdventureRhythmRule : IWaveRule
    {
        public enum Phase { Warmup, Swarm, Reward, Tactical, Rest, Finale, Boss }
        private readonly DaySettings settings;
        private readonly int day, waveLimit;
        private readonly float duration, interval, heading;
        private readonly int capacity;
        private readonly EnemyDefinition weakest;
        private readonly AdventureDayWaveRule mixed;
        public Phase CurrentPhase { get; private set; }
        public int Number { get; private set; }
        public string PatternName => Pattern == 3 ? "포위" : Pattern == 2 ? "양면 협공" : Pattern == 1 ? "갈라진 전선" : "단일 전선";
        private int Pattern => CurrentPhase == Phase.Warmup ? 0 :
            day >= settings.surroundUnlockDay && Number >= 3 ? 3 :
            day >= settings.pincerUnlockDay && Number >= 2 ? 2 :
            day >= settings.splitUnlockDay ? 1 : 0;
        public string Label => CurrentPhase == Phase.Warmup ? "탐색" : CurrentPhase == Phase.Reward ? "보상 · 중간 보스" :
            CurrentPhase == Phase.Rest ? "정비 · 경험치 회수" : CurrentPhase == Phase.Tactical ? "전술 웨이브" :
            CurrentPhase == Phase.Boss ? "최종 결전" : CurrentPhase == Phase.Finale ? "최종 섬멸" : "섬멸 웨이브";
        public float HealthMultiplier => IsSwarm ? settings.swarmHealthMultiplier : 1;
        public float DamageMultiplier => IsSwarm ? settings.swarmDamageMultiplier : 1;
        private bool IsSwarm => CurrentPhase == Phase.Swarm || CurrentPhase == Phase.Finale;

        public AdventureRhythmRule(EnemyDefinition fallback, DaySettings.EnemySpawn[] pool,
            float duration, float interval, int capacity, DaySettings settings, int day)
        {
            this.settings = settings; this.day = day; this.duration = Mathf.Max(1, duration);
            this.interval = interval; this.capacity = capacity; waveLimit = settings.GetWaveLimit(day);
            heading = Random.Range(0f, 360f); weakest = fallback;
            if (pool != null) foreach (var entry in pool)
                if (entry != null && entry.enemy != null && entry.enemy.prefab != null && entry.weight > 0 &&
                    (weakest == null || entry.enemy.health < weakest.health)) weakest = entry.enemy;
            mixed = new AdventureDayWaveRule(fallback, interval, capacity, pool, duration, null, settings.enemySafetyLimit, waveLimit);
        }
        public WaveState Evaluate(float elapsed, bool bossAlive)
        {
            float p = elapsed / duration;
            CurrentPhase = p < 1f / 6 ? Phase.Warmup : p < 1f / 3 ? Phase.Swarm :
                p < 4f / 9 ? Phase.Reward : p < 2f / 3 ? Phase.Tactical :
                p < 7f / 9 ? Phase.Rest : p < 1 ? Phase.Finale : Phase.Boss;
            // Later days introduce additional fronts during the tactical/final assault.
            Number = p < 4f / 9 ? 1 : p < 7f / 9 ? 2 :
                waveLimit == 4 && p < 8f / 9 ? 3 : waveLimit;
            bool quiet = CurrentPhase == Phase.Reward || CurrentPhase == Phase.Rest || CurrentPhase == Phase.Boss;
            return new WaveState {
                number = Number,
                interval = IsSwarm ? settings.swarmInterval : Mathf.Max(.1f, interval),
                batch = quiet ? 0 : IsSwarm ? settings.swarmBatch + (CurrentPhase == Phase.Finale ? 2 : 0) : CurrentPhase == Phase.Warmup ? 1 : 2,
                capacity = Mathf.Clamp(IsSwarm ? settings.swarmCapacity + (waveLimit - 2) * 4 : capacity, 1, settings.enemySafetyLimit)
            };
        }
        public EnemyDefinition ChooseEnemy(float elapsed) => IsSwarm || CurrentPhase == Phase.Warmup ? weakest : mixed.ChooseEnemy(elapsed);
        public Vector3 Direction(int serial)
        {
            float angle = heading + (Number - 1) * 75;
            if (Pattern == 1) angle += serial % 2 == 0 ? -40 : 40;
            else if (Pattern == 2) angle += (serial % 2) * 180;
            else if (Pattern == 3) angle += (serial % 4) * 90;
            angle += Random.Range(-12f, 12f);
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
        }
    }
}
