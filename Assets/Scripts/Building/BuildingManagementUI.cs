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
        }
    }
}
