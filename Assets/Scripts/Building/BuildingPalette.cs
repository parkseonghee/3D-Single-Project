using System;
using TMPro;
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
            [Tooltip("이름과 현재 건설 비용")]
            public TMP_Text label;
            [Tooltip("BuildingDefinition.cardDescription을 표시")]
            public TMP_Text description;
        }

        [SerializeField] private BuildingPlacementController controller;
        [SerializeField] private BuildingOption[] options;
        [Header("카드 문구 · {0} 이름, {1} 현재 비용, {2} 비용 색(벼 부족 여부)")]
        [SerializeField] private string labelFormat = "{0}\n<color=#{2}>{1} 벼</color>";
        [SerializeField] private Color affordableCostColor = new Color(0.45f, 0.29f, 0.1f);
        [SerializeField] private Color shortageCostColor = new Color(0.75f, 0.16f, 0.12f);

        [Header("건설 목록 창")]
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;

        private int lastRice = -1;
        private bool wasOpen;

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
                if (option.description != null) option.description.text = option.building.cardDescription;
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

        public bool RevealBuilding(BuildingDefinition building)
        {
            if (building == null || options == null) return false;
            foreach (BuildingOption option in options)
            {
                if (option.building != building || option.button == null) continue;
                buildingPanel.SetActive(true);
                Canvas.ForceUpdateCanvases();
                ScrollRect scroll = option.button.GetComponentInParent<ScrollRect>();
                if (scroll != null && scroll.content != null)
                {
                    RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
                    RectTransform card = (RectTransform)option.button.transform;
                    float scrollableWidth = scroll.content.rect.width - viewport.rect.width;
                    if (scrollableWidth > 0)
                    {
                        // The cards are laid out from the content's left edge.
                        float cardCenter = card.anchoredPosition.x + card.rect.width * (0.5f - card.pivot.x);
                        scroll.horizontalNormalizedPosition = Mathf.Clamp01(
                            (cardCenter - viewport.rect.width * 0.5f) / scrollableWidth);
                    }
                }
                return true;
            }
            return false;
        }

        private void SelectBuilding(BuildingDefinition building)
        {
            ClosePanel();
            controller.Select(building);
        }

        private void Update()
        {
            // 벼가 바뀌면 건설 가능 여부를, 창을 열 때는 건설 수에 따라 오른 비용도 다시 그린다.
            if (lastRice != controller.Rice || (buildingPanel.activeSelf && !wasOpen))
            {
                RefreshButtons();
            }
            wasOpen = buildingPanel.activeSelf;
        }

        private void RefreshButtons()
        {
            lastRice = controller.Rice;

            foreach (BuildingOption option in options)
            {
                int cost = controller.GetCost(option.building);
                bool affordable = controller.Rice >= cost;
                option.button.interactable = affordable;
                if (option.label != null)
                    option.label.text = string.Format(labelFormat, option.building.displayName, cost,
                        ColorUtility.ToHtmlStringRGB(affordable ? affordableCostColor : shortageCostColor));
            }
        }
    }
}
