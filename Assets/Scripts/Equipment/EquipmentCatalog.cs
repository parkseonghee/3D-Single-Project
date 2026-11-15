using ArmySurvivor.Building;
using TMPro;
using UnityEngine;

namespace ArmySurvivor.Equipment
{
    [CreateAssetMenu(menuName = "Army Survivor/Equipment/Catalog")]
    public sealed class EquipmentCatalog : ScriptableObject
    {
        public EquipmentItem[] items;
        public BuildingDefinition supplyBuilding;
        public TMP_FontAsset font;
        public GameObject ticketPrefab;
        public Sprite ticketIcon;
        [Header("보급소 바로가기 배너 위치 (1920×1080 기준)")]
        [Tooltip("전투 화면: 화면 왼쪽 위 기준")]
        public Vector2 battleShortcutPosition = new Vector2(28, -105);
        [Tooltip("마을: 왼쪽 위에는 왕국 깃발과 벼 채취 창이 있어 화면 오른쪽 위 기준으로 둔다.")]
        public Vector2 villageShortcutAnchor = new Vector2(1, 1);
        public Vector2 villageShortcutPosition = new Vector2(-28, -90);
        [Header("보급 연출")]
        public GameObject supplyFirework;
        [Header("문짝 경로와 로컬 회전 (닫힘 / 열림)")]
        public string supplyDoorAPath;
        public string supplyDoorBPath;
        public Vector3 supplyDoorAClosedEuler;
        public Vector3 supplyDoorAOpenEuler;
        public Vector3 supplyDoorBClosedEuler;
        public Vector3 supplyDoorBOpenEuler;
        [Min(1)] public float supplySequenceDuration = 4.5f;
        [Min(0.05f)] public float supplyFireworkInterval = 0.15f;
        [Min(0.1f)] public float supplyFireworkScale = 1.5f;
        [Header("폭죽 생성 위치 — 성문 중심 기준 월드 오프셋")]
        public Vector3 supplyFireworkLeftOffset = new Vector3(-4, 0.75f, 0);
        public Vector3 supplyFireworkRightOffset = new Vector3(4, 0.75f, 0);
        [Tooltip("각 축에 무작위로 더할 최대 거리입니다. 모두 0이면 설정한 위치에 정확히 생성됩니다.")]
        public Vector3 supplyFireworkJitter = new Vector3(1, 0.5f, 2);
        public Vector3 supplyCameraStart = new Vector3(0, 12, -28);
        public Vector3 supplyCameraEnd = new Vector3(0, 4, -9);
        public Vector3 supplyLookAt = new Vector3(0, 4, 0);
        [Range(20, 90)] public float supplyCameraFov = 48;
        [Range(0, 1)] public float supplyDoorStart = 0.5f;
        [Min(0.1f)] public float supplyDoorDuration = 1.4f;
        [Range(0, 1)] public float ticketDropChance = 0.03f;
        [Min(0)] public float ticketSpawnSeparation = 1.8f;
        [Min(0)] public float ticketPickupDelay;
        [Min(0)] public int firstDayTicketReward;
        [Min(1)] public int firstSupplyDrawDay;
        [Min(0.1f)] public float pickupRadius = 1.5f;
        [Min(0.1f)] public float attractionRadius = 4f;
        [Min(0.1f)] public float attractionSpeed = 8f;
        [Header("기본 확률 (보장 제외). C는 나머지 확률")]
        [Range(0, 1)] public float sRate = 0.006f;
        [Range(0, 1)] public float aRate = 0.094f;
        [Range(0, 1)] public float bRate = 0.30f;
        [Min(1)] public int aGuarantee = 10;
        [Min(1)] public int sGuarantee = 90;
        [Header("서바이벌 인장 보급 (그 판에서만 유지)")]
        [Tooltip("인장 1개당 제시하는 장비 카드 수")]
        [Min(1)] public int survivalChoiceCount = 3;
        [Tooltip("장착 중인 장비가 있으면 그 장비의 강화 카드를 섞어 줄 확률")]
        [Range(0, 1)] public float survivalUpgradeOfferChance = 0.6f;
        [Tooltip("강화 1단계마다 장비 효과 증가율 (0.25 = 기본 효과의 25%씩 추가)")]
        [Min(0)] public float survivalUpgradeBonus = 0.25f;
        [Min(0)] public int survivalMaxUpgrade = 5;
        [Tooltip("카드를 고르지 않고 현재 장비를 유지할 때 받는 경험치 병")]
        [Min(0)] public int survivalKeepBottles = 3;
        [Tooltip("끄면 무기만 제시한다")]
        public bool survivalIncludeNonWeapons;
        [Tooltip("서바이벌 무기 등급 피해 배율 (C, B, A, S 순서)")]
        public float[] survivalGradeDamage = { 1.1f, 1.2f, 1.35f, 1.6f };
        [Header("모험 장비 돌파 (같은 장비를 중복 획득하면 자동 돌파, 저장 슬롯에 유지)")]
        [Tooltip("최대 돌파 단계. 이 이상 중복 획득한 장비는 능력이 더 오르지 않는다.")]
        [Min(0)] public int adventureMaxBreakthrough = 5;
        [Tooltip("돌파 1단계마다 장비 효과 증가율 (0.25 = 기본 효과의 25%씩 추가)")]
        [Min(0)] public float adventureBreakthroughBonus = 0.25f;
        [Tooltip("{0} = 돌파 단계, {1} = 최대 돌파")]
        public string breakthroughFormat = "{0}/{1} 돌파";
        [Tooltip("{0} = 늘어난 능력 %")]
        public string breakthroughBonusFormat = "능력 +{0}%";
        [Tooltip("보급 결과 카드 문구. {0} = 돌파 후 단계")]
        public string breakthroughResultFormat = "중복 획득 · {0}돌파";
        public string breakthroughMaxResultText = "중복 획득 · 최대 돌파";
        [Tooltip("보급소 상세의 장착 효과 줄. {0} = 돌파가 반영된 효과 목록")]
        public string equipEffectFormat = "장착: {0}";
        [Tooltip("장착 효과 목록 구분자")]
        public string equipEffectSeparator = ", ";
        [Header("장비 효과 문구 · {0} = 수치")]
        public string squadDamageFormat = "부대 공격 피해 +{0}%";
        public string squadAttackSpeedFormat = "부대 공격 속도 +{0}%";
        public string commanderMoveFormat = "지휘관 이동 속도 +{0}%";
        public string commanderDamageReductionFormat = "지휘관 받는 피해 -{0}%";
        public string commanderHealOnKillFormat = "처치 시 지휘관 체력 {0}% 회복";
        [Tooltip("{0} = 확률 %, {1} = 지속 초")]
        public string slowOnHitFormat = "적중 시 {0}% 확률로 {1}초 감속";
        public string survivalGradeDamageFormat = "무기 등급 피해 ×{0}";
        public float CRate => Mathf.Max(0, 1 - sRate - aRate - bRate);
        public bool HasSRewards => sRate > 0 && HasGrade(ItemGrade.S);
        public bool HasGrade(ItemGrade grade) => items != null && System.Array.Exists(items, x => x != null && x.grade == grade);
        public EquipmentItem Find(string id) => System.Array.Find(items, x => x != null && x.id == id);
        public bool IsValid
        {
            get
            {
                if (items == null || sRate < 0 || aRate < 0 || bRate < 0 || sRate + aRate + bRate > 1 || aGuarantee < 1 || sGuarantee < 1) return false;
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var item in items)
                    if (item == null || string.IsNullOrEmpty(item.id) || !ids.Add(item.id) || item.model == null) return false;
                if (!HasGrade(ItemGrade.A) || (sRate > 0 && !HasGrade(ItemGrade.S)) ||
                    (bRate > 0 && !HasGrade(ItemGrade.B)) || (CRate > 0 && !HasGrade(ItemGrade.C))) return false;
                return true;
            }
        }
    }
}
