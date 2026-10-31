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
            travel = FindFirstObjectByType<SceneTravel>(FindObjectsInactive.Include);
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
                stage.survivalSeconds, settings.waves, settings.enemySafetyLimit, settings.GetWaveLimit(Day), settings, Day);
            var miniBoss = GetComponent<MiniBossController>();
            if (miniBoss == null) miniBoss = gameObject.AddComponent<MiniBossController>();
            miniBoss.Configure(normal, stage.survivalSeconds,
                sharedHealth != null && sharedHealth.elite != null
                    ? sharedHealth.elite.Health(1) * settings.HealthScale(Day) : 0, 1f / 3f);
            EnsureWaveLabel();
            UpdateProgress();
        }

        private void EndStage()
        {
            if (waveLabel != null) waveLabel.gameObject.SetActive(false);
            bossSpawned = false;
            ReleaseRuntimeDefinitions();
        }

        private EnemyDefinition SharedDefinition(EnemyDefinition source)
        {
            if (source == null) return null;
            EnemyDefinition copy = Instantiate(source);
            copy.health = (sharedHealth != null ? sharedHealth.AdventureHealth(source, 1) : source.health) * settings.HealthScale(Day);
            copy.damage = source.damage * settings.DamageScale(Day);
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

        private TMP_Text waveLabel;
        private void EnsureWaveLabel()
        {
            if (waveLabel == null)
            {
                var rect = new GameObject("Adventure Wave Status", typeof(RectTransform)).GetComponent<RectTransform>();
                rect.SetParent(dayNumber.canvas.transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(28, -90); rect.sizeDelta = new Vector2(420, 70);
                waveLabel = rect.gameObject.AddComponent<TextMeshProUGUI>(); waveLabel.font = dayNumber.font;
                waveLabel.fontSize = 23; waveLabel.color = new Color(1, .9f, .65f); waveLabel.raycastTarget = false;
            }
            waveLabel.gameObject.SetActive(true);
        }

        public void UpdateProgress()
        {
            if (GameModeManager.Current != GameMode.Adventure) return;
            if (!run.IsRunning || run.IsPaused || run.IsChoosingUpgrade || run.CommanderDead || AwaitingResult || CampaignComplete || CampaignDefeated) return;
            int seconds = Mathf.CeilToInt(Mathf.Max(0, stage.survivalSeconds - run.Elapsed));
            if (waveLabel != null) waveLabel.text = combat.AdventureWaveLabel;
            remainingTime.text = $"{seconds / 60:00}:{seconds % 60:00}";
            if (run.Elapsed < stage.survivalSeconds) return;
            if (IsBossDay)
            {
                if (!bossSpawned) bossSpawned = combat.SpawnBoss(runtimeBoss != null ? runtimeBoss : stage.boss);
                if (!combat.BossDefeated) return;
            }
            victorySupportGold = GetComponent<CommanderProgression>()?.VictoryGold ?? 0;
            AwaitingResult = true;
            run.ReturnToPreparation();
            run.CanStart = false;
            clearedDay.text = Day.ToString();
            goldAmount.text = (stage.goldReward + victorySupportGold).ToString();
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

        private int victorySupportGold;

        public void CollectAndReturn()
        {
            if (!AwaitingResult) return;
            AwaitingResult = false;
            if (travel != null) travel.Village.AddStageRewards(stage.goldReward + victorySupportGold, IsBossDay ? stage.bossSpecialReward : 0);
            victorySupportGold = 0;
            CampaignComplete = Day >= settings.finalDay;
            int completedDay = Day;
            if (!CampaignComplete)
            {
                Day++;
                if (harvest != null) harvest.BeginDay(Day);
                GameModeManager.RecordAdventureDay(Day);
            }
            resultPanel.SetActive(false);
            run.CanStart = !CampaignComplete;
            RefreshDay();
            if (travel != null) travel.ReturnToVillage();
            var catalog = Resources.Load<ArmySurvivor.Equipment.EquipmentCatalog>("EquipmentCatalog");
            if (catalog != null && completedDay == catalog.firstSupplyDrawDay)
            {
                if (ArmySurvivor.Equipment.EquipmentInventory.Tickets == 0)
                    ArmySurvivor.Equipment.EquipmentInventory.AddTickets(catalog.firstDayTicketReward);
                ArmySurvivor.Tutorial.TutorialEvents.Raise("FirstSupplyDrawUnlocked");
            }
        }
    }
}
