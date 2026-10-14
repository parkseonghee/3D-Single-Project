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
        private Canvas canvas;
        private TMP_Text timeText, killsText, bestText, alert, bossTitle, resultText;
        private Image bossFill;
        private GameObject bossBar, resultPanel;
        private float alertUntil;

        private void Awake()
        {
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
            BuildDefinitions();
        }

        private void Start()
        {
            if (!enabled) return;
            run.HideDefaultElapsed();
            BuildUI();
            run.RunStarted += Begin;
            run.RunEnded += OnRunEnded;
            combat.EnemyDefeated += CountKill;
        }

        private void OnDestroy()
        {
            if (run != null) { run.RunStarted -= Begin; run.RunEnded -= OnRunEnded; }
            if (combat != null) combat.EnemyDefeated -= CountKill;
            if (activeBoss != null) activeBoss.OnBossDefeated -= BossDefeated;
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
            SceneManager.LoadScene("StartScene", LoadSceneMode.Single);
        }

        private void ShowAlert(string value)
        {
            alert.text = value;
            alert.gameObject.SetActive(true);
            alertUntil = Time.unscaledTime + 2.5f;
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

        private void BuildUI()
        {
            var font = UiFontLocator.ExistingKoreanFont();
            var root = new GameObject("Survival UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 250;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var topHud = GetComponent<DayController>().BattleTopHud;
            if (topHud == null) { Debug.LogError("Battle Top HUD 연결이 없습니다.", this); return; }
            timeText = TopLabel(topHud, "Survival Time", new Vector2(16, -7), new Vector2(180, 26), 23, font);
            killsText = TopLabel(topHud, "Survival Kills", new Vector2(205, -7), new Vector2(190, 26), 23, font);
            bestText = TopLabel(topHud, "Survival Best", new Vector2(16, -35), new Vector2(380, 24), 20, font);
            SetHudVisible(false);
            alert = Label(root.transform, "Alert", "", 72, new Vector2(0, 260), new Vector2(900, 100), font);
            alert.gameObject.SetActive(false);
            var bossRect = Rect(root.transform, "Boss Health", new Vector2(0, 420), new Vector2(750, 65));
            bossBar = bossRect.gameObject;
            bossBar.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.93f);
            bossTitle = Label(bossRect, "Boss Label", "BOSS", 28, new Vector2(0, 12), new Vector2(700, 36), font);
            var fillRect = Rect(bossRect, "Fill", new Vector2(0, -18), new Vector2(690, 16));
            bossFill = fillRect.gameObject.AddComponent<Image>();
            bossFill.color = new Color(0.85f, 0.12f, 0.1f);
            bossFill.type = Image.Type.Filled;
            bossFill.fillMethod = Image.FillMethod.Horizontal;
            bossBar.SetActive(false);
            var resultRect = Rect(root.transform, "Survival Result", Vector2.zero, new Vector2(760, 600));
            resultPanel = resultRect.gameObject;
            resultPanel.AddComponent<Image>().color = new Color(0.025f, 0.06f, 0.08f, 0.97f);
            resultText = Label(resultRect, "Results", "", 38, new Vector2(0, 90), new Vector2(700, 360), font);
            Button(resultRect, "다시 하기", new Vector2(-180, -225), font, Retry);
            Button(resultRect, "시작 화면", new Vector2(180, -225), font, ToStart);
            resultPanel.SetActive(false);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static TMP_Text Label(Transform parent, string name, string value, float size, Vector2 position, Vector2 dimensions, TMP_FontAsset font)
        {
            var label = Rect(parent, name, position, dimensions).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.text = value;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1, 0.91f, 0.7f);
            label.raycastTarget = false;
            return label;
        }

        private static TMP_Text TopLabel(RectTransform parent, string name, Vector2 position, Vector2 dimensions,
            float size, TMP_FontAsset font)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.Left;
            label.color = new Color(1, 0.91f, 0.7f);
            label.raycastTarget = false;
            return label;
        }

        private static void Button(Transform parent, string value, Vector2 position, TMP_FontAsset font, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(parent, value, position, new Vector2(290, 70));
            rect.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.16f, 0.26f);
            rect.gameObject.AddComponent<Button>().onClick.AddListener(action);
            Label(rect, "Label", value, 28, Vector2.zero, rect.sizeDelta, font);
        }
    }
}
