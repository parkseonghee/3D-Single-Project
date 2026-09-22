using System;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Day Settings")]
    public class DaySettings : ScriptableObject
    {
        [Serializable]
        public class Stage
        {
            [Min(1)] public int fromDay = 1;
            [Min(1)] public float survivalSeconds = 90;
            public EnemyDefinition enemy;
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
