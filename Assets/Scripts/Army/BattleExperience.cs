using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class BattleExperience : MonoBehaviour
    {
        [Serializable]
        private class ClassRow
        {
            [NonSerialized] public Transform unit;
            public GameObject root;
            public Button invest;
            public TMP_Text progress;
            public TMP_Text title;
            public Image fill;
            public TMP_Text healthText;
            public Image healthFill;
        }

        public class UnitProgress
        {
            public int Level { get; internal set; } = 1;
            public int Experience { get; internal set; }
            public int Required => 100 * (Level + 2);
            public bool Piercing { get; internal set; }
            public int MultiShotLevel { get; internal set; }
            public int FocusLevel { get; internal set; }
            public bool Suppression { get; internal set; }
            public bool Explosion { get; internal set; }
            public bool WeaponStrike { get; internal set; }
            public int BloodthirstLevel { get; internal set; }
            public int SpinningSwordLevel { get; internal set; }
            public int SpinSlashLevel { get; internal set; }
            public int BannerLevel { get; internal set; }
            public bool MagicTrap { get; internal set; }
            public bool LaserAOE { get; internal set; }
            public bool ChainLightning { get; internal set; }
            public bool TeamShield { get; internal set; }
            public bool TeamAttack { get; internal set; }
            public bool IntimidatingShout { get; internal set; }
            public bool IronWall { get; internal set; }
            public bool TeamAttackSpeed { get; internal set; }
            public bool ChainCharge { get; internal set; }
            public int FlameHoovesLevel { get; internal set; }
            public bool ChargeInvulnerability { get; internal set; }
            public bool MarchingHorn { get; internal set; }
            public bool Momentum { get; internal set; }
            public int ManaCycleLevel { get; internal set; }
        }

        [SerializeField] private RunController run;
        [SerializeField] private CombatController combat;
        [SerializeField] private RecruitmentController recruitment;
        [SerializeField] private GameObject bottlePrefab;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text bottleCount;
        [SerializeField] private ClassRow[] rows;
        [SerializeField, Min(1)] private int experiencePerBottle = 100;
        [SerializeField, Min(0.1f)] private float pickupRadius = 1.2f;
        [Header("경험치 병 표시")]
        [SerializeField] private float bottleWorldY = 0.65f;
        [SerializeField] private float bottleRotationSpeed = 90f;
        [SerializeField, Min(0.1f)] private float bottleAttractionSpeed = 4f;
        [Header("레벨업 카드")]
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private TMP_Text levelUpTarget;
        [SerializeField, Range(1, 3)] private int maximumCardChoices = 3;
        [SerializeField] private CardMotion.Config cardMotionConfig = new CardMotion.Config();
        [SerializeField] private Button piercingButton;
        [SerializeField] private GameObject piercingCard;
        [SerializeField] private Button multiShotButton;
        [SerializeField] private GameObject multiShotCard;
        [SerializeField] private TMP_Text multiShotDescription;
        [SerializeField, Range(1, 180)] private float multiShotSpreadAngle = 30f;
        [SerializeField, Min(1)] private int multiShotMaximumLevel = 3;
        [Header("궁수 집중 패시브")]
        [SerializeField] private GameObject focusCardTemplate;
        [SerializeField] private GameObject focusBuffPrefab;
        [SerializeField] private Vector3 focusBuffLocalPosition = new Vector3(0, 0.05f, 0);
        [SerializeField] private Vector3 focusBuffScale = Vector3.one;
        [SerializeField, Min(0)] private float focusStationaryDelay = 0.5f;
        [SerializeField, Range(0, 1)] private float focusCriticalChance = 0.3f;
        [SerializeField, Min(1)] private float focusBaseCriticalMultiplier = 1.5f;
        [SerializeField, Min(0)] private float focusCriticalMultiplierPerLevel = 0.5f;
        [SerializeField, Min(1)] private int focusMaximumLevel = 3;
        [Header("병사 피의 갈증")]
        [SerializeField] private GameObject bloodthirstCard;
        [SerializeField] private Button bloodthirstButton;
        [SerializeField, Range(0, 1)] private float bloodthirstInitialHeal = 0.1f;
        [SerializeField, Range(0, 1)] private float bloodthirstHealPerLevel = 0.05f;
        [SerializeField, Min(1)] private int bloodthirstMaximumLevel = 3;
        [Header("병사 회전검")]
        [SerializeField] private GameObject spinningSwordCard;
        [SerializeField] private Button spinningSwordButton;
        [SerializeField, Min(1)] private int spinningSwordMaximumLevel = 4;
        [Header("병사 회전 베기")]
        [SerializeField] private GameObject spinSlashCard;
        [SerializeField] private Button spinSlashButton;
        [SerializeField, Min(1)] private int spinSlashMaximumLevel = 4;
        [Header("병사 전투 깃발")]
        [SerializeField] private GameObject bannerCard;
        [SerializeField] private Button bannerButton;
        [SerializeField, Min(1)] private int bannerMaximumLevel = 4;
        [Header("마법사 마법 함정")]
        [SerializeField] private GameObject magicTrapCard;
        [SerializeField] private Button magicTrapButton;
        [Header("마법사 광역 레이저")]
        [SerializeField] private GameObject laserCard;
        [SerializeField] private Button laserButton;
        [SerializeField] private GameObject chainLightningCard;
        [SerializeField] private Button chainLightningButton;
        [SerializeField] private GameObject teamShieldCard;
        [SerializeField] private Button teamShieldButton;
        [SerializeField] private GameObject teamAttackCard;
        [SerializeField] private Button teamAttackButton;
        [SerializeField] private GameObject shoutCard;
        [SerializeField] private Button shoutButton;
        [Header("방패병 철벽")]
        [SerializeField] private GameObject ironWallCard;
        [SerializeField] private Button ironWallButton;
        [SerializeField] private GameObject teamAttackSpeedCard;
        [SerializeField] private Button teamAttackSpeedButton;
        [SerializeField] private GameObject chainChargeCard;
        [SerializeField] private Button chainChargeButton;
        [SerializeField] private GameObject flameHoovesCard;
        [SerializeField] private Button flameHoovesButton;
        [SerializeField] private GameObject chargeInvulnerabilityCard;
        [SerializeField] private Button chargeInvulnerabilityButton;
        [SerializeField] private GameObject marchingHornCard;
        [SerializeField] private Button marchingHornButton;
        [SerializeField] private GameObject momentumCard;
        [SerializeField] private Button momentumButton;
        [SerializeField, Range(0, 1)] private float ironWallDamageReduction = 0.1f;
        [Header("마법사 마나 순환")]
        [SerializeField] private GameObject manaCycleCard;
        [SerializeField] private Button manaCycleButton;
        [SerializeField, Range(0, 0.25f)] private float manaCycleReductionPerLevel = 0.1f;
        [SerializeField, Min(1)] private int manaCycleMaximumLevel = 3;

        private struct LevelUpRequest
        {
            public Transform unit;
            public int level;
        }
        private readonly Queue<LevelUpRequest> levelUps = new Queue<LevelUpRequest>();
        private readonly HashSet<int> shownChoices = new HashSet<int>();
        private readonly Dictionary<int, CardMotion> cardViews = new Dictionary<int, CardMotion>();
        private float previousTimeScale = 1;
        private bool selectionInProgress;
        public bool IsChoosingUpgrade { get; private set; }

        private readonly Dictionary<Transform, UnitProgress> units = new Dictionary<Transform, UnitProgress>();
        private readonly Dictionary<Transform, float> focusStationaryTimes = new Dictionary<Transform, float>();
        private readonly Dictionary<Transform, Transform> focusEffects = new Dictionary<Transform, Transform>();
        private readonly List<Transform> drops = new List<Transform>();
        private GameObject focusCard;
        private Button focusButton;
        private TMP_Text focusDescription;
        private GameObject suppressionCard;
        private Button suppressionButton;
        private TMP_Text suppressionDescription;
        private GameObject explosionCard;
        private Button explosionButton;
        private TMP_Text explosionDescription;
        [SerializeField] private GameObject weaponStrikeCard;
        [SerializeField] private Button weaponStrikeButton;
        private Transform dropRoot;
        public int Bottles { get; private set; }
        public int DropCount => drops.Count;
        public event Action<Transform, int> UnitLeveledUp;

        private void Awake()
        {
            if (cardMotionConfig == null) cardMotionConfig = new CardMotion.Config();
            CreateFocusCard();
            CreateSuppressionCard();
            explosionCard = CreateSkillCard("Explosion Card", "폭발 사격", out explosionButton, out explosionDescription);
            foreach (ClassRow row in rows)
                row.invest.onClick.AddListener(() => Invest(row.unit));
            panel.SetActive(false);
            levelUpPanel.SetActive(false);
            BindCard(3, piercingCard, piercingButton);
            BindCard(4, multiShotCard, multiShotButton);
            BindCard(5, focusCard, focusButton);
            BindCard(6, suppressionCard, suppressionButton);
            BindCard(7, explosionCard, explosionButton);
            BindCard(8, weaponStrikeCard, weaponStrikeButton);
            BindCard(9, bloodthirstCard, bloodthirstButton);
            BindCard(10, spinningSwordCard, spinningSwordButton);
            BindCard(11, spinSlashCard, spinSlashButton);
            BindCard(12, bannerCard, bannerButton);
            BindCard(13, magicTrapCard, magicTrapButton);
            BindCard(14, manaCycleCard, manaCycleButton);
            BindCard(15, laserCard, laserButton);
            BindCard(16, chainLightningCard, chainLightningButton);
            BindCard(17, teamShieldCard, teamShieldButton);
            BindCard(18, teamAttackCard, teamAttackButton);
            BindCard(19, shoutCard, shoutButton);
            BindCard(20, ironWallCard, ironWallButton);
            BindCard(21, teamAttackSpeedCard, teamAttackSpeedButton);
            BindCard(22, chainChargeCard, chainChargeButton);
            BindCard(23, flameHoovesCard, flameHoovesButton);
            BindCard(24, chargeInvulnerabilityCard, chargeInvulnerabilityButton);
            BindCard(25, marchingHornCard, marchingHornButton);
            BindCard(26, momentumCard, momentumButton);
        }

        private void BindCard(int choice, GameObject card, Button legacyButton)
        {
            if (card == null) return;
            CardMotion view = card.GetComponent<CardMotion>();
            if (view == null) view = card.AddComponent<CardMotion>();
            view.Configure(legacyButton, () => SelectCard(choice), cardMotionConfig);
            cardViews[choice] = view;
        }

        private void SelectCard(int choice)
        {
            if (!run.IsRunning || !IsChoosingUpgrade || selectionInProgress || !shownChoices.Contains(choice)) return;
            selectionInProgress = true;
            foreach (CardMotion view in cardViews.Values) view.LockInteraction();
            cardViews[choice].PlaySelection(() =>
            {
                if (!isActiveAndEnabled || !IsChoosingUpgrade) return;
                int remaining = 0;
                foreach (int other in shownChoices)
                    if (other != choice && cardViews.ContainsKey(other)) remaining++;
                if (remaining == 0)
                {
                    FinishCardSelection(choice);
                    return;
                }
                foreach (int other in shownChoices)
                {
                    if (other == choice || !cardViews.TryGetValue(other, out CardMotion view)) continue;
                    view.FadeOut(() =>
                    {
                        remaining--;
                        if (remaining == 0) FinishCardSelection(choice);
                    });
                }
            });
        }

        private void FinishCardSelection(int choice)
        {
            if (isActiveAndEnabled && !ChooseUpgrade(choice)) selectionInProgress = false;
        }

        private void OnEnable()
        {
            run.RunStarted += Begin;
            run.RunEnded += Clear;
            combat.EnemyKilled += Drop;
        }

        private void OnDisable()
        {
            run.RunStarted -= Begin;
            run.RunEnded -= Clear;
            combat.EnemyKilled -= Drop;
            Clear();
        }

        private void Begin()
        {
            Clear();
            int index = 0;
            foreach (Transform unit in recruitment.SoldiersRoot)
            {
                if (!recruitment.Units.TryGetValue(unit, out UnitDefinition definition)) continue;
                units.Add(unit, new UnitProgress());
                focusStationaryTimes[unit] = 0;
                if (index < rows.Length)
                {
                    rows[index].unit = unit;
                    rows[index].title.text = $"{definition.displayName} {index + 1}";
                    index++;
                }
            }
            dropRoot = new GameObject("Experience Drops").transform;
            dropRoot.SetParent(transform, false);
            panel.SetActive(true);
            RefreshUI();
        }

        public UnitProgress GetProgress(Transform unit)
        {
            if (unit == null) return null;
            units.TryGetValue(unit, out UnitProgress progress);
            return progress;
        }

        public void Drop(Vector3 position)
        {
            if (!run.IsRunning || dropRoot == null) return;
            position.y = bottleWorldY;
            drops.Add(Instantiate(bottlePrefab, position, Quaternion.identity, dropRoot).transform);
        }

        private void Update()
        {
            MoveBottles(Time.deltaTime);
            CollectNearby();
            UpdateFocusState(Time.deltaTime);
            if (run.IsRunning) RefreshUI();
        }

        private void CreateFocusCard()
        {
            focusCard = CreateSkillCard("Focus Card", "집중", out focusButton, out focusDescription);
        }

        private void CreateSuppressionCard()
        {
            suppressionCard = CreateSkillCard("Suppression Card", "제압 사격", out suppressionButton, out suppressionDescription);
        }

        private GameObject CreateSkillCard(string objectName, string title, out Button button, out TMP_Text description)
        {
            button = null;
            description = null;
            if (focusCardTemplate == null) return null;
            GameObject card = Instantiate(focusCardTemplate, focusCardTemplate.transform.parent);
            card.name = objectName;
            button = card.GetComponentInChildren<Button>(true);
            foreach (TMP_Text text in card.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "Title") text.text = title;
                else if (text.name == "Description") description = text;
            }
            card.SetActive(false);
            return card;
        }

        private void UpdateFocusState(float deltaTime)
        {
            if (!run.IsRunning || run.IsPaused || IsChoosingUpgrade || deltaTime <= 0) return;
            foreach (var entry in units)
            {
                Transform unit = entry.Key;
                if (unit == null || !unit.gameObject.activeSelf || !HasFocus(unit) || run.IsMoving(unit))
                {
                    // 이동하거나 집중을 잃었을 때만 대기 시간을 초기화한다.
                    // 활성화 대기 중인 매 프레임마다 지우면 0.5초에 도달할 수 없다.
                    if (unit != null) focusStationaryTimes[unit] = 0;
                    SetFocusActive(unit, false);
                    continue;
                }
                focusStationaryTimes[unit] = focusStationaryTimes.TryGetValue(unit, out float time) ? time + deltaTime : deltaTime;
                SetFocusActive(unit, focusStationaryTimes[unit] >= focusStationaryDelay);
            }
        }

        private void SetFocusActive(Transform unit, bool active)
        {
            if (unit == null) return;
            if (!focusEffects.TryGetValue(unit, out Transform effect) && active && focusBuffPrefab != null)
            {
                effect = Instantiate(focusBuffPrefab, unit).transform;
                effect.localPosition = focusBuffLocalPosition;
                effect.localScale = focusBuffScale;
                focusEffects[unit] = effect;
            }
            if (effect == null) return;
            bool wasActive = effect.gameObject.activeSelf;
            if (wasActive != active) effect.gameObject.SetActive(active);
            if (active && !wasActive)
                foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Clear(true);
                    particle.Play(true);
                }
        }

        private void MoveBottles(float deltaTime)
        {
            if (!run.IsRunning || run.IsPaused || IsChoosingUpgrade || deltaTime <= 0) return;
            Vector3 destination = run.Commander.position;
            destination.y = bottleWorldY;
            foreach (Transform bottle in drops)
            {
                bottle.position = Vector3.MoveTowards(bottle.position, destination,
                    bottleAttractionSpeed * deltaTime);
                bottle.Rotate(Vector3.up, bottleRotationSpeed * deltaTime, Space.World);
            }
        }

        public void CollectNearby()
        {
            if (!run.IsRunning || run.IsPaused || IsChoosingUpgrade) return;
            bool collected = false;
            for (int i = drops.Count - 1; i >= 0; i--)
            {
                Vector3 offset = drops[i].position - run.Commander.position;
                offset.y = 0;
                if (offset.sqrMagnitude > pickupRadius * pickupRadius) continue;
                drops[i].gameObject.SetActive(false);
                Destroy(drops[i].gameObject);
                drops.RemoveAt(i);
                Bottles++;
                collected = true;
            }
            if (collected) RefreshUI();
        }

        public bool Invest(Transform unit)
        {
            if (!run.IsRunning || run.IsPaused || IsChoosingUpgrade || Bottles <= 0 || unit == null || !unit.gameObject.activeSelf ||
                !units.TryGetValue(unit, out UnitProgress progress))
                return false;
            Bottles--;
            progress.Experience += experiencePerBottle;
            while (progress.Experience >= progress.Required)
            {
                progress.Experience -= progress.Required;
                progress.Level++;
                levelUps.Enqueue(new LevelUpRequest { unit = unit, level = progress.Level });
                UnitLeveledUp?.Invoke(unit, progress.Level);
            }
            ShowNextUpgrade();
            RefreshUI();
            return true;
        }

        private void RefreshUI()
        {
            bottleCount.text = Bottles.ToString();
            foreach (ClassRow row in rows)
            {
                UnitProgress progress = GetProgress(row.unit);
                row.root.SetActive(progress != null);
                row.invest.interactable = run.IsRunning && !IsChoosingUpgrade && Bottles > 0 && progress != null && row.unit.gameObject.activeSelf;
                if (progress != null)
                {
                    row.progress.text = $"Lv.{progress.Level}   {progress.Experience} / {progress.Required} EXP";
                    SetBar(row.fill, (float)progress.Experience / progress.Required);
                    var health = row.unit.GetComponent<UnitHealth>();
                    if (health != null)
                    {
                        row.healthText.text = $"HP {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Maximum)}";
                        SetBar(row.healthFill, health.Current / health.Maximum);
                    }
                }
            }
        }

        public bool HasPiercing(Transform unit) => GetProgress(unit)?.Piercing == true;
        public bool HasFocus(Transform unit) => GetProgress(unit)?.FocusLevel > 0;
        public bool HasSuppression(Transform unit) => GetProgress(unit)?.Suppression == true;
        public bool HasExplosion(Transform unit) => GetProgress(unit)?.Explosion == true;
        public bool HasWeaponStrike(Transform unit) => GetProgress(unit)?.WeaponStrike == true;
        public float BloodthirstHealRatio(Transform unit)
        {
            int level = GetProgress(unit)?.BloodthirstLevel ?? 0;
            return level == 0 ? 0 : bloodthirstInitialHeal + (level - 1) * bloodthirstHealPerLevel;
        }
        public int SpinningSwordCount(Transform unit)
        {
            int level = GetProgress(unit)?.SpinningSwordLevel ?? 0;
            return level == 0 ? 0 : Mathf.Min(level + 1, 5);
        }
        public int SpinSlashLevel(Transform unit) => GetProgress(unit)?.SpinSlashLevel ?? 0;
        public int BannerLevel(Transform unit) => GetProgress(unit)?.BannerLevel ?? 0;
        public bool HasMagicTrap(Transform unit) => GetProgress(unit)?.MagicTrap == true;
        public float SkillCooldownMultiplier(Transform unit)
        {
            int level = GetProgress(unit)?.ManaCycleLevel ?? 0;
            return 1f - Mathf.Min(0.8f, level * manaCycleReductionPerLevel);
        }
        public bool IsFocusActive(Transform unit) => HasFocus(unit) &&
            focusStationaryTimes.TryGetValue(unit, out float time) && time >= focusStationaryDelay;
        public float FocusCriticalChance(Transform unit) => IsFocusActive(unit) ? focusCriticalChance : 0;
        public float FocusCriticalMultiplier(Transform unit)
        {
            int level = GetProgress(unit)?.FocusLevel ?? 0;
            return focusBaseCriticalMultiplier + Mathf.Max(0, level - 1) * focusCriticalMultiplierPerLevel;
        }
        public int MultiShotCount(Transform unit)
        {
            int level = GetProgress(unit)?.MultiShotLevel ?? 0;
            return level == 0 ? 1 : level + 2;
        }
        public float MultiShotSpreadAngle => multiShotSpreadAngle;
        private bool CanLearnPiercing(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnPiercing &&
            GetProgress(unit) != null && !HasPiercing(unit);
        private bool CanLearnMultiShot(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnMultiShot &&
            GetProgress(unit) is UnitProgress progress && progress.MultiShotLevel < multiShotMaximumLevel;
        private bool CanLearnFocus(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnFocus &&
            GetProgress(unit) is UnitProgress progress && progress.FocusLevel < focusMaximumLevel;
        private bool CanLearnSuppression(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnSuppression &&
            GetProgress(unit) != null && !HasSuppression(unit);
        private bool CanLearnExplosion(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnExplosion &&
            GetProgress(unit) != null && !HasExplosion(unit);
        private bool CanLearnWeaponStrike(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnWeaponStrike &&
            GetProgress(unit) != null && !HasWeaponStrike(unit);
        private bool CanLearnBloodthirst(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnBloodthirst &&
            GetProgress(unit) is UnitProgress progress && progress.BloodthirstLevel < bloodthirstMaximumLevel;
        private bool CanLearnSpinningSword(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnSpinningSword &&
            GetProgress(unit) is UnitProgress progress && progress.SpinningSwordLevel < spinningSwordMaximumLevel;
        private bool CanLearnSpinSlash(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnSpinSlash &&
            GetProgress(unit) is UnitProgress progress && progress.SpinSlashLevel < spinSlashMaximumLevel;
        private bool CanLearnBanner(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnBanner &&
            GetProgress(unit) is UnitProgress progress && progress.BannerLevel < bannerMaximumLevel;

        private bool CanLearnMagicTrap(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnMagicTrap &&
            GetProgress(unit) != null && !HasMagicTrap(unit);

        private bool CanLearnManaCycle(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnManaCycle &&
            GetProgress(unit) is UnitProgress progress && progress.ManaCycleLevel < manaCycleMaximumLevel;

        public bool HasLaserAOE(Transform unit) => GetProgress(unit)?.LaserAOE == true;
        public bool HasChainLightning(Transform unit) => GetProgress(unit)?.ChainLightning == true;
        public bool HasTeamShield(Transform unit) => GetProgress(unit)?.TeamShield == true;
        public bool HasTeamAttack(Transform unit) => GetProgress(unit)?.TeamAttack == true;
        public bool HasIntimidatingShout(Transform unit) => GetProgress(unit)?.IntimidatingShout == true;
        private bool CanLearnIronWall(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnIronWall &&
            GetProgress(unit) is UnitProgress progress && !progress.IronWall;

        public bool HasTeamAttackSpeed(Transform unit) => GetProgress(unit)?.TeamAttackSpeed == true;
        public bool HasChainCharge(Transform unit) => GetProgress(unit)?.ChainCharge == true;
        public int FlameHoovesLevel(Transform unit) => GetProgress(unit)?.FlameHoovesLevel ?? 0;
        public bool HasChargeInvulnerability(Transform unit) => GetProgress(unit)?.ChargeInvulnerability == true;
        public bool HasMarchingHorn(Transform unit) => GetProgress(unit)?.MarchingHorn == true;
        public bool HasMomentum(Transform unit) => GetProgress(unit)?.Momentum == true;
        private bool CanLearnMomentum(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnMomentum &&
            GetProgress(unit) != null && !HasMomentum(unit);
        private bool CanLearnMarchingHorn(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnMarchingHorn &&
            GetProgress(unit) != null && !HasMarchingHorn(unit);
        private bool CanLearnChargeInvulnerability(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnChargeInvulnerability &&
            GetProgress(unit) != null && !HasChargeInvulnerability(unit);
        private bool CanLearnFlameHooves(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnFlameHooves &&
            GetProgress(unit) != null && FlameHoovesLevel(unit) < 3;
        private bool CanLearnChainCharge(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnChainCharge &&
            GetProgress(unit) != null && !HasChainCharge(unit);
        private bool CanLearnTeamAttackSpeed(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnTeamAttackSpeed &&
            GetProgress(unit) != null && !HasTeamAttackSpeed(unit);
        private bool CanLearnIntimidatingShout(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnIntimidatingShout &&
            GetProgress(unit) != null && !HasIntimidatingShout(unit);
        private bool CanLearnTeamAttack(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnTeamAttack &&
            GetProgress(unit) != null && !HasTeamAttack(unit);
        private bool CanLearnTeamShield(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnTeamShield &&
            GetProgress(unit) != null && !HasTeamShield(unit);
        private bool CanLearnChainLightning(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnChainLightning &&
            GetProgress(unit) != null && !HasChainLightning(unit);
        private bool CanLearnLaserAOE(Transform unit) => unit != null &&
            recruitment.Units.TryGetValue(unit, out var definition) && definition.canLearnLaserAOE &&
            GetProgress(unit) != null && !HasLaserAOE(unit);

        private GameObject CardFor(int choice)
        {
            switch (choice)
            {
                case 3: return piercingCard;
                case 4: return multiShotCard;
                case 5: return focusCard;
                case 6: return suppressionCard;
                case 7: return explosionCard;
                case 8: return weaponStrikeCard;
                case 9: return bloodthirstCard;
                case 10: return spinningSwordCard;
                case 11: return spinSlashCard;
                case 12: return bannerCard;
                case 13: return magicTrapCard;
                case 14: return manaCycleCard;
                case 15: return laserCard;
                case 16: return chainLightningCard;
                case 17: return teamShieldCard;
                case 18: return teamAttackCard;
                case 19: return shoutCard;
                case 20: return ironWallCard;
                case 21: return teamAttackSpeedCard;
                case 22: return chainChargeCard;
                case 23: return flameHoovesCard;
                case 24: return chargeInvulnerabilityCard;
                case 25: return marchingHornCard;
                case 26: return momentumCard;
                default: return null;
            }
        }

        private void ShowNextUpgrade()
        {
            selectionInProgress = false;
            shownChoices.Clear();
            while (levelUps.Count > 0)
            {
                LevelUpRequest request = levelUps.Peek();
                List<int> candidates = new List<int>();
                if (piercingCard != null && CanLearnPiercing(request.unit)) candidates.Add(3);
                if (multiShotCard != null && CanLearnMultiShot(request.unit)) candidates.Add(4);
                if (focusCard != null && CanLearnFocus(request.unit)) candidates.Add(5);
                if (suppressionCard != null && CanLearnSuppression(request.unit)) candidates.Add(6);
                if (explosionCard != null && CanLearnExplosion(request.unit)) candidates.Add(7);
                if (weaponStrikeCard != null && CanLearnWeaponStrike(request.unit)) candidates.Add(8);
                if (bloodthirstCard != null && CanLearnBloodthirst(request.unit)) candidates.Add(9);
                if (spinningSwordCard != null && CanLearnSpinningSword(request.unit)) candidates.Add(10);
                if (spinSlashCard != null && CanLearnSpinSlash(request.unit)) candidates.Add(11);
                if (bannerCard != null && CanLearnBanner(request.unit)) candidates.Add(12);
                if (magicTrapCard != null && CanLearnMagicTrap(request.unit)) candidates.Add(13);
                if (manaCycleCard != null && CanLearnManaCycle(request.unit)) candidates.Add(14);
                if (laserCard != null && CanLearnLaserAOE(request.unit)) candidates.Add(15);
                if (chainLightningCard != null && CanLearnChainLightning(request.unit)) candidates.Add(16);
                if (teamShieldCard != null && CanLearnTeamShield(request.unit)) candidates.Add(17);
                if (teamAttackCard != null && CanLearnTeamAttack(request.unit)) candidates.Add(18);
                if (shoutCard != null && CanLearnIntimidatingShout(request.unit)) candidates.Add(19);
                if (ironWallCard != null && CanLearnIronWall(request.unit)) candidates.Add(20);
                if (teamAttackSpeedCard != null && CanLearnTeamAttackSpeed(request.unit)) candidates.Add(21);
                if (chainChargeCard != null && CanLearnChainCharge(request.unit)) candidates.Add(22);
                if (flameHoovesCard != null && CanLearnFlameHooves(unit: request.unit)) candidates.Add(23);
                if (chargeInvulnerabilityCard != null && CanLearnChargeInvulnerability(request.unit)) candidates.Add(24);
                if (marchingHornCard != null && CanLearnMarchingHorn(request.unit)) candidates.Add(25);
                if (momentumCard != null && CanLearnMomentum(request.unit)) candidates.Add(26);
                if (candidates.Count == 0)
                {
                    // 모든 스킬을 배웠거나 이 병사에게 배울 수 있는 스킬이 없다.
                    levelUps.Dequeue();
                    continue;
                }
                if (!IsChoosingUpgrade)
                {
                    previousTimeScale = Time.timeScale;
                    Time.timeScale = 0;
                    IsChoosingUpgrade = true;
                    run.IsChoosingUpgrade = true;
                }
                for (int choice = 3; choice <= 26; choice++)
                    if (CardFor(choice) != null) CardFor(choice).SetActive(false);
                // 부분 Fisher-Yates 셔플로 후보 중 최대 3개를 균등하게 선택한다.
                int count = Mathf.Min(maximumCardChoices, candidates.Count);
                for (int i = 0; i < count; i++)
                {
                    int randomIndex = UnityEngine.Random.Range(i, candidates.Count);
                    (candidates[i], candidates[randomIndex]) = (candidates[randomIndex], candidates[i]);
                    shownChoices.Add(candidates[i]);
                    CardFor(candidates[i]).SetActive(true);
                }
                RefreshCardLayout();
                if (multiShotDescription != null && CanLearnMultiShot(request.unit))
                {
                    int currentCount = MultiShotCount(request.unit);
                    multiShotDescription.text = $"3갈래 화살 발사\n\n현재 {currentCount}갈래 → 다음 강화 +1갈래\n최대 5갈래 · 이 궁수에게만 적용";
                }
                if (focusDescription != null && CanLearnFocus(request.unit))
                {
                    int nextLevel = (GetProgress(request.unit)?.FocusLevel ?? 0) + 1;
                    float multiplier = focusBaseCriticalMultiplier + (nextLevel - 1) * focusCriticalMultiplierPerLevel;
                    focusDescription.text = $"정지 중 치명타 확률 +{focusCriticalChance * 100:0}%\n\n다음 치명타 피해 x{multiplier:0.0}\n정지 {focusStationaryDelay:0.0}초 뒤 활성화 · 궁수에게만 적용";
                }
                if (suppressionDescription != null && CanLearnSuppression(request.unit))
                    suppressionDescription.text = $"화살에 맞은 적의 이동 속도 {combat.SuppressionSlowPercent:0}% 감소\n\n{combat.SuppressionDuration:0.0}초 지속 · 재명중 시 시간 갱신\n관통·멀티샷에도 적용";
                if (explosionDescription != null && CanLearnExplosion(request.unit))
                    explosionDescription.text = $"화살 명중 시 주변 적에게 피해\n\n반경 {combat.ExplosionRadius:0.0} · 화살 피해의 {combat.ExplosionDamagePercent:0}%\n관통·멀티샷에도 적용";
                foreach (ClassRow row in rows)
                    if (row.unit == request.unit) levelUpTarget.text = $"{row.title.text} · Lv.{request.level}";
                levelUpPanel.SetActive(true);
                levelUpPanel.transform.SetAsLastSibling();
                if (focusCardTemplate.transform.parent is RectTransform cardRow)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(cardRow);
                for (int i = 0; i < shownChoices.Count; i++)
                {
                    int choice = candidates[i];
                    if (cardViews.TryGetValue(choice, out CardMotion view))
                        view.PlayEntrance(i);
                }
                return;
            }
            CloseUpgrade();
        }

        private void RefreshCardLayout()
        {
            if (focusCardTemplate == null) return;
            RectTransform parent = focusCardTemplate.transform.parent as RectTransform;
            if (parent == null) return;
            HorizontalLayoutGroup layout = parent.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) return;
            float width = layout.padding.horizontal;
            int activeCards = 0;
            foreach (Transform child in parent)
                if (child.gameObject.activeSelf && child is RectTransform rect)
                {
                    width += rect.sizeDelta.x;
                    activeCards++;
                }
            width += Mathf.Max(0, activeCards - 1) * layout.spacing;
            parent.sizeDelta = new Vector2(width, parent.sizeDelta.y);
        }

        public bool ChooseUpgrade(int choice)
        {
            if (!run.IsRunning || !IsChoosingUpgrade || levelUps.Count == 0 || !shownChoices.Contains(choice)) return false;
            if (choice == 3 && !CanLearnPiercing(levelUps.Peek().unit)) return false;
            if (choice == 4 && !CanLearnMultiShot(levelUps.Peek().unit)) return false;
            if (choice == 5 && !CanLearnFocus(levelUps.Peek().unit)) return false;
            if (choice == 6 && !CanLearnSuppression(levelUps.Peek().unit)) return false;
            if (choice == 7 && !CanLearnExplosion(levelUps.Peek().unit)) return false;
            if (choice == 8 && !CanLearnWeaponStrike(levelUps.Peek().unit)) return false;
            if (choice == 9 && !CanLearnBloodthirst(levelUps.Peek().unit)) return false;
            if (choice == 10 && !CanLearnSpinningSword(levelUps.Peek().unit)) return false;
            if (choice == 11 && !CanLearnSpinSlash(levelUps.Peek().unit)) return false;
            if (choice == 12 && !CanLearnBanner(levelUps.Peek().unit)) return false;
            if (choice == 13 && !CanLearnMagicTrap(levelUps.Peek().unit)) return false;
            if (choice == 14 && !CanLearnManaCycle(levelUps.Peek().unit)) return false;
            if (choice == 15 && !CanLearnLaserAOE(levelUps.Peek().unit)) return false;
            if (choice == 16 && !CanLearnChainLightning(levelUps.Peek().unit)) return false;
            if (choice == 17 && !CanLearnTeamShield(levelUps.Peek().unit)) return false;
            if (choice == 18 && !CanLearnTeamAttack(levelUps.Peek().unit)) return false;
            if (choice == 19 && !CanLearnIntimidatingShout(levelUps.Peek().unit)) return false;
            if (choice == 20 && !CanLearnIronWall(levelUps.Peek().unit)) return false;
            if (choice == 21 && !CanLearnTeamAttackSpeed(levelUps.Peek().unit)) return false;
            if (choice == 22 && !CanLearnChainCharge(levelUps.Peek().unit)) return false;
            if (choice == 23 && !CanLearnFlameHooves(levelUps.Peek().unit)) return false;
            if (choice == 24 && !CanLearnChargeInvulnerability(levelUps.Peek().unit)) return false;
            if (choice == 25 && !CanLearnMarchingHorn(levelUps.Peek().unit)) return false;
            if (choice == 26 && !CanLearnMomentum(levelUps.Peek().unit)) return false;
            LevelUpRequest request = levelUps.Dequeue();
            UnitProgress progress = GetProgress(request.unit);
            if (progress != null)
            {
                if (choice == 3) progress.Piercing = true;
                else if (choice == 4) progress.MultiShotLevel++;
                else if (choice == 5) progress.FocusLevel++;
                else if (choice == 6) progress.Suppression = true;
                else if (choice == 7) progress.Explosion = true;
                else if (choice == 8) progress.WeaponStrike = true;
                else if (choice == 9) progress.BloodthirstLevel++;
                else if (choice == 10) progress.SpinningSwordLevel++;
                else if (choice == 11) progress.SpinSlashLevel++;
                else if (choice == 12) progress.BannerLevel++;
                else if (choice == 13) progress.MagicTrap = true;
                else if (choice == 14) progress.ManaCycleLevel++;
                else if (choice == 15) progress.LaserAOE = true;
                else if (choice == 16) progress.ChainLightning = true;
                else if (choice == 17) progress.TeamShield = true;
                else if (choice == 18) progress.TeamAttack = true;
                else if (choice == 19) progress.IntimidatingShout = true;
                else if (choice == 21) progress.TeamAttackSpeed = true;
                else if (choice == 22) progress.ChainCharge = true;
                else if (choice == 23) progress.FlameHoovesLevel++;
                else if (choice == 24) progress.ChargeInvulnerability = true;
                else if (choice == 25) progress.MarchingHorn = true;
                else if (choice == 26) progress.Momentum = true;
                else if (choice == 20)
                {
                    progress.IronWall = true;
                    if (request.unit.TryGetComponent<UnitHealth>(out var health))
                        health.DamageReduction = ironWallDamageReduction;
                }
            }
            if (levelUps.Count > 0) ShowNextUpgrade();
            else CloseUpgrade();
            RefreshUI();
            return true;
        }

        private void CloseUpgrade()
        {
            selectionInProgress = false;
            shownChoices.Clear();
            if (IsChoosingUpgrade) Time.timeScale = previousTimeScale;
            IsChoosingUpgrade = false;
            if (run != null) run.IsChoosingUpgrade = false;
            if (levelUpPanel != null) levelUpPanel.SetActive(false);
        }

        private static void SetBar(Image bar, float ratio)
        {
            // 단색 이미지를 왼쪽부터 늘려 스프라이트의 투명 여백 영향을 없앤다.
            bar.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
        }

        private void Clear()
        {
            levelUps.Clear();
            CloseUpgrade();
            if (dropRoot != null)
            {
                dropRoot.gameObject.SetActive(false);
                Destroy(dropRoot.gameObject);
            }
            dropRoot = null;
            drops.Clear();
            foreach (Transform effect in focusEffects.Values)
                if (effect != null) Destroy(effect.gameObject);
            focusEffects.Clear();
            focusStationaryTimes.Clear();
            units.Clear();
            foreach (ClassRow row in rows) row.unit = null;
            Bottles = 0;
            if (panel != null) panel.SetActive(false);
        }
    }
}
