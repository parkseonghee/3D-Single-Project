using System;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Survival Settings")]
    public sealed class SurvivalSettings : ScriptableObject
    {
        [Serializable] public sealed class Monster
        {
            public EnemyDefinition definition;
            [Min(0)] public float firstAppearanceSeconds;
            [Min(0)] public float weight = 1;
            [Min(1)] public float tier1Health = 60;
            [Min(1)] public float tier2Health = 90;
            [Min(1)] public float tier3Health = 140;
            public float Health(int tier) => tier <= 1 ? tier1Health : tier == 2 ? tier2Health : tier3Health;
        }

        [Serializable] public sealed class SpawnBand
        {
            [Min(0)] public float startSeconds;
            [Min(0.05f)] public float interval = 2.5f;
            [Min(1)] public int batchSize = 1;
            [Min(1)] public int capacity = 20;
        }

        [Header("몬스터 체력과 등장 시간")]
        public Monster[] monsters;
        public Monster elite;
        public Monster boss;
        [Header("시간과 난이도")]
        [Min(1)] public float tier2Seconds = 300;
        [Min(1)] public float tier3Seconds = 600;
        [Min(1)] public int adventureTier2Day = 11;
        [Min(1)] public int adventureTier3Day = 51;
        [Min(1)] public float scalingStartSeconds = 900;
        [Min(0)] public float healthIncreasePerMinute = 0.1f;
        public SpawnBand[] spawnBands;
        [Min(0)] public float intervalDecreasePerMinute = 0.02f;
        [Min(0)] public float capacityIncreasePerMinute = 1;
        [Min(0)] public float batchIncreaseEveryMinutes = 5;
        [Min(0.05f)] public float minimumSpawnInterval = 0.25f;
        [Min(1)] public int absoluteCapacity = 200;
        [Header("카메라 밖 스폰")]
        [Min(0)] public float minimumSpawnRadius = 15;
        [Min(0)] public float cameraOutsideMargin = 3;
        [Min(1)] public int spawnPositionAttempts = 16;
        [Header("정예 및 보스")]
        [Min(1)] public float eliteInterval = 60;
        public bool enableBoss = true;
        [Min(1)] public float firstBossSeconds = 900;
        [Min(1)] public float bossRepeatSeconds = 600;
        [Min(1)] public float bossHealthMultiplier = 1.5f;
        public GameObject bossPrefab;
        [Min(0.1f)] public float fallbackBossScale = 1.8f;
        public Color fallbackBossTint = Color.red;
        [Range(0.1f, 1)] public float spawnStrengthDuringBoss = 0.5f;
        [Header("점수")]
        [Min(0)] public int secondsScore = 10;
        [Min(0)] public int normalKillScore = 1;
        [Min(0)] public int eliteKillScore = 50;
        [Min(0)] public int bossKillScore = 500;

        public int Tier(float seconds) => seconds < tier2Seconds ? 1 : seconds < tier3Seconds ? 2 : 3;
        public int AdventureTier(int day) => day < adventureTier2Day ? 1 : day < adventureTier3Day ? 2 : 3;
        public float AdventureHealth(EnemyDefinition definition, int day)
        {
            if (definition == null) return 0;
            int tier = AdventureTier(day);
            if (boss != null && boss.definition != null && boss.definition.prefab == definition.prefab)
                return boss.Health(tier);
            if (monsters != null)
                foreach (Monster monster in monsters)
                    if (monster != null && monster.definition != null && monster.definition.prefab == definition.prefab)
                        return monster.Health(tier);
            return definition.health;
        }
        public float HealthMultiplier(float seconds) => 1 +
            Mathf.Max(0, seconds - scalingStartSeconds) / 60f * healthIncreasePerMinute;
        public SpawnBand Band(float seconds)
        {
            if (spawnBands == null || spawnBands.Length == 0) return null;
            SpawnBand selected = spawnBands[0];
            foreach (SpawnBand band in spawnBands)
                if (band != null && band.startSeconds <= seconds && band.startSeconds >= selected.startSeconds)
                    selected = band;
            return selected;
        }
    }
}
