using System;
using System.Collections;
using TMPro;
using ArmySurvivor.Building;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class RecruitmentController : MonoBehaviour
    {
        [Serializable]
        private class HireOption
        {
            public UnitDefinition unit;
            public Button hireButton;
            public GameObject buildingRequired;
            public GameObject riceRequired;
        }

        [SerializeField] private HireOption[] options;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Transform soldiersRoot;
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject formationFull;
        [SerializeField] private TMP_Text riceAmount;
        [SerializeField] private string villageScene = "VillageScene";
        [SerializeField, Min(0.01f)] private float appearDuration = 0.2f;

        private SceneTravel travel;
        private BuildingPlacementController village;
        private int hiredCount;
        public int HiredCount => hiredCount;

        private void Start()
        {
            travel = FindFirstObjectByType<SceneTravel>();
            village = travel != null ? travel.Village : null;
            foreach (HireOption option in options)
                option.hireButton.onClick.AddListener(() => TryHire(option.unit));
            backButton.onClick.AddListener(Back);
            RefreshUI();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        public bool TryHire(UnitDefinition unit)
        {
            if (unit == null || unit.prefab == null || village == null) return false;
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

        private void OnDisable()
        {
            // 등장 도중 마을로 돌아가도 유닛이 작은 크기로 남지 않는다.
            foreach (Transform soldier in soldiersRoot) soldier.localScale = Vector3.one;
        }

        private void RefreshUI()
        {
            // 고정 문구는 Inspector에서 입력하고, 보유량 숫자만 갱신한다.
            riceAmount.text = village != null ? village.Rice.ToString() : "—";
            bool full = hiredCount >= spawnPoints.Length;
            formationFull.SetActive(full);
            foreach (HireOption option in options)
            {
                bool unlocked = village != null && village.HasBuilding(option.unit.requiredBuilding);
                bool affordable = village != null && village.Rice >= option.unit.riceCost;
                option.hireButton.interactable = unlocked && affordable && !full;
                // 문구는 Inspector에 둔다. 코드는 필요한 안내 오브젝트만 켜고 끈다.
                option.buildingRequired.SetActive(!unlocked);
                option.riceRequired.SetActive(unlocked && !affordable);
            }
        }

        private void Back()
        {
            if (travel != null) travel.ReturnToVillage();
            else SceneManager.LoadScene(villageScene);
        }
    }
}
