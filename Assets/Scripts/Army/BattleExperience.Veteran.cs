using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 베테랑 시스템(기획서 4.6) 중 전장 안에서 필요한 부분.
    // - 이번 전장에서 새로 배운 스킬 기록 (자격 판정용)
    // - 전장 시작 시 보유 베테랑 스킬을 Lv.1로 적용
    // - 레벨업 카드 3장 중 베테랑 스킬 강화 카드 1장 보장
    public partial class BattleExperience
    {
        // 카드 번호(3~26)와 저장용 키의 대응. 순서를 바꾸지 말고 끝에만 추가한다.
        private static readonly string[] SkillKeys =
        {
            "Piercing", "MultiShot", "Focus", "Suppression", "Explosion", "WeaponStrike",
            "Bloodthirst", "SpinningSword", "SpinSlash", "Banner", "MagicTrap", "ManaCycle",
            "LaserAOE", "ChainLightning", "TeamShield", "TeamAttack", "IntimidatingShout", "IronWall",
            "TeamAttackSpeed", "ChainCharge", "FlameHooves", "ChargeInvulnerability", "MarchingHorn", "Momentum"
        };
        private const int FirstSkillChoice = 3;

        public static string SkillKey(int choice)
        {
            int index = choice - FirstSkillChoice;
            return index >= 0 && index < SkillKeys.Length ? SkillKeys[index] : null;
        }

        public static int SkillChoice(string key)
        {
            int index = System.Array.IndexOf(SkillKeys, key);
            return index < 0 ? -1 : index + FirstSkillChoice;
        }

        public static int SkillCount => SkillKeys.Length;

        private readonly Dictionary<Transform, List<int>> learnedThisBattle = new Dictionary<Transform, List<int>>();
        private static readonly List<int> NoSkills = new List<int>();

        public IReadOnlyList<int> LearnedThisBattle(Transform unit) =>
            unit != null && learnedThisBattle.TryGetValue(unit, out var list) ? list : NoSkills;

        // 카드 제목을 그대로 스킬 이름으로 쓴다. 이름을 코드에 따로 두지 않는다.
        public string SkillName(int choice)
        {
            GameObject card = CardFor(choice);
            if (card != null)
                foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(true))
                    if (text.name == "Title" && !string.IsNullOrEmpty(text.text)) return text.text;
            return SkillKey(choice) ?? "-";
        }

        private void RecordLearned(Transform unit, int choice)
        {
            if (unit == null) return;
            if (!learnedThisBattle.TryGetValue(unit, out var list))
                learnedThisBattle[unit] = list = new List<int>();
            if (!list.Contains(choice)) list.Add(choice);
        }

        private static bool IsLearned(UnitProgress p, int choice)
        {
            switch (choice)
            {
                case 3: return p.Piercing;
                case 4: return p.MultiShotLevel > 0;
                case 5: return p.FocusLevel > 0;
                case 6: return p.Suppression;
                case 7: return p.Explosion;
                case 8: return p.WeaponStrike;
                case 9: return p.BloodthirstLevel > 0;
                case 10: return p.SpinningSwordLevel > 0;
                case 11: return p.SpinSlashLevel > 0;
                case 12: return p.BannerLevel > 0;
                case 13: return p.MagicTrap;
                case 14: return p.ManaCycleLevel > 0;
                case 15: return p.LaserAOE;
                case 16: return p.ChainLightning;
                case 17: return p.TeamShield;
                case 18: return p.TeamAttack;
                case 19: return p.IntimidatingShout;
                case 20: return p.IronWall;
                case 21: return p.TeamAttackSpeed;
                case 22: return p.ChainCharge;
                case 23: return p.FlameHoovesLevel > 0;
                case 24: return p.ChargeInvulnerability;
                case 25: return p.MarchingHorn;
                case 26: return p.Momentum;
                default: return false;
            }
        }

        // 베테랑 스킬은 매 전장 Lv.1(강화 0단계)로 시작하며 "새로 배운 스킬"에 세지 않는다.
        private void ApplyVeteranSkills(Transform unit, UnitProgress progress)
        {
            if (GameModeManager.Current != GameMode.Adventure) return;
            VeteranRecord record = VeteranRecord.For(unit, false);
            if (record == null) return;
            foreach (string key in record.Skills)
            {
                int choice = SkillChoice(key);
                if (choice < 0 || IsLearned(progress, choice)) continue;
                ApplySkill(unit, progress, choice);
            }
        }

        // candidates 중 베테랑 스킬 강화 카드가 있으면 하나를 골라 0번 칸으로 옮긴다.
        // 강화가 끝난(후보에 없는) 베테랑 스킬만 있으면 보장하지 않는다.
        private bool GuaranteeVeteranCard(Transform unit, List<int> candidates)
        {
            if (GameModeManager.Current != GameMode.Adventure || candidates.Count == 0) return false;
            VeteranRecord record = VeteranRecord.For(unit, false);
            if (record == null || record.Skills.Count == 0) return false;
            int found = 0, pick = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (!record.Has(SkillKey(candidates[i]))) continue;
                found++;
                if (Random.Range(0, found) == 0) pick = i; // 후보 중 균등 선택
            }
            if (pick < 0) return false;
            (candidates[0], candidates[pick]) = (candidates[pick], candidates[0]);
            return true;
        }

        public bool IsVeteranSkill(Transform unit, int choice) =>
            VeteranRecord.For(unit, false)?.Has(SkillKey(choice)) == true;
    }
}
