using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Building
{
    public class BuildingManagementUI : MonoBehaviour
    {
        [System.Serializable]
        private class UpgradeDescription
        {
            public BuildingDefinition building;
            public GameObject textObject;
        }

        [SerializeField] private UpgradeDescription[] descriptions;
        [SerializeField] private BuildingPlacementController controller;
        [SerializeField] private GameObject panel;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text buildingName;
        [SerializeField] private TMP_Text levelAmount;
        [SerializeField] private TMP_Text nextLevelAmount;
        [SerializeField] private TMP_Text upgradeCostAmount;
        [SerializeField] private TMP_Text refundAmount;
        [SerializeField] private GameObject insufficientGold;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button moveButton;
        [SerializeField] private Button demolishButton;
        [SerializeField] private Button closeButton;
        [Header("강화 수치 미리보기 (기획서 9) · 해당 효과가 없는 건물은 숨김")]
        [SerializeField] private TMP_Text bonusPreview;
        [SerializeField] private string statFormat = "공격력·체력  현재 +{0}%  →  강화 후 +{1}%";
        [SerializeField] private string formationFormat = "편성 상한  현재 +{0}칸  →  강화 후 +{1}칸";
        [SerializeField] private string higherCopyFormat = "더 높은 Lv.{0} {1}의 효과가 적용 중";

        private GameObject selected;
        private int lastGold;
        private int lastLevel;

        private void Start()
        {
            upgradeButton.onClick.AddListener(Upgrade);
            moveButton.onClick.AddListener(Move);
            demolishButton.onClick.AddListener(Demolish);
            closeButton.onClick.AddListener(Close);
            Close();
        }

        public void Open(GameObject target)
        {
            controller.Cancel();
            if (controller.GetDefinition(target) == null) return;
            selected = target;
            panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            selected = null;
            panel.SetActive(false);
        }

        private void Update()
        {
            if (!panel.activeSelf) return;
            if (selected == null) { Close(); return; }
            if (lastGold != controller.Gold || lastLevel != controller.GetLevel(selected)) Refresh();
        }

        private void Upgrade()
        {
            controller.TryUpgrade(selected);
            Refresh();
        }

        private void Move()
        {
            GameObject target = selected;
            Close();
            controller.BeginMove(target);
        }

        private void Demolish()
        {
            controller.TryDemolish(selected);
            Close();
        }

        private void Refresh()
        {
            BuildingDefinition definition = controller.GetDefinition(selected);
            if (definition == null) { Close(); return; }
            lastLevel = controller.GetLevel(selected);
            lastGold = controller.Gold;
            int cost = definition.UpgradeCostAt(lastLevel);
            // 고정 문구는 Inspector에 두고, 선택 건물 정보와 수치만 갱신한다.
            buildingName.text = definition.displayName;
            icon.sprite = definition.icon;
            foreach (UpgradeDescription description in descriptions)
                description.textObject.SetActive(description.building == definition);
            levelAmount.text = lastLevel.ToString();
            nextLevelAmount.text = ((long)lastLevel + 1).ToString();
            upgradeCostAmount.text = cost.ToString();
            refundAmount.text = definition.DemolitionRefund.ToString();
            upgradeButton.interactable = lastGold >= cost && lastLevel < int.MaxValue;
            insufficientGold.SetActive(lastGold < cost);
            RefreshBonusPreview(definition);
        }

        // 선택한 건물 1채의 현재·다음 레벨 효과를 보여준다. 수치는 BuildingDefinition에서 계산한다.
        private void RefreshBonusPreview(BuildingDefinition definition)
        {
            if (bonusPreview == null) return;
            var lines = new System.Text.StringBuilder();
            int next = lastLevel < int.MaxValue ? lastLevel + 1 : lastLevel;
            if (definition.formationSlots > 0 || definition.formationSlotsPerUpgrade > 0)
                lines.Append(string.Format(formationFormat, definition.FormationSlotsAt(lastLevel), definition.FormationSlotsAt(next)));
            bool stat = definition.unitDamageBonus > 0 || definition.unitDamageBonusPerUpgrade > 0 ||
                        definition.unitHealthBonus > 0 || definition.unitHealthBonusPerUpgrade > 0;
            if (stat)
            {
                if (lines.Length > 0) lines.Append('\n');
                // 공격력과 체력 보너스는 같은 값으로 설정한다. 다르면 공격력 기준으로 표시한다.
                lines.Append(string.Format(statFormat, Percent(definition.DamageBonusAt(lastLevel)), Percent(definition.DamageBonusAt(next))));
                // 같은 건물이 여러 채면 가장 높은 레벨 하나만 적용된다.
                int highest = controller.GetHighestLevel(definition);
                if (highest > lastLevel)
                    lines.Append('\n').Append(string.Format(higherCopyFormat, highest, definition.displayName));
            }
            bonusPreview.text = lines.ToString();
            bonusPreview.gameObject.SetActive(lines.Length > 0);
        }

        private static int Percent(float value) => Mathf.RoundToInt(value * 100);
    }
}
