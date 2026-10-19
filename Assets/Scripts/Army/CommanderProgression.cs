using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    [DefaultExecutionOrder(150)]
    public sealed class CommanderProgression : MonoBehaviour
    {
        public enum Support { Collection, Education, Retreat, Reserve, Hunter, Contract }
        [Header("지휘관 경험치")]
        [Min(1)] public int experiencePerPickup = 10;
        [Min(1)] public int firstLevelExperience = 50;
        [Min(1)] public int experienceIncrease = 30;
        [Header("지원 카드: 단계당 증가량 / 최대 3단계")]
        [Min(0)] public float pickupBonus = .4f;
        [Min(0)] public float educationBonus = .2f;
        [Min(0)] public float hunterBonus = .2f;
        [Min(0)] public int contractGold = 50;
        [Header("긴급 후퇴")]
        [Range(0, 1)] public float retreatThreshold = .3f;
        [Min(0)] public float retreatSpeedBonus = .5f;
        [Min(.1f)] public float retreatDuration = 5f;
        [Min(.1f)] public float retreatCooldown = 30f;
        [Header("예비 병력: 한 전투에 1회")]
        [Min(.1f)] public float reviveDelay = 5f;
        [Range(.01f, 1)] public float reviveHealth = .5f;
        [SerializeField] private TMP_FontAsset font;
        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int Required => firstLevelExperience + (Level - 1) * experienceIncrease;
        public float PickupMultiplier => 1 + Rank(Support.Collection) * pickupBonus;
        public float InvestmentMultiplier => 1 + Rank(Support.Education) * educationBonus;
        public float EliteDamageMultiplier => 1 + Rank(Support.Hunter) * hunterBonus;
        public int VictoryGold => Rank(Support.Contract) * contractGold;
        public float MoveMultiplier => retreatRemaining > 0 ? 1 + Rank(Support.Retreat) * retreatSpeedBonus : 1;
        public bool IsChoosing { get; private set; }
        public event Action Changed;
        private readonly int[] ranks = new int[6];
        private readonly List<Support> offered = new List<Support>();
        private readonly List<UnitHealth> watched = new List<UnitHealth>();
        private RunController run;
        private BattleExperience experience;
        private RecruitmentController recruitment;
        private UnitHealth commanderHealth, reviveTarget;
        private int pending;
        private bool reserveUsed;
        private float retreatRemaining, retreatCooldownLeft, reviveRemaining, previousScale;
        private GameObject canvasRoot, panel, hud;
        private TMP_Text hudText, title, status;
        private Image expFill;
        private readonly Button[] buttons = new Button[3];
        private readonly TMP_Text[] labels = new TMP_Text[3];
        public int Rank(Support support) => ranks[(int)support];

        private void Awake()
        {
            run = GetComponent<RunController>();
            experience = GetComponent<BattleExperience>();
            recruitment = GetComponent<RecruitmentController>();
        }
        private void OnEnable()
        {
            run.RunStarted += Begin;
            run.RunEnded += End;
            experience.OnBottleCollected += GainPickup;
        }
        private void Begin()
        {
            End(); EnsureUI(); hud.SetActive(true);
            commanderHealth = run.Commander.GetComponent<UnitHealth>();
            if (commanderHealth != null) commanderHealth.OnDamageTaken += CommanderDamaged;
            foreach (var unit in recruitment.Units)
            {
                if (unit.Key == null) continue;
                var health = unit.Key.GetComponent<UnitHealth>();
                if (health == null) continue;
                watched.Add(health); health.OnDamageTaken += SoldierDamaged;
            }
            Refresh();
        }
        public void GainPickup()
        {
            if (!run.IsRunning || run.CommanderDead) return;
            Experience += experiencePerPickup;
            while (Experience >= Required)
            {
                Experience -= Required; Level++; pending++;
            }
            Refresh();
        }
        private void Update()
        {
            if (!run.IsRunning || run.CommanderDead || run.IsPaused || run.IsChoosingUpgrade) return;
            float dt = Time.deltaTime;
            retreatRemaining = Mathf.Max(0, retreatRemaining - dt);
            retreatCooldownLeft = Mathf.Max(0, retreatCooldownLeft - dt);
            if (reviveTarget != null)
            {
                reviveRemaining -= dt;
                if (reviveRemaining <= 0)
                {
                    if (reviveTarget.IsDead)
                    {
                        reviveTarget.transform.position = run.FormationPosition(reviveTarget.transform);
                        reviveTarget.Revive(reviveHealth);
                    }
                    reviveTarget = null;
                }
            }
            if (pending > 0) OpenChoice();
        }
        private void CommanderDamaged(UnitHealth health, float damage, float absorbed)
        {
            if (Rank(Support.Retreat) == 0 || health.IsDead || retreatCooldownLeft > 0 ||
                health.Current / health.Maximum > retreatThreshold) return;
            retreatRemaining = retreatDuration; retreatCooldownLeft = retreatCooldown;
            if (status != null) status.text = "긴급 후퇴 발동!";
        }
        private void SoldierDamaged(UnitHealth health, float damage, float absorbed)
        {
            if (Rank(Support.Reserve) == 0 || reserveUsed || !health.IsDead) return;
            reserveUsed = true; reviveTarget = health; reviveRemaining = reviveDelay;
            if (status != null) status.text = $"예비 병력: {reviveDelay:0.#}초 후 부활";
        }
        private void OpenChoice()
        {
            offered.Clear();
            foreach (Support support in Enum.GetValues(typeof(Support)))
            {
                if (Rank(support) >= (support == Support.Reserve ? 1 : 3)) continue;
                if (support == Support.Contract && GameModeManager.Current != GameMode.Adventure) continue;
                offered.Add(support);
            }
            if (offered.Count == 0) { pending = 0; return; }
            for (int i = offered.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                Support temp = offered[i]; offered[i] = offered[j]; offered[j] = temp;
            }
            if (offered.Count > 3) offered.RemoveRange(3, offered.Count - 3);
            EnsureUI();
            IsChoosing = true; run.IsChoosingUpgrade = true;
            previousScale = Time.timeScale; Time.timeScale = 0;
            title.text = $"지휘관 레벨 업 · Lv.{Level - pending + 1}\n지원 카드 선택";
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].gameObject.SetActive(i < offered.Count);
                if (i >= offered.Count) continue;
                buttons[i].interactable = true;
                ((RectTransform)buttons[i].transform).anchoredPosition = new Vector2((i - (offered.Count - 1) * .5f) * 420, 0);
                labels[i].text = $"<size=34>{Name(offered[i])}</size>\n<size=22>단계 {Rank(offered[i]) + 1}</size>\n\n{Description(offered[i])}\n\n<size=23>선택</size>";
            }
            panel.SetActive(true);
        }
        public bool Choose(int index)
        {
            if (!IsChoosing || !run.IsRunning || index < 0 || index >= offered.Count) return false;
            ranks[(int)offered[index]]++; pending--;
            status.text = Name(offered[index]) + " 획득";
            Close(); Refresh(); return true;
        }
        private void Close()
        {
            if (panel != null) panel.SetActive(false);
            if (!IsChoosing) return;
            IsChoosing = false; run.IsChoosingUpgrade = false; Time.timeScale = previousScale;
        }
        private void End()
        {
            Close();
            if (commanderHealth != null) commanderHealth.OnDamageTaken -= CommanderDamaged;
            foreach (var health in watched) if (health != null) health.OnDamageTaken -= SoldierDamaged;
            watched.Clear(); commanderHealth = reviveTarget = null;
            Level = 1; Experience = pending = 0; Array.Clear(ranks, 0, ranks.Length);
            reserveUsed = false; retreatRemaining = retreatCooldownLeft = 0;
            offered.Clear();
            if (hud != null) hud.SetActive(false);
            if (status != null) status.text = "";
            Changed?.Invoke();
        }
        private void Refresh()
        {
            if (hudText != null) hudText.text = $"지휘관 Lv.{Level}    {Experience} / {Required} EXP";
            if (expFill != null) expFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)Experience / Required), 1);
            Changed?.Invoke();
        }
        private static string Name(Support s) => new[] { "보급 회수", "야전 교육", "긴급 후퇴", "예비 병력", "정예 사냥", "전리품 계약" }[(int)s];
        private string Description(Support s)
        {
            int n = Rank(s) + 1;
            switch (s)
            {
                case Support.Collection: return $"경험치 병 획득 반경\n총 +{n * pickupBonus:P0}\n\n이번 전투 동안 유지";
                case Support.Education: return $"병사에게 투자하는 병의 경험치\n총 +{n * educationBonus:P0}\n\n지휘관 경험치는 증가하지 않음";
                case Support.Retreat: return $"지휘관 체력 {retreatThreshold:P0} 이하에서 피격 시\n전 부대 이동속도 +{n * retreatSpeedBonus:P0}\n{retreatDuration:0.#}초 지속 · 재사용 {retreatCooldown:0.#}초";
                case Support.Reserve: return $"획득 후 처음 사망한 병사 1명\n{reviveDelay:0.#}초 후 체력 {reviveHealth:P0}로 부활\n\n이번 전투에서 1회";
                case Support.Hunter: return $"정예·보스에게 주는 피해\n총 +{n * hunterBonus:P0}\n\n이번 전투 동안 유지";
                default: return $"모험 전투 승리 보상\n추가 골드 총 {n * contractGold}\n\n패배·중도 복귀 시 지급하지 않음";
            }
        }
        private void EnsureUI()
        {
            if (canvasRoot != null) return;
            if (font == null) font = TMP_Settings.defaultFontAsset;
            canvasRoot = new GameObject("Commander Support Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 290;
            var scaler = canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var bar = Rect("Commander EXP", canvasRoot.transform, new Vector2(0, -100), new Vector2(520, 75));
            bar.anchorMin = bar.anchorMax = new Vector2(.5f, 1); hud = bar.gameObject;
            hudText = Text("Level", bar, new Vector2(0, 15), new Vector2(520, 30), 22);
            var track = Rect("EXP Track", bar, new Vector2(0, -10), new Vector2(480, 7));
            track.gameObject.AddComponent<Image>().color = new Color(.04f, .08f, .12f, .9f);
            var fill = Rect("EXP Fill", track, Vector2.zero, Vector2.zero); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            expFill = fill.gameObject.AddComponent<Image>(); expFill.color = new Color(.95f, .73f, .3f); expFill.raycastTarget = false;
            status = Text("Status", bar, new Vector2(0, -32), new Vector2(800, 28), 18);
            var overlay = Rect("Support Choice", canvasRoot.transform, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            panel = overlay.gameObject; panel.AddComponent<Image>().color = new Color(.015f, .04f, .06f, .96f);
            title = Text("Title", overlay, new Vector2(0, 295), new Vector2(1200, 110), 38);
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var card = Rect("Support " + i, overlay, Vector2.zero, new Vector2(390, 410));
                card.gameObject.AddComponent<Image>().color = new Color(.10f, .16f, .22f);
                buttons[i] = card.gameObject.AddComponent<Button>(); buttons[i].onClick.AddListener(() => Choose(index));
                labels[i] = Text("Description", card, Vector2.zero, new Vector2(355, 380), 25);
            }
            var help = Text("Help", overlay, new Vector2(0, -285), new Vector2(1200, 60), 24);
            help.text = "1장을 선택하면 전투가 계속됩니다. · 지원 효과는 이번 전투에만 적용";
            panel.SetActive(false);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(.5f, .5f); r.anchoredPosition = pos; r.sizeDelta = size; return r;
        }
        private TMP_Text Text(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize)
        {
            var t = Rect(name, parent, pos, size).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.fontSize = fontSize; t.color = new Color(1, .92f, .75f); t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; return t;
        }
        private void OnDisable()
        {
            run.RunStarted -= Begin; run.RunEnded -= End; experience.OnBottleCollected -= GainPickup; End();
        }
        private void OnDestroy() { if (canvasRoot != null) Destroy(canvasRoot); }
    }
}
