using System;
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
        public float HealthScale(int day) => 1 + Mathf.Max(0, day - 1) * healthPerDay;
        public float DamageScale(int day) => 1 + Mathf.Max(0, day - 1) * damagePerDay;

        [Serializable]
        public class EnemySpawn
        {
            public EnemyDefinition enemy;
            [Min(0)] public float weight = 1;
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
            [Min(0)] public int bossSpecialReward = 1;
        }
        [Min(1)] public int finalDay = 100;
        [Min(1)] public int bossInterval = 10;
        public Stage[] stages;

        public Stage GetStage(int day)
        {
            Stage result = stages[0];
            foreach (Stage stage in stages)
                if (stage.fromDay <= day && stage.fromDay >= result.fromDay) result = stage;
            return result;
        }
    }
}
