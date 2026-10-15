using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    // Lives beside DayController on the PlayScene recruitment system. It is enabled only in Survival.
    public sealed class SurvivalController : MonoBehaviour
    {
        [SerializeField] private SurvivalSettings settings;
        [Header("씬에 배치한 서바이벌 UI")]
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text alert;
        [SerializeField] private TMP_Text bossTitle;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Image bossFill;
        [SerializeField] private GameObject bossBar;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button startSceneButton;
        [SerializeField, Min(0f)] private float alertDuration = 2.5f;
        [SerializeField] private string startSceneName = "StartScene";
        private RunController run;
        private CombatController combat;
        private MiniBossController eliteCards;
        private EnemyDefinition[,] normalDefinitions;
        private EnemyDefinition eliteDefinition;
        private EnemyDefinition bossDefinition;
        private float nextElite;
        private float nextBoss;
        private int bossesSpawned;
        private float lastBossHealth;
        private int normalKills, eliteKills, bossKills;
        private bool ended, newRecordShown;
        private float bestAtStart;
        private float originalTimeScale = 1;
        private BossUnit activeBoss;
        private float alertUntil;

        private void Awake()
        {
            if (GameModeManager.Current != GameMode.Survival)
            {
                enabled = false;
                return;
            }
            if (settings == null) settings = Resources.Load<SurvivalSettings>("SurvivalSettings");
            run = GetComponent<RunController>();
            combat = GetComponent<CombatController>();
            eliteCards = GetComponent<MiniBossController>();
            if (settings == null || run == null || combat == null)
            {
                Debug.LogError("서바이벌 설정 또는 전투 컴포넌트가 없습니다.", this);
                enabled = false;
                return;
            }
            if (settings.monsters == null || settings.monsters.Length == 0 || settings.monsters[0].definition == null)
            {
                Debug.LogError("서바이벌 몬스터 목록을 연결하세요.", this);
                enabled = false;
                return;
            }
            if (overlayRoot == null || timeText == null || killsText == null || bestText == null ||
                alert == null || bossTitle == null || resultText == null || bossFill == null ||
                bossBar == null || resultPanel == null || retryButton == null || startSceneButton == null)
            {
                Debug.LogError("서바이벌 UI 참조를 PlayScene에서 연결하세요.", this);
                enabled = false;
                return;
            }
            BuildDefinitions();
        }

        private void Start()
        {
            if (!enabled) return;
            run.HideDefaultElapsed();
            overlayRoot.SetActive(true);
            SetHudVisible(false);
            alert.gameObject.SetActive(false);
            bossBar.SetActive(false);
            resultPanel.SetActive(false);
            retryButton.onClick.AddListener(Retry);
            startSceneButton.onClick.AddListener(ToStart);
            run.RunStarted += Begin;
            run.RunEnded += OnRunEnded;
            combat.EnemyDefeated += CountKill;
        }

        private void OnDestroy()
        {
            if (run != null) { run.RunStarted -= Begin; run.RunEnded -= OnRunEnded; }
            if (combat != null) combat.EnemyDefeated -= CountKill;
            if (activeBoss != null) activeBoss.OnBossDefeated -= BossDefeated;
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (startSceneButton != null) startSceneButton.onClick.RemoveListener(ToStart);
            if (normalDefinitions != null)
                foreach (var definition in normalDefinitions) if (definition != null) Destroy(definition);
            if (eliteDefinition != null) Destroy(eliteDefinition);
            if (bossDefinition != null) Destroy(bossDefinition);
        }

        private void BuildDefinitions()
        {
            normalDefinitions = new EnemyDefinition[settings.monsters.Length, 3];
            for (int i = 0; i < settings.monsters.Length; i++)
            {
                var entry = settings.monsters[i];
                if (entry == null || entry.definition == null) continue;
                for (int tier = 1; tier <= 3; tier++)
                {
                    var copy = Instantiate(entry.definition);
                    copy.health = entry.Health(tier);
                    normalDefinitions[i, tier - 1] = copy;
                }
            }
            if (settings.elite != null && settings.elite.definition != null)
                eliteDefinition = Instantiate(settings.elite.definition);
            if (settings.boss != null && settings.boss.definition != null)
                bossDefinition = Instantiate(settings.boss.definition);
        }

        private void Begin()
        {
            ended = newRecordShown = false;
            normalKills = eliteKills = bossKills = bossesSpawned = 0;
            lastBossHealth = 0;
            nextElite = settings.eliteInterval;
            nextBoss = settings.firstBossSeconds;
            bestAtStart = RecordManager.Best?.survivalSeconds ?? 0;
            activeBoss = null;
            combat.SetSurvivalBossAlive(false);
            combat.ConfigureSurvival(new SurvivalWaveRule(settings, normalDefinitions), settings, run.PlayCamera);
            if (eliteCards != null) eliteCards.ConfigureSurvival();
            resultPanel.SetActive(false);
            bossBar.SetActive(false);
            SetHudVisible(true);
            originalTimeScale = Time.timeScale;
        }

        private void Update()
        {
            if (!run.IsRunning || ended) return;
            float elapsed = run.Elapsed;
            if (run.CommanderDead) { Finish(); return; }
            if (run.IsPaused || run.IsChoosingUpgrade) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleTestKeys();
#endif
            if (elapsed >= nextElite && eliteDefinition != null)
            {
                eliteDefinition.health = settings.elite.Health(settings.Tier(elapsed)) * settings.HealthMultiplier(elapsed);
                if (eliteCards != null && eliteCards.SpawnSurvivalElite(eliteDefinition))
                    nextElite = (Mathf.FloorToInt(elapsed / settings.eliteInterval) + 1) * settings.eliteInterval;
            }
            if (settings.enableBoss && elapsed >= nextBoss && activeBoss == null)
            {
                if (SpawnBoss()) nextBoss = settings.firstBossSeconds +
                    (Mathf.FloorToInt((elapsed - settings.firstBossSeconds) / settings.bossRepeatSeconds) + 1) *
                    settings.bossRepeatSeconds;
            }
            timeText.text = "생존 " + RecordManager.TimeLabel(elapsed);
            killsText.text = "처치 " + (normalKills + eliteKills + bossKills);
            bestText.text = "최고 기록 " + RecordManager.TimeLabel(bestAtStart);
            if (!newRecordShown && elapsed > bestAtStart && elapsed >= 1)
            {
                newRecordShown = true;
                ShowAlert("NEW RECORD!");
            }
            if (alert.gameObject.activeSelf && Time.unscaledTime >= alertUntil) alert.gameObject.SetActive(false);
            if (activeBoss != null && activeBoss.IsAlive)
                bossFill.fillAmount = activeBoss.Health.Current / activeBoss.Health.Maximum;
        }

        private bool SpawnBoss()
        {
            if (bossDefinition == null) return false;
            bool fallback = settings.bossPrefab == null;
            bossDefinition.prefab = fallback ? settings.elite?.definition?.prefab : settings.bossPrefab;
            if (bossDefinition.prefab == null) return false;
            bossDefinition.health = bossesSpawned == 0 ? settings.boss.tier3Health :
                lastBossHealth * settings.bossHealthMultiplier;
            GameObject model = combat.SpawnSpecial(bossDefinition, true);
            if (model == null) return false;
            if (fallback)
            {
                model.transform.localScale *= settings.fallbackBossScale;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", settings.fallbackBossTint);
                block.SetColor("_Color", settings.fallbackBossTint);
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) renderer.SetPropertyBlock(block);
            }
            activeBoss = model.GetComponent<BossUnit>();
            if (activeBoss == null) activeBoss = model.AddComponent<BossUnit>();
            activeBoss.Initialize(model.GetComponent<UnitHealth>());
            activeBoss.OnBossDefeated += BossDefeated;
            bossesSpawned++;
            lastBossHealth = bossDefinition.health;
            combat.SetSurvivalBossAlive(true);
            bossTitle.text = "BOSS";
            bossFill.fillAmount = 1;
            bossBar.SetActive(true);
            ShowAlert("BOSS");
            return true;
        }

        private void BossDefeated(BossUnit boss)
        {
            boss.OnBossDefeated -= BossDefeated;
            activeBoss = null;
            combat.SetSurvivalBossAlive(false);
            bossBar.SetActive(false);
        }

        private void CountKill(bool elite, bool boss)
        {
            if (boss) bossKills++;
            else if (elite) eliteKills++;
            else normalKills++;
        }

        private void Finish()
        {
            if (ended) return;
            ended = true;
            float seconds = run.Elapsed;
            var previous = RecordManager.Best;
            var record = new SurvivalRecord {
                survivalSeconds = seconds, normalKills = normalKills, eliteKills = eliteKills, bossKills = bossKills,
                score = Mathf.FloorToInt(seconds) * settings.secondsScore + normalKills * settings.normalKillScore +
                    eliteKills * settings.eliteKillScore + bossKills * settings.bossKillScore
            };
            bool best = RecordManager.Save(record);
            run.ReturnToPreparation();
            SetHudVisible(false);
            bossBar.SetActive(false);
            resultPanel.SetActive(true);
            resultText.text = $"{(best ? "NEW RECORD!\n" : "")}생존 {RecordManager.TimeLabel(seconds)}\n" +
                $"일반 처치 {normalKills}   정예 {eliteKills}   보스 {bossKills}\n" +
                $"점수 {record.score:N0}\n이전 최고 {RecordManager.TimeLabel(previous?.survivalSeconds ?? 0)}";
        }

        private void OnRunEnded()
        {
            if (activeBoss != null) activeBoss.OnBossDefeated -= BossDefeated;
            activeBoss = null;
            combat.SetSurvivalBossAlive(false);
            Time.timeScale = originalTimeScale;
        }

        private void Retry()
        {
            resultPanel.SetActive(false);
            run.BeginRun();
        }

        private void ToStart()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(startSceneName, LoadSceneMode.Single);
        }

        private void ShowAlert(string value)
        {
            alert.text = value;
            alert.gameObject.SetActive(true);
            alertUntil = Time.unscaledTime + alertDuration;
        }

        private void SetHudVisible(bool visible)
        {
            timeText.gameObject.SetActive(visible);
            killsText.gameObject.SetActive(visible);
            bestText.gameObject.SetActive(visible);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void HandleTestKeys()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) Time.timeScale = Time.timeScale > 1 ? 1 : 2;
            if (keyboard.f2Key.wasPressedThisFrame)
            {
                float elapsed = run.Elapsed;
                float next = elapsed < settings.tier2Seconds ? settings.tier2Seconds :
                    elapsed < settings.tier3Seconds ? settings.tier3Seconds :
                    elapsed < settings.scalingStartSeconds ? settings.scalingStartSeconds : elapsed + 300;
                run.JumpElapsedForTesting(next);
            }
            if (keyboard.f3Key.wasPressedThisFrame && settings.enableBoss && activeBoss == null) SpawnBoss();
            if (keyboard.f4Key.wasPressedThisFrame)
            { RecordManager.Clear(); bestAtStart = 0; newRecordShown = false; }
        }
#endif

    }
}
