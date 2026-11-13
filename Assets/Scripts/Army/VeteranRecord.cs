using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 고용 병사 1명이 회차 동안 영구 보유하는 베테랑 스킬(기획서 4.6).
    // 병사 루트 오브젝트에 붙는다. 병사 오브젝트는 편성/예비 이동과 Day 전환 동안 유지되므로 여기에 둔다.
    // 스킬은 저장과 같은 문자열 키(BattleExperience.SkillKey)로 보관한다.
    public class VeteranRecord : MonoBehaviour
    {
        [SerializeField] private List<string> skills = new List<string>();
        public IReadOnlyList<string> Skills => skills;

        public bool Has(string key) => skills.Contains(key);

        public void Add(string key)
        {
            if (!string.IsNullOrEmpty(key) && !skills.Contains(key)) skills.Add(key);
        }

        public void Replace(int index, string key)
        {
            if (index < 0 || index >= skills.Count || string.IsNullOrEmpty(key) || skills.Contains(key)) return;
            skills[index] = key;
        }

        public void SetAll(IEnumerable<string> keys)
        {
            skills.Clear();
            if (keys == null) return;
            foreach (string key in keys) Add(key);
        }

        public static VeteranRecord For(Transform unit, bool create)
        {
            if (unit == null) return null;
            var record = unit.GetComponent<VeteranRecord>();
            if (record == null && create) record = unit.gameObject.AddComponent<VeteranRecord>();
            return record;
        }
    }
}
