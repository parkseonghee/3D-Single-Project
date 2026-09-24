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
        }

        [SerializeField] private HireOption[] options;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform soldiersRoot;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject formationFull;
        private int lastRice = -1;
        [SerializeField] private string villageScene = "VillageScene";
        [SerializeField, Min(0.01f)] private float appearDuration = 0.2f;

        private SceneTravel travel;
        private BuildingPlacementController village;
        private int hiredCount;
        public int HiredCount => hiredCount;
        public bool IsPreparing { get; set; } = true;
        public Transform SoldiersRoot => soldiersRoot;
        public readonly System.Collections.Generic.Dictionary<Transform, UnitDefinition> Units =
            new System.Collections.Generic.Dictionary<Transform, UnitDefinition>();
        private readonly System.Collections.Generic.Dictionary<Transform, UnitDefinition> reserves =
            new System.Collections.Generic.Dictionary<Transform, UnitDefinition>();
        private Transform reserveRoot;

        private void Start()
        {
            travel = FindFirstObjectByType<SceneTravel>();
            village = travel != null ? travel.Village : null;
            foreach (HireOption option in options)
            {
                option.hireButton.onClick.AddListener(() => TryHire(option.unit));
                if (option.addButton != null) option.addButton.onClick.AddListener(() => TryAdd(option.unit));
                if (option.removeButton != null) option.removeButton.onClick.AddListener(() => TryRemove(option.unit));
            }
            backButton.onClick.AddListener(Back);
            RestoreSavedTroops();
            RefreshUI();
        }

        private void Update()
        {
            if (village != null && lastRice != village.Rice) RefreshUI();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public bool TryHire(UnitDefinition unit)
        {
            if (!IsPreparing || unit == null || unit.prefab == null || village == null) return false;
            if (!village.HasBuilding(unit.requiredBuilding) || hiredCount >= spawnPoints.Length) return false;
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
            StartCoroutine(Appear(root, Vector3.one));
            RefreshUI();
            return true;
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
            Rearrange();
            RefreshUI();
            return true;
        }

        public bool TryAdd(UnitDefinition unit)
        {
            if (!IsPreparing || unit == null || hiredCount >= spawnPoints.Length) return false;
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
            return true;
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
            bool full = hiredCount >= spawnPoints.Length;
            formationFull.SetActive(full);
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
                option.riceRequired.SetActive(unlocked && !affordable);
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
