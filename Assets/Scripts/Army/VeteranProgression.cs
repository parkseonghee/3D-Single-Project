using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    // 베테랑 시스템(기획서 4.6): 전장 클리어 시 자격 판정 → 결과창 다음 선택 화면.
    // 흐름: DayController 승리 → CaptureClear() → 결과창 "귀환" → TryOpen(onComplete) → 병사별 선택 → onComplete.
    public class VeteranProgression : MonoBehaviour
    {
        [Header("규칙")]
        [SerializeField, Min(1)] private int maximumVeteranSkills = 2;
        [SerializeField, Min(1)] private int requiredLearnedSkills = 2;

        [Header("씬 연결")]
        [SerializeField] private BattleExperience experience;
        [SerializeField] private RecruitmentController recruitment;

        [Header("선택 화면 - 씬 배치 참조")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text soldier;
        [SerializeField] private TMP_Text guide;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TMP_Text[] optionLabels;
        [SerializeField] private Button skipButton;

        [Header("표시 문구")]
        [SerializeField] private string titleFormat = "베테랑 승급 ({0}/{1})";
        [SerializeField] private string soldierFormat = "{0} · 편성 {1}번  |  베테랑 스킬 {2}/{3}";
        [SerializeField] private string pickGuide = "이번 전장에서 배운 스킬 중 1개를 영구히 익힙니다.\n다음 전장부터 Lv.1로 시작합니다.";
        [SerializeField] private string replaceGuideFormat = "베테랑 스킬이 가득 찼습니다.\n'{0}'(으)로 교체할 스킬을 고르세요.";
        [SerializeField] private string currentVeteranFormat = "{0}";

        private struct Candidate
        {
            public Transform unit;
            public string unitName;
            public int slot;
            public List<int> learned;
        }

        private readonly List<Candidate> pending = new List<Candidate>();
        private Action completed;
        private int index, total, chosen = -1;
        private bool replacing;
        public bool IsOpen => panel != null && panel.activeSelf;
        public int PendingCount => pending.Count;
        public int MaximumVeteranSkills => maximumVeteranSkills;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int option = i;
                optionButtons[i].onClick.AddListener(() => SelectOption(option));
            }
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
        }

        // RunEnded로 전장 성장이 지워지기 전에 호출한다. 클리어 시점에 살아 있어야 자격이 있다.
        public void CaptureClear()
        {
            pending.Clear();
            if (GameModeManager.Current != GameMode.Adventure || experience == null || recruitment == null) return;
            int slot = 0;
            foreach (Transform unit in recruitment.SoldiersRoot)
            {
                slot++;
                if (!recruitment.Units.TryGetValue(unit, out UnitDefinition definition)) continue;
                if (!unit.gameObject.activeSelf) continue;
                if (unit.TryGetComponent<UnitHealth>(out var health) && health.IsDead) continue;
                IReadOnlyList<int> learned = experience.LearnedThisBattle(unit);
                if (learned.Count < requiredLearnedSkills) continue;
                pending.Add(new Candidate { unit = unit, unitName = definition.displayName, slot = slot,
                    learned = new List<int>(learned) });
            }
        }

        public bool TryOpen(Action onComplete)
        {
            if (pending.Count == 0 || panel == null) { pending.Clear(); return false; }
            completed = onComplete;
            index = 0;
            total = pending.Count;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            ShowPick();
            return true;
        }

        private Candidate Current => pending[index];

        private void ShowPick()
        {
            replacing = false;
            chosen = -1;
            Candidate candidate = Current;
            RefreshHeader(candidate);
            guide.text = pickGuide;
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool visible = i < candidate.learned.Count;
                optionButtons[i].gameObject.SetActive(visible);
                if (visible) optionLabels[i].text = experience.SkillName(candidate.learned[i]);
            }
            // 상한 미만이면 반드시 1개를 고른다(기획서 4.6). 건너뛰기는 교체 단계에서만 보인다.
            if (skipButton != null) skipButton.gameObject.SetActive(false);
        }

        private void ShowReplace()
        {
            replacing = true;
            Candidate candidate = Current;
            VeteranRecord record = VeteranRecord.For(candidate.unit, true);
            RefreshHeader(candidate);
            guide.text = string.Format(replaceGuideFormat, experience.SkillName(chosen));
            for (int i = 0; i < optionButtons.Length; i++)
            {
                bool visible = i < record.Skills.Count;
                optionButtons[i].gameObject.SetActive(visible);
                if (visible)
                    optionLabels[i].text = string.Format(currentVeteranFormat,
                        experience.SkillName(BattleExperience.SkillChoice(record.Skills[i])));
            }
            if (skipButton != null) skipButton.gameObject.SetActive(true);
        }

        private void RefreshHeader(Candidate candidate)
        {
            VeteranRecord record = VeteranRecord.For(candidate.unit, false);
            int owned = record != null ? record.Skills.Count : 0;
            title.text = string.Format(titleFormat, index + 1, total);
            soldier.text = string.Format(soldierFormat, candidate.unitName, candidate.slot, owned, maximumVeteranSkills);
        }

        private void SelectOption(int option)
        {
            if (!IsOpen || index >= pending.Count) return;
            Candidate candidate = Current;
            VeteranRecord record = VeteranRecord.For(candidate.unit, true);
            if (!replacing)
            {
                if (option >= candidate.learned.Count) return;
                chosen = candidate.learned[option];
                if (record.Skills.Count < maximumVeteranSkills)
                {
                    record.Add(BattleExperience.SkillKey(chosen));
                    Next();
                }
                else ShowReplace();
                return;
            }
            if (option >= record.Skills.Count) return;
            record.Replace(option, BattleExperience.SkillKey(chosen));
            Next();
        }

        // 교체 단계에서 "기존 유지"를 누르면 이번 스킬을 포기한다.
        private void Skip()
        {
            if (!IsOpen || !replacing) return;
            Next();
        }

        private void Next()
        {
            index++;
            if (index < pending.Count) { ShowPick(); return; }
            pending.Clear();
            panel.SetActive(false);
            Action callback = completed;
            completed = null;
            callback?.Invoke();
        }

        // 테스트·디버그용: 화면 없이 현재 후보를 그대로 확인한다.
        public IReadOnlyList<int> PendingLearned(int candidate) =>
            candidate >= 0 && candidate < pending.Count ? pending[candidate].learned : null;
    }
}
