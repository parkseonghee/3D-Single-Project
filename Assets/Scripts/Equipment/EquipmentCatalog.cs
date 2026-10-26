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
        [Header("보급 연출")]
        public GameObject supplyFirework;
        public AnimationClip supplyGateOpen;
        [Min(1)] public float supplySequenceDuration = 4.5f;
        [Min(0.05f)] public float supplyFireworkInterval = 0.15f;
        [Min(0.1f)] public float supplyFireworkScale = 1.5f;
        public Vector3 supplyCameraStart = new Vector3(0, 12, -28);
        public Vector3 supplyCameraEnd = new Vector3(0, 4, -9);
        public Vector3 supplyLookAt = new Vector3(0, 4, 0);
        [Range(20, 90)] public float supplyCameraFov = 48;
        [Range(0, 1)] public float supplyDoorStart = 0.5f;
        [Min(0.1f)] public float supplyDoorDuration = 1.4f;
        [Range(0, 1)] public float ticketDropChance = 0.03f;
        [Min(0.1f)] public float pickupRadius = 1.5f;
        [Min(0.1f)] public float attractionRadius = 4f;
        [Min(0.1f)] public float attractionSpeed = 8f;
        [Header("기본 확률 (보장 제외). C는 나머지 확률")]
        [Range(0, 1)] public float sRate = 0.006f;
        [Range(0, 1)] public float aRate = 0.094f;
        [Range(0, 1)] public float bRate = 0.30f;
        [Min(1)] public int aGuarantee = 10;
        [Min(1)] public int sGuarantee = 90;
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
