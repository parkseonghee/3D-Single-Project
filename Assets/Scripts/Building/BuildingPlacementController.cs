using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArmySurvivor.Building
{
    public class BuildingPlacementController : MonoBehaviour
    {
        [Header("씬 연결")]
        [SerializeField] private Camera placementCamera;
        [SerializeField] private Collider buildSurface;
        [SerializeField] private Transform buildingsRoot;

        [Header("입력")]
        [SerializeField] private InputActionReference pointer;
        [SerializeField] private InputActionReference confirm;
        [SerializeField] private InputActionReference cancel;

        [Header("배치 설정")]
        [SerializeField, Min(0)] private float gridSize = 1;
        [SerializeField, Min(1)] private float rayDistance = 500;
        [SerializeField, Min(0.01f)] private float surfaceTolerance = 0.1f;
        [SerializeField] private LayerMask obstacleMask = ~0;
        [SerializeField] private Material validPreview;
        [SerializeField] private Material invalidPreview;
        [SerializeField, Min(0)] private int startingRice = 320;

        // 현재 상태는 공유 데이터 에셋 대신 이 컴포넌트에서 관리한다.
        private readonly Dictionary<BuildingDefinition, int> buildCounts = new Dictionary<BuildingDefinition, int>();
        private BuildingDefinition selectedBuilding;
        private GameObject preview;
        private Renderer[] previewRenderers;
        private Material currentPreviewMaterial;
        private int selectionFrame;

        public int Rice { get; private set; }

        private void Awake()
        {
            if (placementCamera == null || buildSurface == null || buildingsRoot == null ||
                pointer == null || confirm == null || cancel == null ||
                validPreview == null || invalidPreview == null)
            {
                Debug.LogError("건설 시스템의 Inspector 연결을 확인해주세요.", this);
                enabled = false;
                return;
            }
            Rice = startingRice;
        }

        private void OnEnable()
        {
            pointer.action.Enable();
            confirm.action.Enable();
            cancel.action.Enable();
        }

        private void OnDisable()
        {
            if (pointer != null) pointer.action.Disable();
            if (confirm != null) confirm.action.Disable();
            if (cancel != null) cancel.action.Disable();
            Cancel();
        }

        public int GetCost(BuildingDefinition building)
        {
            buildCounts.TryGetValue(building, out int count);
            return building.CostAt(count);
        }

        public bool HasBuilding(BuildingDefinition building)
        {
            return building != null && buildCounts.TryGetValue(building, out int count) && count > 0;
        }

        public bool TrySpendRice(int cost)
        {
            if (cost < 0 || Rice < cost) return false;
            Rice -= cost;
            return true;
        }

        public void Select(BuildingDefinition building)
        {
            Cancel();
            if (!enabled || building == null || !building.IsValid)
            {
                return;
            }

            selectedBuilding = building;
            selectionFrame = Time.frameCount;
            preview = Instantiate(building.prefab);
            preview.name = "Placement Preview";
            preview.transform.rotation = Quaternion.Euler(building.modelRotation);
            preview.transform.localScale = building.modelScale;

            // 미리보기 자신이 장애물로 인식되거나 물리 작용을 받지 않게 한다.
            foreach (Collider collider in preview.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            foreach (Rigidbody body in preview.GetComponentsInChildren<Rigidbody>())
            {
                body.isKinematic = true;
                body.detectCollisions = false;
            }

            previewRenderers = preview.GetComponentsInChildren<Renderer>();
            currentPreviewMaterial = null;
            preview.SetActive(false);
        }

        private void Update()
        {
            if (selectedBuilding == null)
            {
                return;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            UpdatePlacement(pointer.action.ReadValue<Vector2>(), confirm.action.WasPressedThisFrame(),
                cancel.action.WasPressedThisFrame(), overUi);
        }

        // 위치 결정 → 배치 검사 → 미리보기 표시 → 클릭 확정 순서로 읽으면 된다.
        private void UpdatePlacement(Vector2 screenPosition, bool confirmPressed, bool cancelPressed, bool overUi)
        {
            if (cancelPressed)
            {
                Cancel();
                return;
            }

            Ray ray = placementCamera.ScreenPointToRay(screenPosition);
            if (overUi || !buildSurface.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                preview.SetActive(false);
                return;
            }

            Vector3 position = hit.point;
            if (gridSize > 0)
            {
                position.x = Mathf.Round(position.x / gridSize) * gridSize;
                position.z = Mathf.Round(position.z / gridSize) * gridSize;
            }

            bool canBuild = CanPlace(selectedBuilding, position);
            preview.SetActive(true);
            preview.transform.position = position + selectedBuilding.modelOffset;
            SetPreviewMaterial(canBuild ? validPreview : invalidPreview);

            // 메뉴 선택에 사용한 클릭으로 바로 건설되는 것을 막는다.
            if (confirmPressed && canBuild && Time.frameCount > selectionFrame)
            {
                TryBuild(selectedBuilding, position);
            }
        }

        private void SetPreviewMaterial(Material material)
        {
            if (currentPreviewMaterial == material)
            {
                return;
            }

            foreach (Renderer renderer in previewRenderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
            currentPreviewMaterial = material;
        }

        public bool CanPlace(BuildingDefinition building, Vector3 position)
        {
            if (building == null || !building.IsValid) return false;
            if (Rice < GetCost(building)) return false;
            if (!HasGround(building, position)) return false;
            if (HasObstacle(building, position)) return false;
            return true;
        }

        private bool HasGround(BuildingDefinition building, Vector3 position)
        {
            // 중심, 모서리, 변의 중간까지 9곳의 바닥 높이를 확인한다.
            Vector3 halfSize = building.footprint * 0.5f;
            float rayHeight = surfaceTolerance * 2;

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 point = position + new Vector3(x * halfSize.x, rayHeight, z * halfSize.z);
                    Ray ray = new Ray(point, Vector3.down);
                    if (!buildSurface.Raycast(ray, out RaycastHit hit, rayHeight * 2)) return false;
                    if (Mathf.Abs(hit.point.y - position.y) > surfaceTolerance) return false;
                }
            }
            return true;
        }

        private bool HasObstacle(BuildingDefinition building, Vector3 position)
        {
            Physics.SyncTransforms();
            Vector3 halfSize = building.footprint * 0.5f;
            Vector3 center = position + Vector3.up * halfSize.y;
            Collider[] obstacles = Physics.OverlapBox(center, halfSize * 0.99f,
                Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);

            foreach (Collider obstacle in obstacles)
            {
                if (obstacle == buildSurface) continue;
                if (preview != null && obstacle.transform.IsChildOf(preview.transform)) continue;
                return true;
            }
            return false;
        }

        public bool TryBuild(BuildingDefinition building, Vector3 position)
        {
            if (!CanPlace(building, position))
            {
                return false;
            }

            int cost = GetCost(building);
            GameObject root = new GameObject(building.displayName);
            root.transform.SetParent(buildingsRoot, false);
            root.transform.position = position;

            GameObject model = Instantiate(building.prefab, root.transform);
            model.transform.localPosition = building.modelOffset;
            model.transform.localRotation = Quaternion.Euler(building.modelRotation);
            model.transform.localScale = building.modelScale;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = building.footprint;
            collider.center = Vector3.up * building.footprint.y * 0.5f;

            Rice -= cost;
            buildCounts.TryGetValue(building, out int count);
            buildCounts[building] = count + 1;
            Cancel();
            return true;
        }

        public void Cancel()
        {
            if (preview != null)
            {
                Destroy(preview);
            }
            preview = null;
            selectedBuilding = null;
        }
    }
}
