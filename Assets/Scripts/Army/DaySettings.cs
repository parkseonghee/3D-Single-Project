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

        [Header("전투 시간에 따른 몬스터 웨이브 (0 = 시작, 1 = 종료)")]
        public Wave[] waves =
        {
            new Wave { startProgress = 0, intervalMultiplier = 1, batchSize = 1, capacityMultiplier = 1 },
            new Wave { startProgress = 0.25f, intervalMultiplier = 0.8f, batchSize = 2, capacityMultiplier = 2 },
            new Wave { startProgress = 0.5f, intervalMultiplier = 0.6f, batchSize = 3, capacityMultiplier = 3 },
            new Wave { startProgress = 0.75f, intervalMultiplier = 0.4f, batchSize = 5, capacityMultiplier = 5 }
        };
        [Min(1)] public int enemySafetyLimit = 120;

        public int GetWaveLimit(int day) => day <= 10 ? 2 : day <= 20 ? 3 : 4;

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
