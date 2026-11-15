using System;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Survival Settings")]
    public sealed class SurvivalSettings : ScriptableObject
    {
        [Serializable] public sealed class Tradeoff
        {
            public string title;
            [TextArea(2, 4)] public string description;
            [Tooltip("카드 그림 (위쪽 리본에 이름, 아래쪽 남색 칸에 설명이 들어간다). 비어 있으면 단색 카드로 표시한다.")]
            public Sprite card;
            [Tooltip("카드 위쪽 리본의 세로 중심 (카드 높이 대비, 위에서부터)")]
            [Range(0, 0.3f)] public float cardTitleCenter = 0.085f;
            [Tooltip("리본이 어두운 색이면 켠다. 제목을 밝은 글자색으로 쓴다.")]
            public bool cardDarkRibbon;
            [Min(0.1f)] public float commanderHealth = 1;
            [Min(0.1f)] public float soldierHealth = 1;
            [Min(0.1f)] public float damage = 1;
            [Min(0.1f)] public float attackSpeed = 1;
            [Min(0.1f)] public float moveSpeed = 1;
            [Min(0.1f)] public float spawnInterval = 1;
            [Min(0.1f)] public float pickupRadius = 1;
            [Min(0.1f)] public float experience = 1;
        }
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
        [Min(1)] public int adventureTier3Day = 21;
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
        [Header("시작 리스크와 보상 (무작위 3개 제시)")]
        public Tradeoff[] openingTradeoffs;
        [Header("시작 계약 카드 배치 (1920×1080 기준)")]
        [Tooltip("카드 이미지 비율(1024×1536 = 2:3)에 맞춘다.")]
        public Vector2 tradeoffCardSize = new Vector2(400, 600);
        [Min(0)] public float tradeoffCardSpacing = 450;
        public float tradeoffCardY = -20;
        [Tooltip("제목 글자 영역 높이 (카드 높이 대비). 세로 위치는 계약마다 cardTitleCenter로 정한다.")]
        [Range(0.02f, 0.2f)] public float tradeoffTitleHeight = 0.055f;
        [Tooltip("카드 높이 대비 위에서부터의 비율. x = 설명 칸 위쪽, y = 설명 칸 아래쪽")]
        public Vector2 tradeoffDescriptionBand = new Vector2(0.80f, 0.93f);
        [Tooltip("카드 너비 대비 글자 영역 너비")]
        [Range(0.3f, 1f)] public float tradeoffTitleWidth = 0.55f;
        [Range(0.3f, 1f)] public float tradeoffDescriptionWidth = 0.8f;
        public Color tradeoffTitleColor = new Color(0.32f, 0.17f, 0.05f);
        public Color tradeoffDarkRibbonTitleColor = new Color(1f, 0.88f, 0.6f);
        public Color tradeoffDescriptionColor = new Color(0.93f, 0.9f, 0.8f);
        [Tooltip("설명의 이 구분자를 줄바꿈으로 바꿔 혜택과 대가를 두 줄로 나눈다. 비우면 그대로 쓴다.")]
        public string tradeoffDescriptionLineBreak = " / ";

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
