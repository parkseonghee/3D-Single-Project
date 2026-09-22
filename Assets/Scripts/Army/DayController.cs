using ArmySurvivor.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class DayController : MonoBehaviour
    {
        [SerializeField] private DaySettings settings;
        [SerializeField] private RunController run;
        [SerializeField] private CombatController combat;
        [SerializeField] private TMP_Text dayNumber;
        [SerializeField] private TMP_Text remainingTime;
        [SerializeField] private GameObject bossNotice;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text clearedDay;
        [SerializeField] private TMP_Text goldAmount;
        [SerializeField] private TMP_Text specialAmount;
        [SerializeField] private GameObject endingLabel;
        [SerializeField] private Button returnButton;

        private SceneTravel travel;
        private RiceHarvest harvest;
        private DaySettings.Stage stage;
        private bool bossSpawned;
        public int Day { get; private set; } = 1;
        public bool AwaitingResult { get; private set; }
        public bool CampaignComplete { get; private set; }
        public bool IsBossDay => Day % settings.bossInterval == 0;

        private void Start()
        {
            travel = FindFirstObjectByType<SceneTravel>();
            harvest = FindFirstObjectByType<RiceHarvest>(FindObjectsInactive.Include);
            if (harvest != null) Day = Mathf.Max(1, harvest.Day);
            run.RunStarted += BeginStage;
            run.RunEnded += EndStage;
            returnButton.onClick.AddListener(CollectAndReturn);
            resultPanel.SetActive(false);
            RefreshDay();
        }

        private void OnDestroy()
        {
            if (run == null) return;
            run.RunStarted -= BeginStage;
            run.RunEnded -= EndStage;
        }

        private void RefreshDay()
        {
            stage = settings.GetStage(Day);
            dayNumber.text = Day.ToString();
            bossNotice.SetActive(IsBossDay);
        }

        private void BeginStage()
        {
            RefreshDay();
            bossSpawned = false;
            combat.Configure(stage.enemy, stage.spawnInterval, stage.maximumEnemies);
            UpdateProgress();
        }

        private void EndStage()
        {
            bossSpawned = false;
        }

        private void LateUpdate() => UpdateProgress();

        public void UpdateProgress()
        {
            if (!run.IsRunning || AwaitingResult || CampaignComplete) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, stage.survivalSeconds - run.Elapsed));
            remainingTime.text = $"{seconds / 60:00}:{seconds % 60:00}";
            if (run.Elapsed < stage.survivalSeconds) return;
            if (IsBossDay)
            {
                if (!bossSpawned) bossSpawned = combat.SpawnBoss(stage.boss);
                if (!combat.BossDefeated) return;
            }
            AwaitingResult = true;
            run.ReturnToPreparation();
            run.CanStart = false;
            clearedDay.text = Day.ToString();
            goldAmount.text = stage.goldReward.ToString();
            specialAmount.text = (IsBossDay ? stage.bossSpecialReward : 0).ToString();
            endingLabel.SetActive(Day >= settings.finalDay);
            resultPanel.SetActive(true);
        }

        public void CollectAndReturn()
        {
            if (!AwaitingResult) return;
            AwaitingResult = false;
            if (travel != null) travel.Village.AddStageRewards(stage.goldReward, IsBossDay ? stage.bossSpecialReward : 0);
            CampaignComplete = Day >= settings.finalDay;
            if (!CampaignComplete)
            {
                Day++;
                if (harvest != null) harvest.BeginDay(Day);
            }
            resultPanel.SetActive(false);
            run.CanStart = !CampaignComplete;
            RefreshDay();
            if (travel != null) travel.ReturnToVillage();
        }
    }
}
