using UnityEngine;

namespace ArmySurvivor.Army
{
    public sealed class AdventureRhythmRule : IWaveRule
    {
        private static readonly int[] SpawnArcSlots = { 0, -2, 2, -3, 3, -1, 1 };
        public enum Phase { Warmup, Swarm, Reward, Tactical, Special, Rest, Finale, Boss }
        public enum SpecialPattern { Encirclement, TwinFront, FanCharge }
        private readonly DaySettings settings;
        private readonly int day, waveLimit;
        private readonly float duration, interval, heading;
        private readonly int capacity;
        private readonly EnemyDefinition weakest;
        private readonly AdventureDayWaveRule mixed;
        private readonly SpecialPattern specialPattern;
        public Phase CurrentPhase { get; private set; }
        public int Number { get; private set; }
        public bool SpecialWaveActive => CurrentPhase == Phase.Special;
        public int SpecialWaveCount => Mathf.Max(1, settings.specialWaveCount);
        public float SpecialWarningSeconds => Mathf.Clamp(settings.specialWaveWarningSeconds, 0,
            duration * Mathf.Max(.01f, settings.specialWaveDurationProgress) * .8f);
        public float EncirclementSpawnRadius => Mathf.Max(3, settings.encirclementSpawnRadius);
        public float EncirclementClosingSpeed => Mathf.Max(.1f, settings.encirclementClosingSpeed);
        public float EncirclementReleaseRadius => Mathf.Clamp(settings.encirclementReleaseRadius, .5f, EncirclementSpawnRadius - .5f);
        public SpecialPattern SelectedSpecialPattern => specialPattern;
        public string PatternName => SpecialWaveActive ? specialPattern == SpecialPattern.Encirclement ? "원형 포위" :
            specialPattern == SpecialPattern.TwinFront ? "양면 동시 돌격" : "부채꼴 돌격" :
            Pattern == 3 ? "포위" : Pattern == 2 ? "양면 협공" : Pattern == 1 ? "갈라진 전선" : "단일 전선";
        private int Pattern => CurrentPhase == Phase.Warmup ? 0 :
            day >= settings.surroundUnlockDay && Number >= 3 ? 3 :
            day >= settings.pincerUnlockDay && Number >= 2 ? 2 :
            day >= settings.splitUnlockDay ? 1 : 0;
        public string Label => CurrentPhase == Phase.Warmup ? "탐색" : CurrentPhase == Phase.Reward ? "보상 · 중간 보스" :
            CurrentPhase == Phase.Rest ? "정비 · 경험치 회수" : CurrentPhase == Phase.Tactical ? "전술 웨이브" :
            CurrentPhase == Phase.Special ? "특별 웨이브" :
            CurrentPhase == Phase.Boss ? "최종 결전" : CurrentPhase == Phase.Finale ? "최종 섬멸" : "섬멸 웨이브";
        public float HealthMultiplier => SpecialWaveActive ? settings.specialWaveHealthMultiplier : IsSwarm ? settings.swarmHealthMultiplier : 1;
        public float DamageMultiplier => SpecialWaveActive ? settings.specialWaveDamageMultiplier : IsSwarm ? settings.swarmDamageMultiplier : 1;
        public float SpawnSeparation => settings.spawnSeparation;
        private bool IsSwarm => CurrentPhase == Phase.Swarm || CurrentPhase == Phase.Finale;

