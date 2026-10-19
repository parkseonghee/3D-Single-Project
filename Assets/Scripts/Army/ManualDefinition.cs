using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [CreateAssetMenu(menuName = "Army Survivor/Manual/Definition")]
    public sealed class ManualDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea(2, 6)] public string description;
        [TextArea(2, 4)] public string playStyle;
        public Sprite icon;
        public ManualEffectDefinition effect;
        public bool unlocked = true;
        [Min(1)] public int minimumSoldiers = 1;
        [Tooltip("모든 지정 병종이 출전 편성에 있어야 제시됩니다. 비워두면 병종 제한 없음.")]
        public UnitDefinition[] requiredTypes;

        public bool IsEligible(RecruitmentController recruitment)
        {
            if (!unlocked || effect == null || recruitment == null) return false;
            var types = new HashSet<UnitDefinition>();
            int count = 0;
            foreach (Transform unit in recruitment.SoldiersRoot)
                if (unit.gameObject.activeInHierarchy && recruitment.Units.TryGetValue(unit, out var type))
                { count++; types.Add(type); }
            if (count < Mathf.Max(minimumSoldiers, effect.MinimumSoldiers)) return false;
            if (requiredTypes != null)
                foreach (var type in requiredTypes) if (type != null && !types.Contains(type)) return false;
            return true;
        }
    }
}
