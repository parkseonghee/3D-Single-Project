using UnityEngine;

namespace ArmySurvivor.Army
{
    public sealed class AdventureRhythmRule : IWaveRule
    {
        private static readonly int[] SpawnArcSlots = { 0, -2, 2, -3, 3, -1, 1 };
        public enum Phase { Warmup, Swarm, Reward, Tactical, Special, Rest, Finale, Boss }
        public enum SpecialPattern { Encirclement, TwinFront, FanCharge, RearAmbush, ClosingRing,
            CrossPressure, SpiralEntry, DecoyAndMain, EliteEscort, RangedSiege, PursuitTrain,
            AlternatingFlanks, EscapeGap, CommanderHunt, SurroundThenCharge, EliteReinforcement }
        public struct SpecialOrder
        {
            public float delay;
            public Vector3 direction;
            public bool ring, hunter, ranged, elite;
            public float hold, speed, health, radius;
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static SpecialPattern? TestPatternOverride;
#endif
        private readonly DaySettings settings;
        private readonly int day, waveLimit;
        private readonly float duration, interval, heading;
        private readonly int capacity;
        private readonly EnemyDefinition weakest;
        private readonly AdventureDayWaveRule mixed;
        private SpecialPattern specialPattern;
        public Phase CurrentPhase { get; private set; }
        public int Number { get; private set; }
        public bool SpecialWaveActive => CurrentPhase == Phase.Special;
        public int SpecialWaveCount => Mathf.Max(1, settings.specialWaveCount);
        public int Day => day;
        public float SpecialEliteHealthMultiplier => Mathf.Max(1f, settings.specialEliteHealthMultiplier);
        public float SpecialHealthMultiplier => settings.specialWaveHealthMultiplier;
        public float SpecialDamageMultiplier => settings.specialWaveDamageMultiplier;
        public float SpecialWarningSeconds => Mathf.Clamp(settings.specialWaveWarningSeconds, 0,
            duration * Mathf.Max(.01f, settings.specialWaveDurationProgress) * .8f);
        public float EncirclementSpawnRadius => Mathf.Max(3, settings.encirclementSpawnRadius);
        public float EncirclementClosingSpeed => Mathf.Max(.1f, settings.encirclementClosingSpeed);
        public float EncirclementReleaseRadius => Mathf.Clamp(settings.encirclementReleaseRadius, .5f, EncirclementSpawnRadius - .5f);
        public SpecialPattern SelectedSpecialPattern => specialPattern;
        public string PatternName => SpecialWaveActive ? SpecialName(specialPattern) :
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
            specialPattern = forcedSpecialPattern ?? PickSpecialPattern(day);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!forcedSpecialPattern.HasValue && TestPatternOverride.HasValue) specialPattern = TestPatternOverride.Value;
#endif
            if (pool != null) foreach (var entry in pool)
                if (entry != null && entry.firstDay <= day && entry.enemy != null && entry.enemy.prefab != null && entry.weight > 0 &&
                    (weakest == null || entry.enemy.health < weakest.health)) weakest = entry.enemy;
            mixed = new AdventureDayWaveRule(fallback, interval, capacity, pool, duration, null, settings.enemySafetyLimit, waveLimit, day);
        }
        private static SpecialPattern PickSpecialPattern(int day)
        {
            // The first four days use simpler formations; later days expand the draw pool.
            int count = day < 5 ? 5 : day < 11 ? 11 : 16;
            return (SpecialPattern)Random.Range(0, count);
        }
        public static string SpecialName(SpecialPattern pattern)
        {
            switch (pattern)
            {
                case SpecialPattern.Encirclement: return "원형 포위";
                case SpecialPattern.TwinFront: return "양면 동시 돌격";
                case SpecialPattern.FanCharge: return "부채꼴 돌격";
                case SpecialPattern.RearAmbush: return "후방 기습";
                case SpecialPattern.ClosingRing: return "좁혀오는 고리";
                case SpecialPattern.CrossPressure: return "십자 압박";
                case SpecialPattern.SpiralEntry: return "나선 진입";
                case SpecialPattern.DecoyAndMain: return "미끼와 본대";
                case SpecialPattern.EliteEscort: return "정예 호위대";
                case SpecialPattern.RangedSiege: return "원거리 포위망";
                case SpecialPattern.PursuitTrain: return "추격 열차";
                case SpecialPattern.AlternatingFlanks: return "양쪽 교대 공격";
                case SpecialPattern.EscapeGap: return "틈새 돌파";
                case SpecialPattern.CommanderHunt: return "지휘관 사냥";
                case SpecialPattern.SurroundThenCharge: return "포위 후 돌격";
                default: return "정예 증원";
            }
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetPatternForTesting(SpecialPattern pattern) => specialPattern = pattern;
#endif
        public SpecialOrder GetSpecialOrder(int serial)
        {
            int count = SpecialWaveCount;
            int half = Mathf.Max(1, count / 2);
            float t = count <= 1 ? 0 : serial / (float)(count - 1);
            var order = new SpecialOrder { speed = 1f, health = 1f, radius = EncirclementSpawnRadius };
            float angle = heading + serial * 360f / count;
            switch (specialPattern)
            {
                case SpecialPattern.Encirclement: order.ring = true; break;
                case SpecialPattern.TwinFront: angle = heading + (serial % 2) * 180f + ((serial / 2) % 3 - 1) * 20f; break;
                case SpecialPattern.FanCharge: angle = heading + Mathf.Lerp(-75f, 75f, t); break;
                case SpecialPattern.RearAmbush: angle = heading + 180f + Mathf.Lerp(-55f, 55f, t); order.speed = 1.35f; break;
                case SpecialPattern.ClosingRing:
                    angle = heading + serial * 360f / count; order.ring = true;
                    order.radius += (serial / 4) * 2f; order.delay = (serial / 4) * .45f; break;
                case SpecialPattern.CrossPressure: angle = heading + (serial % 4) * 90f + ((serial / 4) % 3 - 1) * 12f; order.delay = (serial / 4) * .35f; break;
                case SpecialPattern.SpiralEntry: angle = heading + serial * 137.5f; order.delay = serial * .25f; break;
                case SpecialPattern.DecoyAndMain:
                    angle = heading + (serial < half ? 0 : 180) + Mathf.Lerp(-45f, 45f, (serial % half) / (float)Mathf.Max(1, half - 1));
                    order.delay = serial < half ? 0 : 2f; order.health = serial < half ? .55f : 1.25f; break;
                case SpecialPattern.EliteEscort:
                    angle = heading + (serial == 0 ? 0 : Mathf.Lerp(-65f, 65f, t));
                    order.elite = serial == 0; break;
                case SpecialPattern.RangedSiege:
                    angle = heading + serial * 360f / count; order.ring = serial >= half;
                    order.ranged = serial < half; order.radius += order.ranged ? 5f : 0; break;
                case SpecialPattern.PursuitTrain:
                    angle = heading + Mathf.Lerp(-25f, 25f, (serial % 4) / 3f);
                    order.delay = (serial / 4) * 1.1f; order.speed = 1.15f; break;
                case SpecialPattern.AlternatingFlanks:
                    angle = heading + (serial % 2 == 0 ? -90f : 90f) + ((serial / 2) % 3 - 1) * 18f;
                    order.delay = (serial / 2) * .45f; break;
                case SpecialPattern.EscapeGap:
                    angle = heading + 45f + serial * 270f / Mathf.Max(1, count - 1); order.ring = true; break;
                case SpecialPattern.CommanderHunt:
                    angle = heading + serial * 360f / count; order.hunter = true; order.speed = 1.4f; break;
                case SpecialPattern.SurroundThenCharge:
                    angle = heading + serial * 360f / count; order.ring = true; order.hold = 2.5f; order.speed = 3f; break;
                case SpecialPattern.EliteReinforcement:
                    angle = heading + (serial < half ? Mathf.Lerp(-45f, 45f, serial / (float)Mathf.Max(1, half - 1)) :
                        135f + Mathf.Lerp(-35f, 35f, (serial - half) / (float)Mathf.Max(1, count - half - 1)));
                    order.delay = serial < half ? 0 : 2f; order.elite = serial == half; break;
            }
            float radians = angle * Mathf.Deg2Rad;
            order.direction = new Vector3(Mathf.Cos(radians), 0, Mathf.Sin(radians));
            return order;
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
            return GetSpecialOrder(serial).direction;
        }
    }
}
