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
        [TextArea] public string description;
        public GameObject model;
        public Vector3 previewRotation;
    }
}
