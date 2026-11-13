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
        [Header("지휘관 카드 UI")]
        [SerializeField] private Sprite[] cardSprites = new Sprite[6];
        [SerializeField] private CardMotion.Config cardMotionConfig = new CardMotion.Config();
        [Header("지휘관 카드 UI - 씬 배치 참조")]
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject hud;
        [SerializeField] private TMP_Text hudText;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Image expFill;
        [SerializeField] private Button[] buttons = new Button[3];
        [SerializeField] private CardMotion[] cardMotions = new CardMotion[3];
        [SerializeField] private TMP_Text[] cardTitles = new TMP_Text[3];
        [SerializeField] private TMP_Text[] labels = new TMP_Text[3];
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
        private bool selectionInProgress, uiBound;
        public int Rank(Support support) => ranks[(int)support];

        private void Awake()
        {
            run = GetComponent<RunController>();
            experience = GetComponent<BattleExperience>();
            recruitment = GetComponent<RecruitmentController>();
            EnsureUI();
        }
        private void OnEnable()
        {
            run.RunStarted += Begin;
            run.RunEnded += End;
            experience.OnBottlesCollected += GainPickups;
        }
        private void Begin()
        {
            End();
            if (!EnsureUI()) return;
            hud.SetActive(true);
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
        public void GainPickup() => GainPickups(1);
        private void GainPickups(int count)
        {
            if (!run.IsRunning || run.CommanderDead) return;
            Experience += experiencePerPickup * count;
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
            if (!EnsureUI()) return;
            IsChoosing = true; run.IsChoosingUpgrade = true;
            previousScale = Time.timeScale; Time.timeScale = 0;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].gameObject.SetActive(i < offered.Count);
                if (i >= offered.Count) continue;
                Image cardImage = buttons[i].GetComponent<Image>();
                int spriteIndex = (int)offered[i];
                cardImage.sprite = cardSprites != null && spriteIndex < cardSprites.Length ? cardSprites[spriteIndex] : null;
                cardImage.color = cardImage.sprite != null ? Color.white : new Color(.10f, .16f, .22f);
                cardTitles[i].text = Name(offered[i]);
                labels[i].text = $"<size=22>단계 {Rank(offered[i]) + 1}</size>\n\n{Description(offered[i])}";
            }
            panel.SetActive(true);
            selectionInProgress = false;
            for (int i = 0; i < offered.Count; i++) cardMotions[i].PlayEntrance(i);
        }
        public bool Choose(int index)
        {
            if (!IsChoosing || !run.IsRunning || index < 0 || index >= offered.Count) return false;
            ranks[(int)offered[index]]++; pending--;
            status.text = Name(offered[index]) + " 획득";
            Close(); Refresh(); return true;
        }
        private void SelectCard(int index)
        {
            if (!IsChoosing || selectionInProgress || index < 0 || index >= offered.Count) return;
            selectionInProgress = true;
            foreach (CardMotion motion in cardMotions) if (motion != null) motion.LockInteraction();
            cardMotions[index].PlaySelection(() =>
            {
                if (!isActiveAndEnabled || !IsChoosing) return;
                int remaining = offered.Count - 1;
                if (remaining <= 0) { FinishCardSelection(index); return; }
                for (int i = 0; i < offered.Count; i++)
                {
                    if (i == index) continue;
                    cardMotions[i].FadeOut(() =>
                    {
                        remaining--;
                        if (remaining == 0) FinishCardSelection(index);
                    });
                }
            });
        }
        private void FinishCardSelection(int index)
        {
            if (!Choose(index)) selectionInProgress = false;
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
            selectionInProgress = false;
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
        private bool EnsureUI()
        {
            bool missing = canvasRoot == null || panel == null || hud == null || hudText == null ||
                title == null || status == null || expFill == null || buttons == null ||
                cardMotions == null || cardTitles == null || labels == null ||
                buttons.Length < 3 || cardMotions.Length < 3 || cardTitles.Length < 3 || labels.Length < 3;
            if (missing)
            {
                Debug.LogError("CommanderProgression: PlayScene의 Commander Support Canvas 참조가 연결되지 않았습니다.", this);
                return false;
            }
            if (uiBound) return true;
            for (int i = 0; i < 3; i++)
            {
                if (buttons[i] == null || cardMotions[i] == null || cardTitles[i] == null || labels[i] == null)
                {
                    Debug.LogError($"CommanderProgression: 지휘관 카드 {i + 1}의 씬 참조가 비어 있습니다.", this);
                    return false;
                }
                int index = i;
                cardMotions[i].Configure(buttons[i], () => SelectCard(index), cardMotionConfig);
            }
            uiBound = true;
            return true;
        }
        private void OnDisable()
        {
            run.RunStarted -= Begin; run.RunEnded -= End; experience.OnBottlesCollected -= GainPickups; End();
        }
    }
}
