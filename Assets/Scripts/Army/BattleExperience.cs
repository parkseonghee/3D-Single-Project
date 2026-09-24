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
            public float DamageBonus { get; internal set; }
            public float SpeedBonus { get; internal set; }
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
        [Header("레벨업 카드")]
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private TMP_Text levelUpTarget;
        [SerializeField] private Button[] upgradeButtons;
        [SerializeField, Min(0)] private float damageUpgrade = 0.2f;
        [SerializeField, Min(0)] private float speedUpgrade = 0.15f;
        [SerializeField, Min(0)] private float healthUpgrade = 0.25f;

        private struct LevelUpRequest
        {
            public Transform unit;
            public int level;
        }
        private readonly Queue<LevelUpRequest> levelUps = new Queue<LevelUpRequest>();
        private float previousTimeScale = 1;
        public bool IsChoosingUpgrade { get; private set; }

        private readonly Dictionary<Transform, UnitProgress> units = new Dictionary<Transform, UnitProgress>();
        private readonly List<Transform> drops = new List<Transform>();
        private Transform dropRoot;
        public int Bottles { get; private set; }
        public int DropCount => drops.Count;
        public event Action<Transform, int> UnitLeveledUp;

        private void Awake()
        {
            foreach (ClassRow row in rows)
                row.invest.onClick.AddListener(() => Invest(row.unit));
            panel.SetActive(false);
            levelUpPanel.SetActive(false);
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                int choice = i;
                upgradeButtons[i].onClick.AddListener(() => ChooseUpgrade(choice));
            }
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
            position.y = run.Commander.position.y + 0.35f;
            drops.Add(Instantiate(bottlePrefab, position, Quaternion.identity, dropRoot).transform);
        }

        private void Update()
        {
            CollectNearby();
            if (run.IsRunning) RefreshUI();
        }

        public void CollectNearby()
        {
            if (!run.IsRunning) return;
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

        public float DamageMultiplier(Transform unit) => GetProgress(unit) is UnitProgress p ? 1 + p.DamageBonus : 1;
        public float SpeedMultiplier(Transform unit) => GetProgress(unit) is UnitProgress p ? 1 + p.SpeedBonus : 1;

        private void ShowNextUpgrade()
        {
            if (levelUps.Count == 0) return;
            if (!IsChoosingUpgrade)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0;
                IsChoosingUpgrade = true;
                run.IsChoosingUpgrade = true;
            }
            LevelUpRequest request = levelUps.Peek();
            foreach (ClassRow row in rows)
                if (row.unit == request.unit) levelUpTarget.text = $"{row.title.text} · Lv.{request.level}";
            levelUpPanel.SetActive(true);
            levelUpPanel.transform.SetAsLastSibling();
        }

        public bool ChooseUpgrade(int choice)
        {
            if (!run.IsRunning || !IsChoosingUpgrade || levelUps.Count == 0 || choice < 0 || choice > 2) return false;
            LevelUpRequest request = levelUps.Dequeue();
            UnitProgress progress = GetProgress(request.unit);
            if (progress != null)
            {
                if (choice == 0) progress.DamageBonus += damageUpgrade;
                else if (choice == 1) progress.SpeedBonus += speedUpgrade;
                else
                {
                    var health = request.unit.GetComponent<UnitHealth>();
                    if (health != null && !health.IsDead)
                        health.IncreaseMaximum(recruitment.Units[request.unit].health * healthUpgrade);
                }
            }
            if (levelUps.Count > 0) ShowNextUpgrade();
            else CloseUpgrade();
            RefreshUI();
            return true;
        }

        private void CloseUpgrade()
        {
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
            units.Clear();
            foreach (ClassRow row in rows) row.unit = null;
            Bottles = 0;
            if (panel != null) panel.SetActive(false);
        }
    }
}
