using UnityEngine;

namespace ArmySurvivor.Equipment
{
    public enum ItemGrade { C, B, A, S }

    [CreateAssetMenu(menuName = "Army Survivor/Equipment/Item")]
    public sealed class EquipmentItem : ScriptableObject
    {
        public string id;
        public string displayName;
        public ItemGrade grade;
        public bool isWeapon = true;
        [TextArea] public string description;
        public GameObject model;
        public Vector3 previewRotation;
        [Header("장착 능력 (0.1 = 10%)")]
        [Range(0, 1)] public float squadDamageBonus;
        [Range(0, 1)] public float squadAttackSpeedBonus;
        [Range(0, 1)] public float commanderMoveBonus;
        [Range(0, 1)] public float commanderDamageReduction;
        [Range(0, 1)] public float commanderHealOnKillRatio;
        [Range(0, 1)] public float slowOnHitChance;
        [Min(0)] public float slowOnHitDuration;
    }
}
