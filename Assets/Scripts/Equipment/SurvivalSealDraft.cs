using System;
using System.Collections.Generic;
using ArmySurvivor.Army;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Equipment
{
    // 서바이벌 인장 보급: 장비 카드 중 1장을 고르거나 현재 장비를 유지하고 경험치 병을 받는다.
    // 장착 중인 장비가 카드로 나오면 교체가 아니라 강화(+1)한다. 결과는 그 판에서만 유지된다.
    // 창과 카드는 PlayScene에 배치하고, 이 스크립트는 문구와 선택 동작만 채운다.
    public sealed class SurvivalSealDraft : MonoBehaviour
    {
        [Serializable]
        private class Card
        {
            public Button button;
            public Image gradeBand;
            public TMP_Text tag;
            public TMP_Text grade;
            public TMP_Text itemName;
            public TMP_Text effects;
        }

        [SerializeField] private RunController run;
        [SerializeField] private BattleExperience experience;
        [SerializeField] private EquipmentCatalog catalog;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text currentEquipment;
        [SerializeField] private Card[] cards;
        [SerializeField] private Button keepButton;
        [SerializeField] private TMP_Text keepLabel;

        [Header("문구")]
        [SerializeField] private string newTag = "새 장비";
        [Tooltip("{0} = 현재 강화, {1} = 강화 후")]
        [SerializeField] private string upgradeTag = "강화  +{0} → +{1}";
        [Tooltip("{0} = 등급")]
        [SerializeField] private string gradeFormat = "{0} 등급";
        [Tooltip("{0} = 장비 이름, {1} = 강화 표시")]
        [SerializeField] private string currentFormat = "현재 장비 · {0}{1}";
        [SerializeField] private string currentNone = "현재 장비 없음";
        [SerializeField] private string upgradeSuffix = " +{0}";
        [Tooltip("{0} = 경험치 병 수")]
        [SerializeField] private string keepFormat = "현재 장비 유지 · 경험치 병 +{0}";
        [SerializeField] private string keepEmptyFormat = "건너뛰기 · 경험치 병 +{0}";
        [Header("등급 색 (C, B, A, S)")]
        [SerializeField] private Color[] gradeColors =
        {
            new Color(0.62f, 0.66f, 0.72f), new Color(0.35f, 0.62f, 0.95f),
            new Color(0.72f, 0.42f, 0.95f), new Color(1f, 0.74f, 0.25f)
        };
        [SerializeField] private Color upgradeTagColor = new Color(1f, 0.8f, 0.35f);
        [SerializeField] private Color newTagColor = new Color(0.85f, 0.9f, 1f);

        private readonly List<EquipmentItem> offered = new List<EquipmentItem>();
        private int pending;
        private float previousTimeScale = 1f;
        private bool previousChoosing;
        private bool ownsPause;

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].button.onClick.AddListener(() => Choose(index));
            }
            keepButton.onClick.AddListener(Keep);
            if (run != null) run.RunEnded += CloseWithoutChoice;
        }

        private void OnDestroy()
        {
            if (run != null) run.RunEnded -= CloseWithoutChoice;
            CloseWithoutChoice();
        }

        // 인장 1개를 주웠을 때 호출한다. 창이 열려 있으면 닫힌 뒤 이어서 연다.
        public void Grant()
        {
            pending++;
            if (!IsOpen) OpenNext();
        }

        private void OpenNext()
        {
            if (pending <= 0 || run == null || !run.IsRunning || catalog == null) { pending = 0; return; }
            pending--;
            offered.Clear();
            offered.AddRange(EquipmentInventory.RollSurvivalChoices(catalog));
            if (!ownsPause)
            {
                previousTimeScale = Time.timeScale;
                previousChoosing = run.IsChoosingUpgrade;
                Time.timeScale = 0;
                run.IsChoosingUpgrade = true;
                ownsPause = true;
            }
            Refresh();
            panel.transform.SetAsLastSibling();
            panel.SetActive(true);
        }

        private void Refresh()
        {
            var equipped = EquipmentInventory.EquippedItem;
            int level = EquipmentInventory.EquippedUpgrade;
            currentEquipment.text = equipped == null ? currentNone
                : string.Format(currentFormat, equipped.displayName, level > 0 ? string.Format(upgradeSuffix, level) : "");
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                bool used = i < offered.Count;
                card.button.gameObject.SetActive(used);
                if (!used) continue;
                var item = offered[i];
                bool upgrade = item == equipped;
                int nextLevel = upgrade ? level + 1 : 0;
                card.tag.text = upgrade ? string.Format(upgradeTag, level, nextLevel) : newTag;
                card.tag.color = upgrade ? upgradeTagColor : newTagColor;
                card.grade.text = string.Format(gradeFormat, item.grade);
                Color color = GradeColor(item.grade);
                card.grade.color = color;
                if (card.gradeBand != null) card.gradeBand.color = color;
                card.itemName.text = item.displayName + (nextLevel > 0 ? string.Format(upgradeSuffix, nextLevel) : "");
                card.effects.text = EquipmentInventory.EffectText(item, EquipmentInventory.ScaleAt(nextLevel), true);
            }
            keepLabel.text = string.Format(equipped != null ? keepFormat : keepEmptyFormat, catalog.survivalKeepBottles);
        }

        private Color GradeColor(ItemGrade grade) =>
            gradeColors != null && (int)grade < gradeColors.Length ? gradeColors[(int)grade] : Color.white;

        private void Choose(int index)
        {
            if (!IsOpen || index >= offered.Count) return;
            var item = offered[index];
            if (item == EquipmentInventory.EquippedItem) EquipmentInventory.UpgradeEquipped();
            else EquipmentInventory.EquipSurvivalItem(item);
            Finish();
        }

        private void Keep()
        {
            if (!IsOpen) return;
            if (experience != null) experience.GrantBottles(catalog.survivalKeepBottles);
            Finish();
        }

        private void Finish()
        {
            panel.SetActive(false);
            if (pending > 0 && run.IsRunning) { OpenNext(); return; }
            ReleasePause();
        }

        private void CloseWithoutChoice()
        {
            pending = 0;
            if (panel != null) panel.SetActive(false);
            ReleasePause();
        }

        private void ReleasePause()
        {
            if (!ownsPause) return;
            ownsPause = false;
            if (run != null) run.IsChoosingUpgrade = previousChoosing;
            // 전투 종료 처리에서 이미 시간 배율을 되돌렸다면 덮어쓰지 않는다.
            if (Time.timeScale == 0) Time.timeScale = previousTimeScale;
        }
    }
}
