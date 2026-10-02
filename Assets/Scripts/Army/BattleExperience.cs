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

        private struct LevelUpRequest
        {
            public Transform unit;
            public int level;
        }
        private readonly Queue<LevelUpRequest> levelUps = new Queue<LevelUpRequest>();
        private readonly HashSet<int> shownChoices = new HashSet<int>();
        private float previousTimeScale = 1;
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
        private GameObject weaponStrikeCard;
        private Button weaponStrikeButton;
        private TMP_Text weaponStrikeDescription;
        private Transform dropRoot;
        public int Bottles { get; private set; }
        public int DropCount => drops.Count;
        public event Action<Transform, int> UnitLeveledUp;

        private void Awake()
        {
            CreateFocusCard();
            CreateSuppressionCard();
            explosionCard = CreateSkillCard("Explosion Card", "폭발 사격", out explosionButton, out explosionDescription);
            weaponStrikeCard = CreateSkillCard("Weapon Strike Card", "낙하 창", out weaponStrikeButton, out weaponStrikeDescription);
            foreach (ClassRow row in rows)
                row.invest.onClick.AddListener(() => Invest(row.unit));
            panel.SetActive(false);
            levelUpPanel.SetActive(false);
            if (piercingButton != null) piercingButton.onClick.AddListener(() => ChooseUpgrade(3));
            if (multiShotButton != null) multiShotButton.onClick.AddListener(() => ChooseUpgrade(4));
            if (focusButton != null) focusButton.onClick.AddListener(() => ChooseUpgrade(5));
            if (suppressionButton != null) suppressionButton.onClick.AddListener(() => ChooseUpgrade(6));
            if (explosionButton != null) explosionButton.onClick.AddListener(() => ChooseUpgrade(7));
            if (weaponStrikeButton != null) weaponStrikeButton.onClick.AddListener(() => ChooseUpgrade(8));
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
                default: return null;
            }
        }

        private void ShowNextUpgrade()
        {
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
                for (int choice = 3; choice <= 8; choice++)
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
                if (weaponStrikeDescription != null && CanLearnWeaponStrike(request.unit))
                    weaponStrikeDescription.text = $"{combat.WeaponStrikeInterval:0}초마다 무작위 적 위치에 창 낙하\n\n{combat.WeaponStrikeImpactDelay:0.0}초 뒤 착탄 · 반경 {combat.WeaponStrikeRadius:0.0} 광역 피해\n이 병사에게만 적용";
                foreach (ClassRow row in rows)
                    if (row.unit == request.unit) levelUpTarget.text = $"{row.title.text} · Lv.{request.level}";
                levelUpPanel.SetActive(true);
                levelUpPanel.transform.SetAsLastSibling();
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
            }
            if (levelUps.Count > 0) ShowNextUpgrade();
            else CloseUpgrade();
            RefreshUI();
            return true;
        }

        private void CloseUpgrade()
        {
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
