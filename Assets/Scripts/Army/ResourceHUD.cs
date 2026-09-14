using ArmySurvivor.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class ResourceHUD : MonoBehaviour
    {
        [SerializeField] private TMP_Text riceAmount;
        [SerializeField] private TMP_Text goldAmount;
        [SerializeField] private TMP_Text specialAmount;
        [SerializeField] private Button exchangeButton;

        private BuildingPlacementController resources;
        private int lastRice = -1;
        private int lastGold = -1;
        private int lastSpecial = -1;

        private void Start()
        {
            resources = FindFirstObjectByType<BuildingPlacementController>(FindObjectsInactive.Include);
            exchangeButton.onClick.AddListener(Exchange);
            Refresh();
        }

        private void OnEnable()
        {
            if (resources != null) Refresh();
        }

        private void Update()
        {
            if (resources != null && (lastRice != resources.Rice || lastGold != resources.Gold ||
                lastSpecial != resources.SpecialResource)) Refresh();
        }

        private void Exchange()
        {
            if (resources != null) resources.TryExchangeRice();
            Refresh();
        }

        private void Refresh()
        {
            // 자원 이름과 버튼 문구는 Inspector에서 입력하고 숫자만 갱신한다.
            lastRice = resources != null ? resources.Rice : -1;
            lastGold = resources != null ? resources.Gold : -1;
            lastSpecial = resources != null ? resources.SpecialResource : -1;
            riceAmount.text = lastRice >= 0 ? lastRice.ToString() : "—";
            goldAmount.text = lastGold >= 0 ? lastGold.ToString() : "—";
            specialAmount.text = lastSpecial >= 0 ? lastSpecial.ToString() : "—";
            exchangeButton.interactable = resources != null && resources.CanExchange;
        }
    }
}