        public AdventureRhythmRule(EnemyDefinition fallback, DaySettings.EnemySpawn[] pool,
            float duration, float interval, int capacity, DaySettings settings, int day,
            SpecialPattern? forcedSpecialPattern = null)
        {
            this.settings = settings; this.day = day; this.duration = Mathf.Max(1, duration);
            this.interval = interval; this.capacity = capacity; waveLimit = settings.GetWaveLimit(day);
            heading = Random.Range(0f, 360f); weakest = fallback;
            // Other formations stay available for later work; this pass presents the ring alone.
            specialPattern = forcedSpecialPattern ?? SpecialPattern.Encirclement;
            if (pool != null) foreach (var entry in pool)
                if (entry != null && entry.firstDay <= day && entry.enemy != null && entry.enemy.prefab != null && entry.weight > 0 &&
                    (weakest == null || entry.enemy.health < weakest.health)) weakest = entry.enemy;
            mixed = new AdventureDayWaveRule(fallback, interval, capacity, pool, duration, null, settings.enemySafetyLimit, waveLimit, day);
        }
        public WaveState Evaluate(float elapsed, bool bossAlive)
        {
            float p = elapsed / duration;
            CurrentPhase = p < 1f / 6 ? Phase.Warmup : p < 1f / 3 ? Phase.Swarm :
                p < 4f / 9 ? Phase.Reward : p < 2f / 3 ? Phase.Tactical :
                p < 7f / 9 ? Phase.Rest : p < 1 ? Phase.Finale : Phase.Boss;
            float specialStart = Mathf.Clamp(settings.specialWaveStartProgress, 4f / 9f, .6f);
            float specialEnd = Mathf.Min(2f / 3f, specialStart + Mathf.Max(.01f, settings.specialWaveDurationProgress));
            if (p >= specialStart && p < specialEnd) CurrentPhase = Phase.Special;
            // Later days introduce additional fronts during the tactical/final assault.
            Number = p < 4f / 9 ? 1 : p < 7f / 9 ? 2 :
                waveLimit == 4 && p < 8f / 9 ? 3 : waveLimit;
            bool quiet = CurrentPhase == Phase.Reward || CurrentPhase == Phase.Special || CurrentPhase == Phase.Rest || CurrentPhase == Phase.Boss;
            return new WaveState {
                number = Number,
                interval = IsSwarm ? settings.swarmInterval : Mathf.Max(.1f, interval),
                batch = quiet ? 0 : IsSwarm ? settings.swarmBatch + (CurrentPhase == Phase.Finale ? 2 : 0) : CurrentPhase == Phase.Warmup ? 1 : 2,
                capacity = Mathf.Clamp(SpecialWaveActive ? capacity + SpecialWaveCount :
                    IsSwarm ? settings.swarmCapacity + (waveLimit - 2) * 4 : capacity, 1, settings.enemySafetyLimit)
            };
        }
        public EnemyDefinition ChooseEnemy(float elapsed) => IsSwarm || CurrentPhase == Phase.Warmup ? weakest : mixed.ChooseEnemy(elapsed);
        public Vector3 Direction(int serial)
        {
            if (SpecialWaveActive) return SpecialDirection(serial, SpecialWaveCount);
            float angle = heading + (Number - 1) * 75;
            int fronts = Pattern == 3 ? 4 : Pattern == 0 ? 1 : 2;
            int front = serial % fronts;
            if (Pattern == 1) angle += front == 0 ? -40 : 40;
            else if (Pattern == 2) angle += front * 180;
            else if (Pattern == 3) angle += front * 90;
            // Cycle through a wide arc in a dispersed order, including within one spawn batch.
            angle += SpawnArcSlots[(serial / fronts) % SpawnArcSlots.Length] * settings.spawnArcDegrees / 6f;
            angle += Random.Range(-5f, 5f);
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
        }
        public Vector3 SpecialDirection(int serial, int count)
        {
            count = Mathf.Max(1, count);
            float angle;
            if (specialPattern == SpecialPattern.Encirclement)
                angle = heading + serial * 360f / count;
            else if (specialPattern == SpecialPattern.TwinFront)
            {
                int side = serial % 2;
                int slot = serial / 2;
                int sideCount = side == 0 ? (count + 1) / 2 : count / 2;
                float offset = sideCount <= 1 ? 0 : Mathf.Lerp(-35f, 35f, slot / (float)(sideCount - 1));
                angle = heading + side * 180f + offset;
            }
            else angle = heading + (count <= 1 ? 0 : Mathf.Lerp(-75f, 75f, serial / (float)(count - 1)));
            float radians = angle * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
        }
    }
}
