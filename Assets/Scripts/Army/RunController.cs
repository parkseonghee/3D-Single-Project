using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class RunController : MonoBehaviour
    {
        [Serializable]
        public class Tactic
        {
            public Button button;
            public GameObject selected;
            public Key key;
            [Min(1)] public float radius = 3;
            [Min(0)] public float moveMultiplier = 1;
            [Min(0)] public float damageMultiplier = 1;
            [Min(0)] public float receivedDamageMultiplier = 1;
            public bool soldiersCollectExperience;
        }

        [Header("씬 연결")]
        [SerializeField] private RecruitmentController recruitment;
        [SerializeField] private Transform commander;
        [SerializeField] private Camera playCamera;
        [SerializeField] private Collider ground;
        [SerializeField] private Button startButton;
        [SerializeField] private Button prepareButton;
        [SerializeField] private GameObject[] preparationUI;
        [SerializeField] private GameObject runUI;
        [SerializeField] private TMP_Text elapsedText;
        [SerializeField] private LineRenderer formationRing;

        [Header("이동과 전술")]
        [SerializeField, Min(0.1f)] private float moveSpeed = 5;
        [SerializeField, Min(0.1f)] private float followSpeed = 8;
        [SerializeField] private Tactic[] tactics;
        [SerializeField] private Vector3 cameraOffset = new Vector3(14, 18, -14);
        [SerializeField, Min(1)] private float cameraSize = 11;

        private readonly List<Transform> soldiers = new List<Transform>();
        private readonly Dictionary<Transform, Animator[]> animators = new Dictionary<Transform, Animator[]>();
        private readonly Dictionary<Transform, Quaternion> facingCorrections = new Dictionary<Transform, Quaternion>();
        private readonly Dictionary<Transform, Vector3> moveDirections = new Dictionary<Transform, Vector3>();
        private readonly List<Vector3> preparationPositions = new List<Vector3>();
        private readonly List<Quaternion> preparationRotations = new List<Quaternion>();
        private Vector3 commanderPosition, cameraPosition;
        private Quaternion commanderRotation, cameraRotation;
        private bool cameraOrthographic;
        private float preparationCameraSize;
        private int shownSecond = -1;
        private static readonly int Moving = Animator.StringToHash("Moving");

        public bool IsRunning { get; private set; }
        public bool CanStart { get; set; } = true;
        public Transform Commander => commander;
        public Collider Ground => ground;
        public event Action RunStarted;
        public event Action RunEnded;
        public float Elapsed { get; private set; }
        public int TacticIndex { get; private set; }
        public Tactic CurrentTactic => tactics[TacticIndex];

        private void Start()
        {
            startButton.onClick.AddListener(BeginRun);
            prepareButton.onClick.AddListener(ReturnToPreparation);
            for (int i = 0; i < tactics.Length; i++)
            {
                int index = i;
                tactics[i].button.onClick.AddListener(() => SelectTactic(index));
            }
            runUI.SetActive(false);
            formationRing.gameObject.SetActive(false);
        }

        public void BeginRun()
        {
            if (IsRunning || !CanStart) return;
            recruitment.FinishAppearances();
            soldiers.Clear();
            animators.Clear();
            facingCorrections.Clear();
            moveDirections.Clear();
            preparationPositions.Clear();
            preparationRotations.Clear();
            foreach (Transform soldier in recruitment.SoldiersRoot)
            {
                soldiers.Add(soldier);
                preparationPositions.Add(soldier.position);
                preparationRotations.Add(soldier.rotation);
                RegisterModel(soldier);
            }
            RegisterModel(commander);
            commanderPosition = commander.position;
            commanderRotation = commander.rotation;
            cameraPosition = playCamera.transform.position;
            cameraRotation = playCamera.transform.rotation;
            cameraOrthographic = playCamera.orthographic;
            preparationCameraSize = playCamera.orthographicSize;
            recruitment.IsPreparing = false;
            IsRunning = true;
            Elapsed = 0;
            shownSecond = -1;
            foreach (GameObject ui in preparationUI) ui.SetActive(false);
            runUI.SetActive(true);
            formationRing.gameObject.SetActive(true);
            playCamera.orthographic = true;
            playCamera.orthographicSize = cameraSize;
            SelectTactic(0);
            FollowCamera();
            RunStarted?.Invoke();
        }

        public void SelectTactic(int index)
        {
            if (!IsRunning || index < 0 || index >= tactics.Length) return;
            TacticIndex = index;
            for (int i = 0; i < tactics.Length; i++) tactics[i].selected.SetActive(i == index);
            // 진형 반경은 지휘관과 독립된 월드 방향을 사용한다. 방향 전환 때 병력이 빙돌지 않는다.
            for (int i = 0; i < formationRing.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / formationRing.positionCount;
                formationRing.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * CurrentTactic.radius);
            }
        }

        private void Update()
        {
            startButton.interactable = CanStart;
            if (!IsRunning) return;
            Vector2 input = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < tactics.Length; i++)
                    if (keyboard[tactics[i].key].wasPressedThisFrame) SelectTactic(i);
                input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                        - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                        - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            Tick(input, Time.deltaTime);
        }

        public void Tick(Vector2 input, float deltaTime)
        {
            if (!IsRunning || deltaTime <= 0) return;
            Elapsed += deltaTime;
            int second = Mathf.FloorToInt(Elapsed);
            if (shownSecond != second)
            {
                shownSecond = second;
                elapsedText.text = $"{second / 60:00}:{second % 60:00}";
            }
            Vector3 forward = Vector3.ProjectOnPlane(playCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 direction = right * input.x + forward * input.y;
            Vector3 target = commander.position + direction * (moveSpeed * CurrentTactic.moveMultiplier * deltaTime);
            // 가장 넓은 진형도 땅 밖으로 나가지 않도록 지휘관의 이동 범위를 제한한다.
            float margin = 1;
            foreach (Tactic tactic in tactics) margin = Mathf.Max(margin, tactic.radius + 1);
            Bounds bounds = ground.bounds;
            target.x = Mathf.Clamp(target.x, bounds.min.x + margin, bounds.max.x - margin);
            target.z = Mathf.Clamp(target.z, bounds.min.z + margin, bounds.max.z - margin);
            Move(commander, target, deltaTime);
            for (int i = 0; i < soldiers.Count; i++)
            {
                if (soldiers[i] == null) continue;
                float angle = i * Mathf.PI * 2 / soldiers.Count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * CurrentTactic.radius;
                Vector3 destination = commander.position + offset;
                Vector3 next = Vector3.MoveTowards(soldiers[i].position, destination,
                    followSpeed * CurrentTactic.moveMultiplier * deltaTime);
                Move(soldiers[i], next, deltaTime);
            }
            formationRing.transform.position = commander.position + Vector3.up * 0.04f;
        }

        private void RegisterModel(Transform unit)
        {
            Animator[] models = unit.GetComponentsInChildren<Animator>();
            animators.Add(unit, models);
            // 준비 화면용 모델 회전은 유지하고, 이동할 때 루트 회전으로 보정한다.
            Vector3 forward = models.Length > 0 ? models[0].transform.forward : unit.forward;
            forward = Vector3.ProjectOnPlane(unit.InverseTransformDirection(forward), Vector3.up);
            moveDirections[unit] = unit.TransformDirection(forward).normalized;
            facingCorrections.Add(unit, forward.sqrMagnitude > 0.0001f
                ? Quaternion.Inverse(Quaternion.LookRotation(forward)) : Quaternion.identity);
        }

        private void Move(Transform unit, Vector3 target, float deltaTime)
        {
            Vector3 direction = target - unit.position;
            bool moving = direction.sqrMagnitude > 0.00001f;
            if (moving) moveDirections[unit] = direction.normalized;
            unit.position = target;
            if (moving) unit.rotation = Quaternion.RotateTowards(unit.rotation,
                Quaternion.LookRotation(direction) * facingCorrections[unit], 540 * deltaTime);
            foreach (Animator animator in animators[unit]) animator.SetBool(Moving, moving);
        }

        public void FaceTarget(Transform unit, Vector3 target)
        {
            Vector3 direction = target - unit.position;
            direction.y = 0;
            if (direction.sqrMagnitude > Mathf.Epsilon && facingCorrections.ContainsKey(unit))
                unit.rotation = Quaternion.LookRotation(direction) * facingCorrections[unit];
        }

        // 정지 중에는 마지막 이동 방향으로 창을 찌른다.
        public Vector3 MoveDirection(Transform unit) => moveDirections[unit];

        private void LateUpdate()
        {
            if (IsRunning) FollowCamera();
        }

        private void FollowCamera()
        {
            Vector3 target = commander.position + Vector3.up;
            playCamera.transform.position = target + cameraOffset;
            playCamera.transform.LookAt(target);
        }

        public void ReturnToPreparation()
        {
            if (!IsRunning) return;
            IsRunning = false;
            RunEnded?.Invoke();
            recruitment.IsPreparing = true;
            commander.SetPositionAndRotation(commanderPosition, commanderRotation);
            for (int i = 0; i < soldiers.Count; i++)
                if (soldiers[i] != null) soldiers[i].SetPositionAndRotation(preparationPositions[i], preparationRotations[i]);
            foreach (var group in animators.Values)
                foreach (Animator animator in group) if (animator != null) animator.SetBool(Moving, false);
            playCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            playCamera.orthographic = cameraOrthographic;
            playCamera.orthographicSize = preparationCameraSize;
            runUI.SetActive(false);
            formationRing.gameObject.SetActive(false);
            foreach (GameObject ui in preparationUI) ui.SetActive(true);
        }
    }
}
