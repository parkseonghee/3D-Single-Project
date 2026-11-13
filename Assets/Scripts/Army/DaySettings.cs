using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Day Settings")]
    public class DaySettings : ScriptableObject
    {
        [Serializable]
        public class Wave
        {
            [Range(0, 1)] public float startProgress;
            [Min(0.05f)] public float intervalMultiplier = 1;
            [Min(1)] public int batchSize = 1;
            [Min(1)] public float capacityMultiplier = 1;
        }

        [Header("구형 웨이브 호환 데이터 (현재 모험은 리듬 규칙 사용)")]
        public Wave[] waves =
        {
            new Wave { startProgress = 0, intervalMultiplier = 1, batchSize = 1, capacityMultiplier = 1 },
            new Wave { startProgress = 0.25f, intervalMultiplier = 0.8f, batchSize = 2, capacityMultiplier = 2 },
            new Wave { startProgress = 0.5f, intervalMultiplier = 0.6f, batchSize = 3, capacityMultiplier = 3 },
            new Wave { startProgress = 0.75f, intervalMultiplier = 0.4f, batchSize = 5, capacityMultiplier = 5 }
        };
        [Min(1)] public int enemySafetyLimit = 120;

        public int GetWaveLimit(int day) => day <= 10 ? 2 : day <= 20 ? 3 : 4;

        [Header("모험 웨이브 리듬 / Day 1 기준 가산 성장")]
        [Min(0)] public float healthPerDay = .04f;
        [Min(0)] public float damagePerDay = .02f;
        [Range(.1f, 1)] public float swarmHealthMultiplier = .55f;
        [Range(.1f, 1)] public float swarmDamageMultiplier = .65f;
        [Min(1)] public int swarmBatch = 4;
        [Min(.1f)] public float swarmInterval = 2f;
        [Min(1)] public int swarmCapacity = 24;
        [Min(1)] public int splitUnlockDay = 5;
        [Min(1)] public int pincerUnlockDay = 11;
        [Min(1)] public int surroundUnlockDay = 21;
        [Header("모험 적 생성 분산")]
        [Range(0, 160)] public float spawnArcDegrees = 90;
        [Min(0)] public float spawnSeparation = 3.5f;
        [Header("모험 특별 웨이브 (하루 한 번)")]
        [Range(.45f, .6f)] public float specialWaveStartProgress = .5f;
        [Range(.04f, .15f)] public float specialWaveDurationProgress = .1f;
        [Min(1)] public int specialWaveCount = 12;
        [Range(.1f, 1f)] public float specialWaveHealthMultiplier = .7f;
        [Range(.1f, 1f)] public float specialWaveDamageMultiplier = .8f;
        [Min(0)] public float specialWaveWarningSeconds = 1.25f;
        [Min(3f)] public float encirclementSpawnRadius = 11f;
        [Min(.1f)] public float encirclementClosingSpeed = 1.5f;
        [Min(.5f)] public float encirclementReleaseRadius = 3.5f;
        [Min(1f)] public float specialEliteHealthMultiplier = 3f;
        public float HealthScale(int day) => 1 + Mathf.Max(0, day - 1) * healthPerDay;
        public float DamageScale(int day) => 1 + Mathf.Max(0, day - 1) * damagePerDay;

        [Serializable]
        public class EnemySpawn
        {
            public EnemyDefinition enemy;
            [Min(0)] public float weight = 1;
            [Min(1)] public int firstDay = 1;
        }

        [Serializable]
        public class Stage
        {
            [Min(1)] public int fromDay = 1;
            [Min(1)] public float survivalSeconds = 90;
            public EnemyDefinition enemy;
            public EnemySpawn[] enemies;
            public EnemyDefinition boss;
            [Min(0.1f)] public float spawnInterval = 2.5f;
            [Min(1)] public int maximumEnemies = 12;
            [Tooltip("구형 호환 값. 현재는 Day 1~10: 2, 11~20: 3, 21 이후: 4로 결정")]
            [Min(1)] public int availableWaveCount = 4;
            [Min(0)] public int goldReward = 100;
        }
        [Min(1)] public int finalDay = 30;
        [Min(1)] public int bossInterval = 10;
        public Stage[] stages;

        public Stage GetStage(int day)
        {
            Stage result = stages[0];
            foreach (Stage stage in stages)
                if (stage.fromDay <= day && stage.fromDay >= result.fromDay) result = stage;
            return result;
        }

        [Header("출전 미리보기 / 전장 변수 (하루 1개)")]
        [Min(1)] public int firstModifierDay = 2;
        [Tooltip("값을 바꾸면 Day별 변수 배정이 다시 섞인다.")]
        public int modifierSeed = 7;
        public BattlefieldModifierDefinition[] battlefieldModifiers;

        // 같은 Day는 화면 재진입·저장/불러오기와 무관하게 항상 같은 변수를 고른다.
        public BattlefieldModifierDefinition GetBattlefieldModifier(int day)
        {
            if (day < firstModifierDay || battlefieldModifiers == null) return null;
            Stage stage = GetStage(day);
            int count = 0;
            foreach (var modifier in battlefieldModifiers)
                if (modifier != null && modifier.IsAvailable(stage, day)) count++;
            if (count == 0) return null;
            int pick = (int)(Mix((uint)day * 0x9E3779B1u + (uint)modifierSeed) % (uint)count);
            BattlefieldModifierDefinition chosen = Available(stage, day, pick);
            // 이틀 연속 같은 변수가 나오지 않게 전날 결과와 같으면 다음 후보를 쓴다.
            if (count > 1 && day > firstModifierDay && chosen == GetBattlefieldModifier(day - 1))
                chosen = Available(stage, day, (pick + 1) % count);
            return chosen;
        }

        private BattlefieldModifierDefinition Available(Stage stage, int day, int index)
        {
            foreach (var modifier in battlefieldModifiers)
            {
                if (modifier == null || !modifier.IsAvailable(stage, day)) continue;
                if (index-- == 0) return modifier;
            }
            return null;
        }

        // 정수 해시(murmur3 finalizer). 연속된 Day 값도 고르게 흩어진다.
        private static uint Mix(uint h)
        {
            h ^= h >> 16; h *= 0x85EBCA6Bu; h ^= h >> 13; h *= 0xC2B2AE35u; h ^= h >> 16;
            return h;
        }

        public struct EnemyShare
        {
            public EnemyDefinition enemy;
            public float share;
            public bool boosted;
        }

        // 전투와 같은 규칙(firstDay, 가중치, 변수 배율)으로 그날 일반 적 출현 비율을 계산한다.
        public void GetEnemyShares(int day, BattlefieldModifierDefinition modifier, List<EnemyShare> results)
        {
            results.Clear();
            Stage stage = GetStage(day);
            float total = 0;
            if (stage.enemies != null)
                foreach (EnemySpawn entry in stage.enemies)
                {
                    if (entry == null || entry.enemy == null || entry.firstDay > day || entry.weight <= 0) continue;
                    float multiplier = modifier != null ? modifier.WeightMultiplier(entry.enemy) : 1f;
                    float weight = entry.weight * multiplier;
                    if (weight <= 0) continue;
                    total += weight;
                    results.Add(new EnemyShare { enemy = entry.enemy, share = weight, boosted = multiplier > 1f });
                }
            if (total <= 0)
            {
                results.Clear();
                if (stage.enemy != null) results.Add(new EnemyShare { enemy = stage.enemy, share = 1 });
                return;
            }
            for (int i = 0; i < results.Count; i++)
            {
                EnemyShare item = results[i];
                item.share /= total;
                results[i] = item;
            }
            results.Sort((a, b) => b.share.CompareTo(a.share));
        }
    }
}
