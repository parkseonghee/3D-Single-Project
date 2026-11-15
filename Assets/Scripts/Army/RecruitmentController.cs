using System;
using System.Collections;
using TMPro;
using ArmySurvivor.Building;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public partial class RecruitmentController : MonoBehaviour
    {
        [Serializable]
        private class HireOption
        {
            public UnitDefinition unit;
            public Button hireButton;
            public Button addButton;
            public Button removeButton;
            public TMP_Text formationCount;
            public GameObject buildingRequired;
            public GameObject riceRequired;
            [Tooltip("마을 건물 강화 보너스 표시 (기획서 9). 보너스가 없으면 빈 문구")]
            public TMP_Text buildingBonus;
            [Tooltip("UnitDefinition.riceCost를 costFormat으로 표시")]
            public TMP_Text cost;
        }
        [SerializeField] private string costFormat = "벼 {0}";
        [SerializeField] private string buildingBonusFormat = "공격력·체력 +{0}%";
        [SerializeField] private string buildingBonusSplitFormat = "공격력 +{0}% · 체력 +{1}%";

        [SerializeField] private HireOption[] options;
        [SerializeField] private ScrollRect hireScroll;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform soldiersRoot;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject formationFull;
        [Header("편성 상한 (기획서 9) · 최대치는 Spawn Points 개수")]
        [SerializeField, Min(1)] private int baseFormationSlots = 3;
        [SerializeField, Min(1)] private int survivalSquadSize = 5;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private string capacityFormat = "편성 상한 {0}명";
        private int lastRice = -1;
        [SerializeField] private string villageScene = "VillageScene";
        [SerializeField, Min(0.01f)] private float appearDuration = 0.2f;

        private SceneTravel travel;
        private BuildingPlacementController village;
        private int hiredCount;
        public int HiredCount => hiredCount;
        // 마을 건물 강화로 얻는 공격력(x)·체력(y) 보너스. 서바이벌에는 마을이 없어 0이다.
        public Vector2 BuildingStatBonus(UnitDefinition unit) =>
            village != null && unit != null && GameModeManager.Current == GameMode.Adventure
                ? village.GetUnitStatBonus(unit.requiredBuilding) : Vector2.zero;
        // 기본 칸 + 마을 건물(막사) 기여. 배치 위치 수를 넘지 않는다.
        public int FormationCap
        {
            get
            {
                int max = spawnPoints != null ? spawnPoints.Length : 0;
                if (GameModeManager.Current == GameMode.Survival) return Mathf.Min(survivalSquadSize, max);
                long cap = baseFormationSlots + (long)(village != null ? village.GetFormationSlots() : 0);
                return (int)System.Math.Min(cap, max);
            }
        }
        public bool IsPreparing { get; set; } = true;
        public Transform SoldiersRoot => soldiersRoot;
        public readonly System.Collections.Generic.Dictionary<Transform, UnitDefinition> Units =
            new System.Collections.Generic.Dictionary<Transform, UnitDefinition>();
        public bool HasAssignedSoldier
        {
            get
            {
                foreach (var unit in Units)
                    if (unit.Key != null && unit.Key.gameObject.activeInHierarchy) return true;
                return false;
            }
        }
        private readonly System.Collections.Generic.Dictionary<Transform, UnitDefinition> reserves =
            new System.Collections.Generic.Dictionary<Transform, UnitDefinition>();
        private Transform reserveRoot;

        private void Start()
        {
            travel = FindFirstObjectByType<SceneTravel>(FindObjectsInactive.Include);
            village = travel != null ? travel.Village : null;
            foreach (HireOption option in options)
            {
                option.hireButton.onClick.AddListener(() => TryHire(option.unit));
                if (option.addButton != null) option.addButton.onClick.AddListener(() => TryAdd(option.unit));
                if (option.removeButton != null) option.removeButton.onClick.AddListener(() => TryRemove(option.unit));
            }
            backButton.onClick.AddListener(Back);
            if (GameModeManager.Current == GameMode.Survival)
            {
                backButton.gameObject.SetActive(false);
                GenerateSurvivalSquad();
            }
            else RestoreSavedTroops();
            RefreshUI();
            ShowFirstHireOption();
        }

        private void Update()
        {
            if (village != null && lastRice != village.Rice) RefreshUI();
        }

        private void OnEnable()
        {
            EnforceFormationCap();
            RefreshUI();
            ShowFirstHireOption();
        }

        private void ShowFirstHireOption()
        {
            if (!IsPreparing || hireScroll == null) return;
            Canvas.ForceUpdateCanvases();
            hireScroll.StopMovement();
            hireScroll.verticalNormalizedPosition = 1f;
        }

        public bool TryHire(UnitDefinition unit)
        {
            if (!IsPreparing || unit == null || unit.prefab == null || village == null) return false;
            if (!village.HasBuilding(unit.requiredBuilding) || hiredCount >= FormationCap) return false;
            if (!village.TrySpendRice(unit.riceCost)) return false;

            Transform point = spawnPoints[hiredCount];
            Transform root = new GameObject(unit.name).transform;
            root.SetParent(soldiersRoot, false);
            root.SetPositionAndRotation(point.position, point.rotation);
            GameObject soldier = Instantiate(unit.prefab, root);
            soldier.transform.localPosition = unit.modelOffset;
            soldier.transform.localRotation = Quaternion.Euler(unit.modelRotation);
            soldier.transform.localScale = unit.modelScale;
            foreach (Animator animator in soldier.GetComponentsInChildren<Animator>())
                animator.applyRootMotion = false;
            hiredCount++;
            Units.Add(root, unit);
            var tutorialTarget = root.gameObject.AddComponent<ArmySurvivor.Tutorial.TutorialTarget>();
            tutorialTarget.SetId("Soldier");
            StartCoroutine(Appear(root, Vector3.one));
            RefreshUI();
            ArmySurvivor.Audio.GameAudio.Play(ArmySurvivor.Audio.Sfx.Recruit);
            ArmySurvivor.Tutorial.TutorialEvents.Raise("SoldierHired", root);
            return true;
        }

        public void GenerateSurvivalSquad()
        {
            if (GameModeManager.Current != GameMode.Survival || soldiersRoot == null || spawnPoints == null) return;
            FinishAppearances();
            for (int i = soldiersRoot.childCount - 1; i >= 0; i--)
            {
                Transform soldier = soldiersRoot.GetChild(i);
                soldier.SetParent(null);
                Destroy(soldier.gameObject);
            }
            Units.Clear();
            hiredCount = 0;
            var pool = new System.Collections.Generic.List<UnitDefinition>();
            foreach (var option in options)
                if (option != null && option.unit != null && option.unit.prefab != null && !pool.Contains(option.unit))
                    pool.Add(option.unit);
            if (pool.Count == 0) { Debug.LogError("서바이벌 무작위 병사 후보가 없습니다.", this); return; }
            for (int i = 0; i < FormationCap; i++)
            {
                UnitDefinition unit = pool[UnityEngine.Random.Range(0, pool.Count)];
                Transform root = new GameObject(unit.name).transform;
                root.SetParent(soldiersRoot, false);
                root.SetPositionAndRotation(spawnPoints[i].position, spawnPoints[i].rotation);
                GameObject model = Instantiate(unit.prefab, root);
                model.transform.localPosition = unit.modelOffset;
                model.transform.localRotation = Quaternion.Euler(unit.modelRotation);
                model.transform.localScale = unit.modelScale;
                foreach (Animator animator in model.GetComponentsInChildren<Animator>()) animator.applyRootMotion = false;
                root.gameObject.AddComponent<ArmySurvivor.Tutorial.TutorialTarget>().SetId("Soldier");
                Units.Add(root, unit);
                hiredCount++;
            }
            RefreshUI();
        }

        private IEnumerator Appear(Transform soldier, Vector3 scale)
        {
            float elapsed = 0;
            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / appearDuration);
                soldier.localScale = scale * Mathf.Sin(t * Mathf.PI * 0.5f);
                yield return null;
            }
            soldier.localScale = scale;
        }

        public int AssignedCount(UnitDefinition unit)
        {
            int count = 0;
            foreach (var entry in Units) if (entry.Value == unit) count++;
            return count;
        }

        public int ReserveCount(UnitDefinition unit)
        {
            int count = 0;
            foreach (var entry in reserves) if (entry.Value == unit) count++;
            return count;
        }

        public bool TryRemove(UnitDefinition unit)
        {
            if (!IsPreparing || unit == null) return false;
            Transform target = null;
            foreach (var entry in Units) if (entry.Value == unit) target = entry.Key;
            if (target == null) return false;
            FinishAppearances();
            MoveToReserve(target, unit);
            Rearrange();
            RefreshUI();
            ArmySurvivor.Tutorial.TutorialEvents.Raise("FormationChanged", target);
            return true;
        }

        public bool TryAdd(UnitDefinition unit)
        {
            if (!IsPreparing || unit == null || hiredCount >= FormationCap) return false;
            Transform target = null;
            foreach (var entry in reserves) if (entry.Value == unit) { target = entry.Key; break; }
            if (target == null) return false;
            FinishAppearances();
            reserves.Remove(target);
            Units.Add(target, unit);
            target.SetParent(soldiersRoot, false);
            target.gameObject.SetActive(true);
            hiredCount++;
            Rearrange();
            RefreshUI();
            ArmySurvivor.Tutorial.TutorialEvents.Raise("FormationChanged", target);
            return true;
        }

        private void MoveToReserve(Transform target, UnitDefinition unit)
        {
            if (reserveRoot == null)
            {
                reserveRoot = new GameObject("Reserve Troops").transform;
                reserveRoot.SetParent(transform, false);
            }
            Units.Remove(target);
            reserves.Add(target, unit);
            target.SetParent(reserveRoot, true);
            target.gameObject.SetActive(false);
            hiredCount--;
        }

        // 막사 철거 등으로 상한이 줄면 뒤쪽 편성부터 대기로 옮긴다. 편성 화면에 들어올 때 확인한다.
        private void EnforceFormationCap()
        {
            if (village == null || !IsPreparing || soldiersRoot == null || GameModeManager.Current == GameMode.Survival) return;
            int cap = FormationCap;
            if (hiredCount <= cap) return;
            FinishAppearances();
            for (int i = soldiersRoot.childCount - 1; i >= 0 && hiredCount > cap; i--)
            {
                Transform soldier = soldiersRoot.GetChild(i);
                if (Units.TryGetValue(soldier, out var unit)) MoveToReserve(soldier, unit);
            }
            Rearrange();
        }

        private void Rearrange()
        {
            int index = 0;
            foreach (Transform soldier in soldiersRoot)
            {
                Transform point = spawnPoints[index++];
                soldier.SetPositionAndRotation(point.position, point.rotation);
            }
        }

        private void OnDisable()
        {
            FinishAppearances();
        }

        public void FinishAppearances()
        {
            // 등장 도중 화면을 전환해도 유닛이 작은 크기로 남지 않는다.
            StopAllCoroutines();
            if (soldiersRoot != null)
                foreach (Transform soldier in soldiersRoot) soldier.localScale = Vector3.one;
        }

        private void RefreshUI()
        {
            // 고정 문구는 Inspector에서 입력하고, 보유량 숫자만 갱신한다.
            lastRice = village != null ? village.Rice : -1;
            int cap = FormationCap;
            bool full = hiredCount >= cap;
            formationFull.SetActive(full);
            if (capacityText != null) capacityText.text = string.Format(capacityFormat, cap);
            foreach (HireOption option in options)
            {
                bool unlocked = village != null && village.HasBuilding(option.unit.requiredBuilding);
                bool affordable = village != null && village.Rice >= option.unit.riceCost;
                option.hireButton.interactable = IsPreparing && unlocked && affordable && !full;
                int assigned = AssignedCount(option.unit);
                int reserve = ReserveCount(option.unit);
                if (option.addButton != null) option.addButton.interactable = IsPreparing && reserve > 0 && !full;
                if (option.removeButton != null) option.removeButton.interactable = IsPreparing && assigned > 0;
                // 편성 / 보유 숫자만 갱신한다. 버튼과 설명 문구는 Inspector에서 편집한다.
                if (option.formationCount != null) option.formationCount.text = $"{assigned} / {assigned + reserve}";
                // 문구는 Inspector에 둔다. 코드는 필요한 안내 오브젝트만 켜고 끈다.
                option.buildingRequired.SetActive(!unlocked);
                if (option.buildingBonus != null)
                {
                    Vector2 bonus = BuildingStatBonus(option.unit);
                    int damage = Mathf.RoundToInt(bonus.x * 100), health = Mathf.RoundToInt(bonus.y * 100);
                    option.buildingBonus.text = damage == 0 && health == 0 ? ""
                        : damage == health ? string.Format(buildingBonusFormat, damage)
                        : string.Format(buildingBonusSplitFormat, damage, health);
                }
                option.riceRequired.SetActive(unlocked && !affordable);
                if (option.cost != null) option.cost.text = string.Format(costFormat, option.unit.riceCost);
            }
        }

        private void Back()
        {
            if (!IsPreparing) return;
            if (travel != null) travel.ReturnToVillage();
            else SceneManager.LoadScene(villageScene);
        }
    }
}
