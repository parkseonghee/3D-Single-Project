using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Building
{
    public class BuildingPalette : MonoBehaviour
    {
        [Serializable]
        private class BuildingOption
        {
            public BuildingDefinition building;
            public Button button;
            public Image icon;

        }

        [SerializeField] private BuildingPlacementController controller;
        [SerializeField] private BuildingOption[] options;

        [Header("건설 목록 창")]
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private int lastRice = -1;

        private void Start()
        {
            openButton.onClick.AddListener(TogglePanel);
            closeButton.onClick.AddListener(ClosePanel);
            buildingPanel.SetActive(false);

            foreach (BuildingOption option in options)
            {
                option.icon.sprite = option.building.icon;
                option.icon.gameObject.SetActive(option.building.icon != null);
                option.button.onClick.AddListener(() => SelectBuilding(option.building));
            }
            RefreshButtons();
        }

        private void TogglePanel()
        {
            controller.Cancel();
            buildingPanel.SetActive(!buildingPanel.activeSelf);
        }

        private void ClosePanel()
        {
            buildingPanel.SetActive(false);
        }

        private void SelectBuilding(BuildingDefinition building)
        {
            ClosePanel();
            controller.Select(building);
        }

        private void Update()
        {
            // 문구는 Inspector에서 입력하고, 코드는 버튼의 건설 가능 여부만 갱신한다.
            if (lastRice != controller.Rice)
            {
                RefreshButtons();
            }
        }

        private void RefreshButtons()
        {
            lastRice = controller.Rice;

            foreach (BuildingOption option in options)
            {
                int cost = controller.GetCost(option.building);

                option.button.interactable = controller.Rice >= cost;
            }
        }
    }
}
