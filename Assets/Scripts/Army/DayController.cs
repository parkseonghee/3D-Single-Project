using ArmySurvivor.Building;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class DayController : MonoBehaviour
    {
        [SerializeField] private DaySettings settings;
        [SerializeField] private RunController run;
        [SerializeField] private CombatController combat;
        [SerializeField] private TMP_Text dayNumber;
        [SerializeField] private TMP_Text dayLabel;
        [SerializeField] private TMP_Text remainingTime;
        [SerializeField] private GameObject bossNotice;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text clearedDay;
        [SerializeField] private TMP_Text goldAmount;
        [SerializeField] private TMP_Text specialAmount;
        [SerializeField] private GameObject endingLabel;
        [SerializeField] private Button returnButton;
        [Header("모험 모드 패배")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private TMP_Text defeatedDay;
        [SerializeField] private Button defeatToStartButton;
        [SerializeField] private string startSceneName = "StartScene";

        private SceneTravel travel;
        private RiceHarvest harvest;
        private DaySettings.Stage stage;
        private bool bossSpawned;
        private SurvivalSettings sharedHealth;
        private readonly List<EnemyDefinition> runtimeDefinitions = new List<EnemyDefinition>();
        private EnemyDefinition runtimeBoss;
        public int Day { get; private set; } = 1;
        public bool AwaitingResult { get; private set; }
        public bool CampaignComplete { get; private set; }
        public bool CampaignDefeated { get; private set; }
        public bool IsBossDay => Day % settings.bossInterval == 0;
        public RectTransform BattleTopHud => dayNumber != null ? dayNumber.transform.parent as RectTransform : null;

        private void Start()
        {
            if (GameModeManager.Current == GameMode.Survival)
            {
                resultPanel.SetActive(false);
                if (defeatPanel != null) defeatPanel.SetActive(false);
                bossNotice.SetActive(false);
                dayNumber.gameObject.SetActive(false);
                if (dayLabel != null) dayLabel.gameObject.SetActive(false);
                remainingTime.gameObject.SetActive(false);
                if (GetComponent<SurvivalController>() == null) gameObject.AddComponent<SurvivalController>();
                return;
            }
            travel = FindFirstObjectByType<SceneTravel>();
            sharedHealth = Resources.Load<SurvivalSettings>("SurvivalSettings");
            harvest = FindFirstObjectByType<RiceHarvest>(FindObjectsInactive.Include);
            if (harvest != null) Day = Mathf.Max(1, harvest.Day);
            run.RunStarted += BeginStage;
            run.RunEnded += EndStage;
            combat.CommanderDeathFinished += DefeatCampaign;
            returnButton.onClick.AddListener(CollectAndReturn);
            defeatToStartButton.onClick.AddListener(ReturnToStart);
            resultPanel.SetActive(false);
            defeatPanel.SetActive(false);
            RefreshDay();
        }

        private void OnDestroy()
        {
            if (run == null) return;
            run.RunStarted -= BeginStage;
            run.RunEnded -= EndStage;
            if (combat != null) combat.CommanderDeathFinished -= DefeatCampaign;
            if (returnButton != null) returnButton.onClick.RemoveListener(CollectAndReturn);
            if (defeatToStartButton != null) defeatToStartButton.onClick.RemoveListener(ReturnToStart);
            ReleaseRuntimeDefinitions();
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
            EnemyDefinition normal = SharedDefinition(stage.enemy);
            DaySettings.EnemySpawn[] pool = null;
            if (stage.enemies != null)
            {
                pool = new DaySettings.EnemySpawn[stage.enemies.Length];
                for (int i = 0; i < pool.Length; i++)
                    if (stage.enemies[i] != null)
                        pool[i] = new DaySettings.EnemySpawn {
                            enemy = SharedDefinition(stage.enemies[i].enemy), weight = stage.enemies[i].weight };
            }
            runtimeBoss = SharedDefinition(stage.boss);
            combat.Configure(normal, stage.spawnInterval, stage.maximumEnemies, pool,
                stage.survivalSeconds, settings.waves, settings.enemySafetyLimit, settings.GetWaveLimit(Day));
            var miniBoss = GetComponent<MiniBossController>();
            if (miniBoss == null) miniBoss = gameObject.AddComponent<MiniBossController>();
            miniBoss.Configure(normal, stage.survivalSeconds,
                sharedHealth != null && sharedHealth.elite != null
                    ? sharedHealth.elite.Health(sharedHealth.AdventureTier(Day)) : 0);
            UpdateProgress();
        }

        private void EndStage()
        {
            bossSpawned = false;
            ReleaseRuntimeDefinitions();
        }

        private EnemyDefinition SharedDefinition(EnemyDefinition source)
        {
            if (source == null || sharedHealth == null) return source;
            EnemyDefinition copy = Instantiate(source);
            copy.health = sharedHealth.AdventureHealth(source, Day);
            runtimeDefinitions.Add(copy);
            return copy;
        }

        private void ReleaseRuntimeDefinitions()
        {
            foreach (EnemyDefinition definition in runtimeDefinitions)
                if (definition != null) Destroy(definition);
            runtimeDefinitions.Clear();
            runtimeBoss = null;
        }

        private void LateUpdate() => UpdateProgress();

        public void UpdateProgress()
        {
            if (GameModeManager.Current != GameMode.Adventure) return;
            if (!run.IsRunning || run.IsPaused || run.IsChoosingUpgrade || run.CommanderDead || AwaitingResult || CampaignComplete || CampaignDefeated) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, stage.survivalSeconds - run.Elapsed));
            remainingTime.text = $"{seconds / 60:00}:{seconds % 60:00}";
            if (run.Elapsed < stage.survivalSeconds) return;
            if (IsBossDay)
            {
                if (!bossSpawned) bossSpawned = combat.SpawnBoss(runtimeBoss != null ? runtimeBoss : stage.boss);
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

        private void DefeatCampaign()
        {
            if (GameModeManager.Current != GameMode.Adventure || CampaignDefeated || AwaitingResult || CampaignComplete) return;
            CampaignDefeated = true;
            run.CanStart = false;
            run.ReturnToPreparation();
            resultPanel.SetActive(false);
            defeatedDay.text = Day.ToString();
            defeatPanel.SetActive(true);
        }

        private void ReturnToStart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
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
