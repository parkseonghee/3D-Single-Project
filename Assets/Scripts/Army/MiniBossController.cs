using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    // Independent reward draft; never replaces the soldier's level-up choices.
    public sealed class MiniBossController : MonoBehaviour
    {
        [SerializeField, Range(0.1f, 0.9f)] private float spawnProgress = 0.5f;
        [SerializeField, Min(1)] private float healthMultiplier = 5f;
        [SerializeField, Min(1)] private float damageMultiplier = 1.5f;
        [SerializeField, Min(1)] private float explosionDamage = 500f;
        [SerializeField, Min(0.1f)] private float invulnerabilitySeconds = 5f;
        [SerializeField, Min(1)] private int bottleReward = 10;
        [Header("즉시 카드 버프")]
        [SerializeField, Min(0.1f)] private float speedDuration = 15f;
        [SerializeField, Min(0)] private float speedBonus = 0.5f;
        [SerializeField, Min(0.1f)] private float warCryDuration = 20f;
        [SerializeField, Min(0)] private float warCryBonus = 0.5f;
        [Header("등급 추첨 가중치")]
        [SerializeField, Min(0)] private float commonWeight = 55;
        [SerializeField, Min(0)] private float rareWeight = 30;
        [SerializeField, Min(0)] private float epicWeight = 15;
        [Header("테스트")]
        [SerializeField] private bool guaranteeDiablo;
        public enum Reward { Explosion, Protection, Bottles, Healing, Speed, Mines, WarCry, Diablo }
        private readonly List<Reward> offered = new List<Reward>();
        public IReadOnlyList<Reward> OfferedRewards => offered.AsReadOnly();
        private readonly TMP_Text[] cardTitles = new TMP_Text[3];
        private readonly TMP_Text[] cardDescriptions = new TMP_Text[3];
        private readonly TMP_Text[] cardRarities = new TMP_Text[3];
        private readonly Image[] cardBackgrounds = new Image[3];
        private RunController run;
        private CombatController combat;
        private BattleExperience experience;
        private EnemyDefinition definition;
        private float spawnAt;
        private bool spawned;
        private bool survivalMode;
        private int pending;
        private bool choosing;
        private float previousTimeScale;
        private GameObject panel;
        private TMP_Text notice;
        private float noticeUntil;
        public bool IsChoosing => choosing;
        public bool HasSpawned => spawned;

        private void Awake()
        {
            run = GetComponent<RunController>();
            combat = GetComponent<CombatController>();
            experience = GetComponent<BattleExperience>();
        }

        private void OnEnable()
        {
            combat.MiniBossKilled += QueueReward;
            run.RunEnded += ResetRun;
        }

        public void Configure(EnemyDefinition source, float survivalSeconds)
            => Configure(source, survivalSeconds, 0);

        public void Configure(EnemyDefinition source, float survivalSeconds, float sharedHealth)
        {
            ResetRun();
            survivalMode = false;
            if (source == null) return;
            definition = Instantiate(source);
            definition.name = "MiniBoss Runtime Definition";
            definition.health = sharedHealth > 0 ? sharedHealth : definition.health * healthMultiplier;
            definition.damage *= damageMultiplier;
            spawnAt = survivalSeconds * spawnProgress;
        }

        public void ConfigureSurvival()
        {
            ResetRun();
            survivalMode = true;
        }

        public bool SpawnSurvivalElite(EnemyDefinition eliteDefinition)
        {
            if (!survivalMode || eliteDefinition == null) return false;
            bool created = combat.SpawnMiniBoss(eliteDefinition);
            if (created) ShowNotice("중간 보스 · 정예 오크 등장!");
            return created;
        }

        private void Update()
        {
            if (!run.IsRunning || run.CommanderDead) return;
            if (notice != null && Time.time >= noticeUntil) notice.gameObject.SetActive(false);
            if (run.IsPaused || run.IsChoosingUpgrade) return;
            if (!survivalMode && !spawned && definition != null && run.Elapsed >= spawnAt)
            {
                spawned = combat.SpawnMiniBoss(definition);
                if (spawned) ShowNotice("중간 보스 · 정예 오크 등장!");
            }
            if (pending > 0 && !experience.IsChoosingUpgrade) OpenReward();
        }

        private void QueueReward()
        {
            pending++;
            // Stop the stage timer immediately, including on the final survival frame.
            if (!run.IsPaused && !run.IsChoosingUpgrade && !experience.IsChoosingUpgrade) OpenReward();
        }

        private void OpenReward()
        {
            if (choosing || pending <= 0 || !run.IsRunning) return;
            EnsureUI();
            RollRewards();
            pending--;
            choosing = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            run.IsChoosingUpgrade = true;
            panel.SetActive(true);
        }

        public bool ChooseReward(int choice)
        {
            if (!choosing || !run.IsRunning || choice < 0 || choice >= offered.Count) return false;
            Reward reward = offered[choice];
            choosing = false;
            panel.SetActive(false);
            // Keep the run locked until the chosen effect has been applied.
            switch (reward)
            {
                case Reward.Explosion: combat.ApplyRewardExplosion(explosionDamage); break;
                case Reward.Protection: combat.ApplyRewardInvulnerability(invulnerabilitySeconds); break;
                case Reward.Bottles: experience.GrantBottles(bottleReward); break;
                case Reward.Healing: combat.ApplyRewardHealing(); break;
                case Reward.Speed: run.GrantRewardSpeed(speedDuration, speedBonus); break;
                case Reward.Mines: combat.ApplyRewardMines(); break;
                case Reward.WarCry: combat.ApplyRewardWarCry(warCryDuration, warCryBonus); break;
                case Reward.Diablo: GetComponent<DiabloController>().Activate(); break;
            }
            run.IsChoosingUpgrade = false;
            Time.timeScale = previousTimeScale;
            ShowNotice(RewardTitle(reward) + " 발동!");
            return true;
        }

        private void ResetRun()
        {
            if (choosing)
            {
                Time.timeScale = previousTimeScale;
                run.IsChoosingUpgrade = false;
            }
            choosing = spawned = false;
            pending = 0;
            offered.Clear();
            if (panel != null) panel.SetActive(false);
            if (notice != null) notice.gameObject.SetActive(false);
            if (definition != null) Destroy(definition);
            definition = null;
        }

        private GameObject uiRoot;
        [SerializeField] private TMP_FontAsset font;
        private void EnsureUI()
        {
            if (uiRoot != null) return;
            var label = GetComponentInChildren<TMP_Text>(true);
            if (font == null) font = label != null ? label.font : TMP_Settings.defaultFontAsset;
            uiRoot = new GameObject("Instant Reward Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            uiRoot.transform.SetParent(transform, false);
            var canvas = uiRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = uiRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var overlay = Rect("Instant Reward Selection", uiRoot.transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            panel = overlay.gameObject;
            panel.AddComponent<Image>().color = new Color(0.015f, 0.045f, 0.06f, 0.94f);
            Text("Title", overlay, new Vector2(0, 260), new Vector2(1200, 80), "중간 보스 처치 · 즉시 효과 선택", 42);
            for (int i = 0; i < 3; i++)
            {
                int choice = i;
                var card = Rect("Reward " + i, overlay, new Vector2((i - 1) * 430, 0), new Vector2(390, 370));
                cardBackgrounds[i] = card.gameObject.AddComponent<Image>();
                card.gameObject.AddComponent<Button>().onClick.AddListener(() => ChooseReward(choice));
                cardRarities[i] = Text("Rarity", card, new Vector2(0, 155), new Vector2(360, 35), "", 22);
                cardTitles[i] = Text("Title", card, new Vector2(0, 108), new Vector2(360, 65), "", 34);
                cardDescriptions[i] = Text("Description", card, new Vector2(0, -10), new Vector2(350, 175), "", 25);
                Text("Choose", card, new Vector2(0, -130), new Vector2(340, 50), "선택 · 즉시 발동", 28);
            }
            Text("Help", overlay, new Vector2(0, -265), new Vector2(1200, 70), "한 장을 선택하면 효과가 발동하고 전투가 계속됩니다.", 26);
            notice = Text("Notice", uiRoot.transform, new Vector2(0, 380), new Vector2(1200, 70), "", 36);
            notice.gameObject.SetActive(false);
            panel.SetActive(false);
        }

        private void ShowNotice(string message)
        {
            EnsureUI(); notice.text = message; notice.gameObject.SetActive(true); noticeUntil = Time.time + 3f;
        }

        private static int Rarity(Reward reward)
        {
            if (reward == Reward.Bottles || reward == Reward.Speed) return 0;
            if (reward == Reward.Mines || reward == Reward.WarCry || reward == Reward.Diablo) return 2;
            return 1;
        }

        private void RollRewards()
        {
            offered.Clear();
            var remaining = new List<Reward>((Reward[])System.Enum.GetValues(typeof(Reward)));
            if (!combat.CanPlaceRewardMines) remaining.Remove(Reward.Mines);
            var diablo = GetComponent<DiabloController>();
            if (diablo == null || !diablo.IsConfigured) remaining.Remove(Reward.Diablo);
            float[] weights = { commonWeight, rareWeight, epicWeight };
            string[] names = { "일반", "희귀", "영웅" };
            Color[] colors = { new Color(0.7f, 0.83f, 0.76f), new Color(0.35f, 0.7f, 1), new Color(0.8f, 0.45f, 1) };
            for (int i = 0; i < 3; i++)
            {
                // Roll an available tier, then choose uniformly within it. Remove the chosen card.
                float total = 0;
                for (int tier = 0; tier < 3; tier++)
                    if (remaining.Exists(r => Rarity(r) == tier)) total += Mathf.Max(0, weights[tier]);
                Reward selected;
                if (i == 0 && guaranteeDiablo && remaining.Contains(Reward.Diablo)) selected = Reward.Diablo;
                else if (total <= 0) selected = remaining[Random.Range(0, remaining.Count)];
                else
                {
                    float roll = Random.value * total;
                    int selectedTier = 0;
                    for (int tier = 0; tier < 3; tier++)
                    {
                        if (!remaining.Exists(r => Rarity(r) == tier) || weights[tier] <= 0) continue;
                        selectedTier = tier;
                        roll -= weights[tier];
                        if (roll < 0) break;
                    }
                    var candidates = remaining.FindAll(r => Rarity(r) == selectedTier);
                    selected = candidates[Random.Range(0, candidates.Count)];
                }
                remaining.Remove(selected);
                offered.Add(selected);
                int rarity = Rarity(selected);
                cardTitles[i].text = RewardTitle(selected);
                cardDescriptions[i].text = RewardDescription(selected);
                cardRarities[i].text = names[rarity];
                cardRarities[i].color = colors[rarity];
                cardBackgrounds[i].color = Color.Lerp(new Color(0.06f, 0.1f, 0.15f), colors[rarity], 0.18f);
            }
        }

        private static string RewardTitle(Reward reward)
        {
            switch (reward)
            {
                case Reward.Explosion: return "전장 폭발";
                case Reward.Protection: return "절대 보호";
                case Reward.Bottles: return "경험치 보급";
                case Reward.Healing: return "긴급 치료";
                case Reward.Speed: return "신속";
                case Reward.Mines: return "지뢰밭";
                case Reward.Diablo: return "디아블로";
                default: return "전투 함성";
            }
        }

        private string RewardDescription(Reward reward)
        {
            switch (reward)
            {
                case Reward.Explosion: return $"화면 안 모든 적에게\n{explosionDamage} 피해\n\n보스에게도 적용";
                case Reward.Protection: return $"지휘관과 모든 생존 병력\n{invulnerabilitySeconds}초 동안 무적";
                case Reward.Bottles: return $"경험치 병 {bottleReward}개 획득\n\nCtrl + 1~5로 분배";
                case Reward.Healing: return "지휘관과 모든 생존 병력\n체력 즉시 전부 회복\n\n사망한 병력은 부활하지 않음";
                case Reward.Speed: return $"지휘관과 모든 병력\n{speedDuration}초간 이동속도 +{speedBonus * 100:0}%";
                case Reward.Mines: return "지휘관 주변에\n마법 함정 10개 설치\n\n적이 밟으면 메테오 낙하";
                case Reward.Diablo: return $"{GetComponent<DiabloController>().Duration:0.#}초간 병력을 흡수하고 강화\n이동 시 원형 화살\n좌클릭 화염 · 자동 메테오";
                default: return $"전 부대 {warCryDuration}초간\n공격력 +{warCryBonus * 100:0}%\n공격속도 +{warCryBonus * 100:0}%";
            }
        }
        private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.text = value; label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center; label.color = new Color(1, 0.91f, 0.7f); label.raycastTarget = false;
            return label;
        }
        private void OnDisable()
        {
            combat.MiniBossKilled -= QueueReward; run.RunEnded -= ResetRun; ResetRun();
        }
        private void OnDestroy() { if (uiRoot != null) Destroy(uiRoot); }
    }
}
