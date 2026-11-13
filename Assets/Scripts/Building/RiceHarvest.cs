using ArmySurvivor.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Building
{
    public partial class RiceHarvest : MonoBehaviour
    {
        [SerializeField] private BuildingPlacementController resources;
        // 2026-11-12: 클릭 1회로 하루 총량을 모두 수확한다(기획서 5.1).
        [SerializeField, Min(1)] private int harvestsPerDay = 1;
        [SerializeField] private Button harvestButton;
        [SerializeField] private TMP_Text dayAmount;
        [SerializeField] private TMP_Text harvestAmount;
        [SerializeField] private TMP_Text remainingCount;
        [SerializeField] private TMP_Text remainingAmount;
        [SerializeField] private GameObject exhaustedLabel;

        public int Day { get; private set; }
        public int DailyTotal { get; private set; }
        public int RemainingCount { get; private set; }
        public int RemainingRice { get; private set; }
        public int TodayHarvestedCount => Mathf.Clamp(harvestsPerDay - RemainingCount, 0, harvestsPerDay);
        private int portion;
        private int lastRice = -1;

        public int NextAmount => RemainingCount == 0 ? 0 : RemainingCount == 1 ? RemainingRice : portion;
        public bool CanHarvest => RemainingCount > 0 && RemainingRice > 0 &&
            resources.Rice <= int.MaxValue - NextAmount;

        private void Start()
        {
            harvestButton.onClick.AddListener(Harvest);
            BeginDay(1);
        }

        // 전장 클리어 후 다음 Day 진입 시 호출한다. 같은 날 재호출은 무시한다.
        public bool BeginDay(int day)
        {
            if (day <= Day) return false;
            Day = day;
            DailyTotal = resources.GetDailyHarvestTotal();
            RemainingRice = DailyTotal;
            RemainingCount = harvestsPerDay;
            portion = DailyTotal / harvestsPerDay;
            Refresh();
            return true;
        }

        public bool TryHarvest()
        {
            if (!CanHarvest) return false;
            int amount = NextAmount;
            if (!resources.TryAddRice(amount)) return false;
            RemainingRice -= amount;
            RemainingCount--;
            Refresh();
            TutorialEvents.Raise("RiceHarvested", harvestButton.transform);
            return true;
        }

        private void Harvest()
        {
            TryHarvest();
        }

        private void Update()
        {
            if (lastRice != resources.Rice) Refresh();
        }

        private void Refresh()
        {
            lastRice = resources.Rice;
            dayAmount.text = Day.ToString();
            harvestAmount.text = NextAmount.ToString();
            remainingCount.text = RemainingCount.ToString();
            remainingAmount.text = RemainingRice.ToString();
            harvestButton.interactable = CanHarvest;
            exhaustedLabel.SetActive(RemainingRice == 0 || RemainingCount == 0);
        }
    }
}
