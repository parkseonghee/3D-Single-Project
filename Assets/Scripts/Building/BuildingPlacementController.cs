using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArmySurvivor.Building
{
    public partial class BuildingPlacementController : MonoBehaviour
    {
        [System.Serializable]
        private class StartingBuilding
        {
            public BuildingDefinition definition;
            public GameObject sceneObject;
        }

        private class BuildingState
        {
            public BuildingDefinition definition;
            public int level = 1;
            public bool isStarting;
        }

        [Header("시작 건물 · 씬에 미리 배치")]
        [SerializeField] private StartingBuilding[] startingBuildings = new StartingBuilding[0];

        [Header("씬 연결")]
        [SerializeField] private Camera placementCamera;
        [SerializeField] private Collider buildSurface;
        [SerializeField] private Transform buildingsRoot;
        [SerializeField] private BuildingManagementUI buildingMenu;

        [Header("입력")]
        [SerializeField] private InputActionReference pointer;
        [SerializeField] private InputActionReference confirm;
        [SerializeField] private InputActionReference cancel;

        [Header("배치 설정")]
        [SerializeField, Min(0.1f)] private float gridSize = 2;
        [SerializeField] private Material gridMaterial;
        [SerializeField, Min(1)] private float rayDistance = 500;
        [SerializeField] private Material validPreview;
        [SerializeField] private Material invalidPreview;
        [SerializeField, Min(0)] private int startingRice = 320;
        [Header("자원과 환전")]
        [SerializeField, Min(0)] private int startingGold;
        [SerializeField, Min(0)] private int startingSpecialResource;
        [SerializeField, Min(1)] private int exchangeRiceCost = 100;
        [SerializeField, Min(1)] private int exchangeGoldGain = 40;

        // 현재 상태는 공유 데이터 에셋 대신 이 컴포넌트에서 관리한다.
        private readonly Dictionary<BuildingDefinition, int> buildCounts = new Dictionary<BuildingDefinition, int>();
        private BuildingDefinition selectedBuilding;
        private GameObject preview;
        private Renderer[] previewRenderers;
        private Material currentPreviewMaterial;
        private int selectionFrame;
        private readonly HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        private GameObject gridLines;
        private readonly Dictionary<GameObject, BuildingState> placedBuildings = new Dictionary<GameObject, BuildingState>();
        private GameObject movingBuilding;
        private RectInt originalArea;

        public int Rice { get; private set; }
        public int Gold { get; private set; }
        public void AddStageRewards(int gold, int special)
        {
            Gold = (int)System.Math.Min(int.MaxValue, (long)Gold + Mathf.Max(0, gold));
            SpecialResource = (int)System.Math.Min(int.MaxValue, (long)SpecialResource + Mathf.Max(0, special));
        }
        public int SpecialResource { get; private set; }
        public bool CanExchange => exchangeRiceCost > 0 && exchangeGoldGain > 0 &&
            Rice >= exchangeRiceCost && Gold <= int.MaxValue - exchangeGoldGain;

        public bool TryExchangeRice()
        {
            if (!CanExchange) return false;
            Rice -= exchangeRiceCost;
            Gold += exchangeGoldGain;
            return true;
        }

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
            Gold = startingGold;
            SpecialResource = startingSpecialResource;
            foreach (StartingBuilding initial in startingBuildings)
            {
                if (initial.definition == null || initial.sceneObject == null) continue;
                Vector3 position = SnapPosition(initial.definition, initial.sceneObject.transform.position);
                initial.sceneObject.transform.position = position;
                Occupy(GetArea(initial.definition, position));
                placedBuildings[initial.sceneObject] = new BuildingState { definition = initial.definition, isStarting = true };
            }
            CreateGridLines();
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
            if (building == null) return false;
            foreach (var placed in placedBuildings)
            {
                if (placed.Key != null && placed.Key.activeSelf && placed.Value.definition == building)
                    return true;
            }
            return false;
        }

        public BuildingDefinition GetDefinition(GameObject target)
        {
            return target != null && placedBuildings.TryGetValue(target, out BuildingState state)
                ? state.definition : null;
        }

        public int GetLevel(GameObject target)
        {
            return target != null && placedBuildings.TryGetValue(target, out BuildingState state) ? state.level : 0;
        }

        public bool TryUpgrade(GameObject target)
        {
            if (movingBuilding != null || target == null || !placedBuildings.TryGetValue(target, out BuildingState state)) return false;
            if (state.level == int.MaxValue) return false;
            int cost = state.definition.UpgradeCostAt(state.level);
            if (Gold < cost) return false;
            Gold -= cost;
            state.level++;
            return true;
        }

        public bool TryDemolish(GameObject target)
        {
            if (target == null || !placedBuildings.TryGetValue(target, out BuildingState state)) return false;
            Cancel();
            foreach (Vector2Int cell in GetArea(state.definition, target.transform.position).allPositionsWithin)
                occupiedCells.Remove(cell);
            placedBuildings.Remove(target);
            Rice = (int)System.Math.Min((long)Rice + state.definition.DemolitionRefund, int.MaxValue);
            // Destroy는 프레임 끝에 처리되므로 즉시 숨겨 중복 클릭과 충돌을 막는다.
            target.SetActive(false);
            Destroy(target);
            return true;
        }

        public bool TrySpendRice(int cost)
        {
            if (cost < 0 || Rice < cost) return false;
            Rice -= cost;
            return true;
        }

        public bool TryAddRice(int amount)
        {
            if (amount < 0 || Rice > int.MaxValue - amount) return false;
            Rice += amount;
            return true;
        }

        public int GetDailyHarvestTotal()
        {
            long total = 0;
            foreach (var placed in placedBuildings)
            {
                if (placed.Key == null || !placed.Key.activeSelf) continue;
                BuildingState state = placed.Value;
                BuildingDefinition definition = state.definition;
                total += state.isStarting ? definition.initialDailyRice : definition.additionalDailyRice;
                total += (long)(state.level - 1) * definition.dailyRicePerUpgrade;
            }
            return (int)System.Math.Min(total, int.MaxValue);
        }

        public void Select(BuildingDefinition building)
        {
            Cancel();
            if (!enabled || building == null || !building.IsValid)
            {
                return;
            }

            selectedBuilding = building;
            gridLines.SetActive(true);
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
            if (selectedBuilding == null)
            {
                if (!overUi && confirmPressed) SelectPlacedBuilding(ray);
                return;
            }
            if (overUi || !buildSurface.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                preview.SetActive(false);
                return;
            }

            Vector3 position = SnapPosition(selectedBuilding, hit.point);

            bool canBuild = CanPlace(selectedBuilding, position);
            preview.SetActive(true);
            preview.transform.position = position + selectedBuilding.modelOffset;
            SetPreviewMaterial(canBuild ? validPreview : invalidPreview);

            // 메뉴 선택에 사용한 클릭으로 바로 건설되는 것을 막는다.
            if (confirmPressed && canBuild && Time.frameCount > selectionFrame)
            {
                if (movingBuilding != null) TryMove(position);
                else TryBuild(selectedBuilding, position);
            }
        }

        private void SelectPlacedBuilding(Ray ray)
        {
            if (buildingMenu != null) buildingMenu.Close();
            Physics.SyncTransforms();
            if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance)) return;
            Transform target = hit.collider.transform;
            while (target != null && target != buildingsRoot)
            {
                if (placedBuildings.ContainsKey(target.gameObject))
                {
                    if (buildingMenu != null) buildingMenu.Open(target.gameObject);
                    return;
                }
                target = target.parent;
            }
        }

        public void BeginMove(GameObject target)
        {
            BuildingDefinition definition = GetDefinition(target);
            if (definition == null) return;
            Select(definition);
            if (selectedBuilding == null) return;
            movingBuilding = target;
            originalArea = GetArea(definition, target.transform.position);
            // 원본 위치와 점유 칸은 확정 전까지 보존하고 모델만 숨긴다.
            target.transform.GetChild(0).gameObject.SetActive(false);
        }

        public bool TryMove(Vector3 position)
        {
            if (movingBuilding == null || !CanPlace(selectedBuilding, position)) return false;
            position = SnapPosition(selectedBuilding, position);
            foreach (Vector2Int cell in originalArea.allPositionsWithin) occupiedCells.Remove(cell);
            movingBuilding.transform.position = position;
            Occupy(GetArea(selectedBuilding, position));
            Cancel();
            return true;
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

        // 바닥의 왼쪽 아래를 기준으로 건물 전체가 정수 개의 칸을 차지한다.
        private RectInt GetArea(BuildingDefinition building, Vector3 position)
        {
            Bounds ground = buildSurface.bounds;
            int width = Mathf.CeilToInt(building.footprint.x / gridSize);
            int depth = Mathf.CeilToInt(building.footprint.z / gridSize);
            int x = Mathf.RoundToInt((position.x - ground.min.x) / gridSize - width * 0.5f);
            int z = Mathf.RoundToInt((position.z - ground.min.z) / gridSize - depth * 0.5f);
            return new RectInt(x, z, width, depth);
        }

        public Vector3 SnapPosition(BuildingDefinition building, Vector3 position)
        {
            RectInt area = GetArea(building, position);
            Bounds ground = buildSurface.bounds;
            return new Vector3(ground.min.x + area.center.x * gridSize,
                ground.max.y, ground.min.z + area.center.y * gridSize);
        }

        public bool CanPlace(BuildingDefinition building, Vector3 position)
        {
            if (building == null || !building.IsValid) return false;
            if (movingBuilding == null && Rice < GetCost(building)) return false;
            RectInt area = GetArea(building, position);
            Bounds ground = buildSurface.bounds;
            if (area.xMin < 0 || area.yMin < 0 ||
                area.xMax > Mathf.FloorToInt(ground.size.x / gridSize) ||
                area.yMax > Mathf.FloorToInt(ground.size.z / gridSize)) return false;

            foreach (Vector2Int cell in area.allPositionsWithin)
                if (occupiedCells.Contains(cell) &&
                    !(movingBuilding != null && originalArea.Contains(cell))) return false;
            return true;
        }

        private void Occupy(RectInt area)
        {
            foreach (Vector2Int cell in area.allPositionsWithin) occupiedCells.Add(cell);
        }

        private void CreateGridLines()
        {
            gridLines = new GameObject("Placement Grid");
            gridLines.transform.SetParent(transform, false);
            Bounds ground = buildSurface.bounds;
            int columns = Mathf.FloorToInt(ground.size.x / gridSize);
            int rows = Mathf.FloorToInt(ground.size.z / gridSize);
            Vector3 origin = new Vector3(ground.min.x, ground.max.y + 0.03f, ground.min.z);
            for (int x = 0; x <= columns; x++)
                AddGridLine(origin + Vector3.right * x * gridSize,
                    origin + Vector3.right * x * gridSize + Vector3.forward * rows * gridSize);
            for (int z = 0; z <= rows; z++)
                AddGridLine(origin + Vector3.forward * z * gridSize,
                    origin + Vector3.forward * z * gridSize + Vector3.right * columns * gridSize);
            gridLines.SetActive(false);
        }

        private void AddGridLine(Vector3 from, Vector3 to)
        {
            var line = new GameObject("Line").AddComponent<LineRenderer>();
            line.transform.SetParent(gridLines.transform, false);
            line.sharedMaterial = gridMaterial;
            line.startWidth = line.endWidth = 0.025f;
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
        public bool TryBuild(BuildingDefinition building, Vector3 position)
        {
            if (movingBuilding != null || !CanPlace(building, position))
            {
                return false;
            }

            position = SnapPosition(building, position);
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

            Occupy(GetArea(building, position));
            placedBuildings[root] = new BuildingState { definition = building };
            Rice -= cost;
            buildCounts.TryGetValue(building, out int count);
            buildCounts[building] = count + 1;
            Cancel();
            return true;
        }

        public void Cancel()
        {
            if (buildingMenu != null) buildingMenu.Close();
            if (movingBuilding != null)
                movingBuilding.transform.GetChild(0).gameObject.SetActive(true);
            movingBuilding = null;
            if (preview != null)
            {
                Destroy(preview);
            }
            preview = null;
            selectedBuilding = null;
            if (gridLines != null) gridLines.SetActive(false);
        }
    }
}
