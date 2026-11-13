using ArmySurvivor.Building;
using ArmySurvivor.Saving;
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
        [SerializeField] private GameObject endingLabel;
        [SerializeField] private Button returnButton;
        [Header("베테랑 (기획서 4.6)")]
        [SerializeField] private VeteranProgression veterans;
        [Header("모험 모드 패배")]
        [SerializeField] private GameObject defeatPanel;
        [SerializeField] private TMP_Text defeatedDay;
        [SerializeField] private Button defeatToStartButton;
        [SerializeField] private string startSceneName = "StartScene";
        [Header("재도전권 (기획서 8)")]
        [SerializeField] private CampaignRetry retry;
        [SerializeField] private TMP_Text resultRetryInfo;
        [SerializeField] private TMP_Text defeatRetryInfo;

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
        public DaySettings Settings => settings;
        // 출전 미리보기와 전투가 같은 값을 쓰도록 Day에서 바로 계산한다.
        public BattlefieldModifierDefinition CurrentModifier => settings.GetBattlefieldModifier(Day);
        public event System.Action DayChanged;
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
            run.RetreatRequested += RetreatRequested;
            combat.SpecialWaveAnnounced += ShowSpecialWaveNotice;
            returnButton.onClick.AddListener(ReturnClicked);
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
            run.RetreatRequested -= RetreatRequested;
            if (combat != null) combat.CommanderDeathFinished -= DefeatCampaign;
            if (combat != null) combat.SpecialWaveAnnounced -= ShowSpecialWaveNotice;
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnClicked);
            if (defeatToStartButton != null) defeatToStartButton.onClick.RemoveListener(ReturnToStart);
            ReleaseRuntimeDefinitions();
        }

        private void RefreshDay()
        {
            stage = settings.GetStage(Day);
            dayNumber.text = Day.ToString();
            bossNotice.SetActive(IsBossDay);
            DayChanged?.Invoke();
        }

        private void BeginStage()
        {
            RefreshDay();
            bossSpawned = false;
            BattlefieldModifierDefinition modifier = CurrentModifier;
            combat.BattlefieldModifier = modifier;
            EnemyDefinition normal = SharedDefinition(stage.enemy, modifier);
            DaySettings.EnemySpawn[] pool = null;
            if (stage.enemies != null)
            {
                pool = new DaySettings.EnemySpawn[stage.enemies.Length];
                for (int i = 0; i < pool.Length; i++)
                    if (stage.enemies[i] != null)
                        pool[i] = new DaySettings.EnemySpawn {
                            enemy = SharedDefinition(stage.enemies[i].enemy, modifier),
                            // 복사본이 아닌 원본 에셋으로 대상 여부를 판정한다.
                            weight = stage.enemies[i].weight *
                                (modifier != null ? modifier.WeightMultiplier(stage.enemies[i].enemy) : 1f),
                            firstDay = stage.enemies[i].firstDay };
            }
            runtimeBoss = SharedDefinition(stage.boss, null);
            combat.Configure(normal, stage.spawnInterval, stage.maximumEnemies, pool,
                stage.survivalSeconds, settings.waves, settings.enemySafetyLimit, settings.GetWaveLimit(Day), settings, Day);
            var miniBoss = GetComponent<MiniBossController>();
            if (miniBoss == null) miniBoss = gameObject.AddComponent<MiniBossController>();
            miniBoss.Configure(normal, stage.survivalSeconds,
                sharedHealth != null && sharedHealth.elite != null
                    ? sharedHealth.elite.Health(1) * settings.HealthScale(Day) : 0, 1f / 3f);
            EnsureWaveLabel();
            EnsureSpecialWaveNotice();
            specialWaveNotice.gameObject.SetActive(false);
            specialWaveNoticeRemaining = 0;
            UpdateProgress();
            // 전투 중 강제 종료 후 이어하기도 재도전권을 쓰도록 표시한다.
            GameSave.MarkBattleInProgress(true);
        }

        private void EndStage()
        {
            if (waveLabel != null) waveLabel.gameObject.SetActive(false);
            if (specialWaveNotice != null) specialWaveNotice.gameObject.SetActive(false);
            bossSpawned = false;
            combat.BattlefieldModifier = null;
            ReleaseRuntimeDefinitions();
        }

        // modifier는 일반 적에만 넘긴다. 보스는 null로 호출해 변수 영향을 받지 않는다.
        private EnemyDefinition SharedDefinition(EnemyDefinition source, BattlefieldModifierDefinition modifier)
        {
            if (source == null) return null;
            EnemyDefinition copy = Instantiate(source);
            copy.health = (sharedHealth != null ? sharedHealth.AdventureHealth(source, 1) : source.health) * settings.HealthScale(Day)
                * (modifier != null ? modifier.EnemyHealthMultiplier : 1f);
            copy.damage = source.damage * settings.DamageScale(Day);
            copy.speed = source.speed * (modifier != null ? modifier.EnemySpeedMultiplier : 1f);
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

        private void LateUpdate()
        {
            UpdateProgress();
            if (specialWaveNotice == null || specialWaveNoticeRemaining <= 0) return;
            if (run.IsChoosingUpgrade || run.IsPaused)
            {
                specialWaveNotice.gameObject.SetActive(false);
                return;
            }
            if (!specialWaveNotice.gameObject.activeSelf) specialWaveNotice.gameObject.SetActive(true);
            specialWaveNoticeRemaining = Mathf.Max(0, specialWaveNoticeRemaining - Time.unscaledDeltaTime);
            Color color = specialWaveNotice.color;
            color.a = Mathf.Clamp01(specialWaveNoticeRemaining / .5f);
            specialWaveNotice.color = color;
            if (specialWaveNoticeRemaining <= 0) specialWaveNotice.gameObject.SetActive(false);
        }

        private TMP_Text waveLabel;
        private TMP_Text specialWaveNotice;
        private float specialWaveNoticeRemaining;
        private void EnsureSpecialWaveNotice()
        {
            if (specialWaveNotice != null) return;
            var rect = new GameObject("Adventure Special Wave Notice", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(dayNumber.canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -175);
            rect.sizeDelta = new Vector2(800, 70);
            specialWaveNotice = rect.gameObject.AddComponent<TextMeshProUGUI>();
            specialWaveNotice.font = dayNumber.font;
            specialWaveNotice.fontSize = 38;
            specialWaveNotice.fontStyle = FontStyles.Bold;
            specialWaveNotice.alignment = TextAlignmentOptions.Center;
            specialWaveNotice.color = new Color(1, .84f, .45f);
            specialWaveNotice.raycastTarget = false;
        }
        private void ShowSpecialWaveNotice(string waveName)
        {
            EnsureSpecialWaveNotice();
            specialWaveNotice.text = waveName + " 웨이브";
            specialWaveNotice.color = new Color(1, .84f, .45f);
            specialWaveNoticeRemaining = 3f;
            specialWaveNotice.gameObject.SetActive(true);
        }
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
            GameSave.MarkBattleInProgress(false);
            // 전장 성장은 RunEnded에서 지워지므로 그 전에 베테랑 자격을 기록한다. 마지막 Day는 다음 전장이 없어 생략한다.
            if (veterans != null && Day < settings.finalDay) veterans.CaptureClear();
            run.ReturnToPreparation();
            run.CanStart = false;
            clearedDay.text = Day.ToString();
            goldAmount.text = (stage.goldReward + victorySupportGold).ToString();
            endingLabel.SetActive(Day >= settings.finalDay);
            if (resultRetryInfo != null && retry != null) resultRetryInfo.text = retry.TokensLabel(CampaignState.RetryTokens);
            resultPanel.SetActive(true);
        }

        private void DefeatCampaign() => Fail(false);

        // 일시정지 메뉴의 '준비로'. 전투 결과가 정해진 뒤에는 기존처럼 바로 돌아간다.
        private void RetreatRequested()
        {
            if (AwaitingResult || CampaignComplete || CampaignDefeated || run.CommanderDead || retry == null)
            { run.ReturnToPreparation(); return; }
            if (retry.IsOpen) return;
            retry.ShowRetreatConfirm(Day, () => Fail(true));
        }

        // 사망과 중도 복귀를 같은 규칙으로 처리한다: 재도전권 1장 → Day 아침 재시작, 0장 → 회차 종료.
        private void Fail(bool retreat)
        {
            if (GameModeManager.Current != GameMode.Adventure || CampaignDefeated || AwaitingResult || CampaignComplete) return;
            CampaignDefeated = true;
            run.CanStart = false;
            run.ReturnToPreparation();
            resultPanel.SetActive(false);
            if (retry != null && GameSave.ConsumeRetryToken(out int remaining))
            {
                retry.ShowRetry(Day, remaining, retreat);
                return;
            }
            GameSave.DeleteAutoSave();
            defeatedDay.text = Day.ToString();
            if (defeatRetryInfo != null && retry != null) defeatRetryInfo.text = retry.TokensLabel(0);
            defeatPanel.SetActive(true);
        }

        private void ReturnToStart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
        }

        private int victorySupportGold;

        // 결과창 귀환 버튼: 베테랑 자격자가 있으면 선택 화면을 먼저 띄우고, 끝나면 귀환한다.
        private void ReturnClicked()
        {
            if (!AwaitingResult || (veterans != null && veterans.IsOpen)) return;
            if (veterans != null && veterans.TryOpen(CollectAndReturn))
            {
                resultPanel.SetActive(false);
                return;
            }
            CollectAndReturn();
        }

        public void CollectAndReturn()
        {
            if (!AwaitingResult) return;
            AwaitingResult = false;
            if (travel != null) travel.Village.AddStageRewards(stage.goldReward + victorySupportGold);
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
            // 마을 도착 = 다음 Day 아침(보상·보급권 지급 후). 회차를 끝냈으면 이어할 저장을 지운다.
            if (CampaignComplete) GameSave.DeleteAutoSave();
            else FindFirstObjectByType<GameSave>(FindObjectsInactive.Include)?.WriteAutoSave();
        }
    }
}
