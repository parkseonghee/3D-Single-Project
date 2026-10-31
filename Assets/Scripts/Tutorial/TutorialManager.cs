using System;
using System.Collections.Generic;
using ArmySurvivor.Army;
using ArmySurvivor.Building;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.Tutorial
{
    // One instance per scene. A static session lets the village tutorial continue in the additive battle scene.
    public sealed class TutorialManager : MonoBehaviour
    {
        [SerializeField] private TutorialSequence[] sequences;
        [SerializeField] private TutorialSequence debugSequence;
        [SerializeField] private TutorialStyle style;
        [SerializeField] private string sceneEntryEvent;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private RunController run;
        [SerializeField] private CombatController combat;
        [SerializeField] private bool debugKeys = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField] private Rect debugWindow = new Rect(20, 20, 390, 270);
        [SerializeField, Min(50)] private float debugReplayButtonWidth = 70;
        private bool showDebugWindow;
#endif

        private static TutorialSequence activeSequence;
        private struct SuspendedTutorial
        {
            public TutorialSequence sequence;
            public int step;
            public Transform target;
            public bool nextAvailable;
        }
        private static readonly Stack<SuspendedTutorial> suspended = new Stack<SuspendedTutorial>();
        private static int activeStep;
        private static Transform eventTarget;
        private static bool forceNextAvailable;
        private Canvas canvas;
        private RectTransform canvasRect;
        private TutorialSpotlight spotlight;
        private RectTransform bubble;
        private CanvasGroup bubbleGroup;
        private TMP_Text titleText, bodyText, progressText;
        private Button skipButton;
        private RectTransform arrow;
        private GameObject confirmation;
        private float stepElapsed;
        private int stepEventCount;
        private int stepStartedFrame;
        private Vector3 commanderStart;
        private bool startedStep;
        private bool revealRequested;
        private bool startedOnce;
        private bool ownedPause;
        private float previousTimeScale;
        private TutorialTarget resolvedTarget;
        private readonly Vector3[] corners = new Vector3[4];
        public static bool KeyboardBlocked { get; private set; }
        public static bool MovementWhilePaused { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeSequence = null;
            suspended.Clear();
            activeStep = 0;
            eventTarget = null;
            forceNextAvailable = false;
            KeyboardBlocked = false;
            MovementWhilePaused = false;
        }

        private void Awake()
        {
            if (style == null) { enabled = false; Debug.LogWarning("TutorialManager: TutorialStyle 누락", this); return; }
            if (run == null) run = FindFirstObjectByType<RunController>();
            if (combat == null) combat = FindFirstObjectByType<CombatController>();
            BuildOverlay();
            HideOverlay();
        }

        private void OnEnable()
        {
            TutorialEvents.Raised += OnEvent;
            TutorialTarget.Clicked += OnTargetClicked;
            if (startedOnce && !string.IsNullOrEmpty(sceneEntryEvent)) TutorialEvents.Raise(sceneEntryEvent);
        }

        private void Start()
        {
            startedOnce = true;
            if (!string.IsNullOrEmpty(sceneEntryEvent)) TutorialEvents.Raise(sceneEntryEvent);
        }

        private void OnDisable()
        {
            TutorialEvents.Raised -= OnEvent;
            TutorialTarget.Clicked -= OnTargetClicked;
            RestoreModifiers();
            startedStep = false;
            HideOverlay();
        }

        private void OnDestroy()
        {
            if (activeSequence != null) Stop(false);
        }

        private void Update()
        {
            if (debugKeys && Keyboard.current != null)
            {
                if (Keyboard.current.f5Key.wasPressedThisFrame && debugSequence != null) ForceStart(debugSequence);
                if (Keyboard.current.f6Key.wasPressedThisFrame && activeSequence != null) Advance();
                if (Keyboard.current.f7Key.wasPressedThisFrame) { TutorialProgressStore.Clear(); Debug.Log("튜토리얼 완료 기록 초기화"); }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (Keyboard.current.f8Key.wasPressedThisFrame) showDebugWindow = !showDebugWindow;
#endif
            }
            if (activeSequence == null || activeSequence.steps == null || activeStep >= activeSequence.steps.Length)
            {
                HideOverlay(); return;
            }
            if (run == null) run = FindFirstObjectByType<RunController>();
            if (combat == null) combat = FindFirstObjectByType<CombatController>();
            if (activeSequence.cancelOnCommanderDeath && run != null && run.CommanderDead)
            { Stop(false); return; }
            if (run != null && run.IsChoosingUpgrade) { HideOverlay(); return; }
            TutorialStep step = activeSequence.steps[activeStep];
            if (!startedStep)
            {
                if (step.revealBuilding != null && !revealRequested)
                {
                    BuildingPalette palette = FindFirstObjectByType<BuildingPalette>();
                    revealRequested = palette != null && palette.RevealBuilding(step.revealBuilding);
                }
                if (!Ready(step)) { HideOverlay(); return; }
                BeginStep(step);
            }
            if (step.hideWhenTargetMissing && !string.IsNullOrEmpty(step.targetId) &&
                TutorialTarget.Find(step.targetId, eventTarget) == null)
            { HideOverlay(); return; }
            spotlight.gameObject.SetActive(true);
            bubble.gameObject.SetActive(true);
            skipButton.gameObject.SetActive(true);
            stepElapsed += Time.unscaledDeltaTime;
            UpdateVisuals(step);
            bool clickToAdvance = step.advance == TutorialAdvance.NextButton ||
                (step.showNextButtonAfterJump && forceNextAvailable) ||
                (step.showNextWhenNoFocus && combat != null && !combat.HasFocusCommand);
            if (clickToAdvance && Time.frameCount > stepStartedFrame && Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame &&
                (confirmation == null || !confirmation.activeSelf) &&
                !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)skipButton.transform,
                    Mouse.current.position.ReadValue()))
            { Advance(); return; }
            if (step.advance == TutorialAdvance.Seconds && stepElapsed >= step.seconds) Advance();
            else if (step.advance == TutorialAdvance.CommanderDistance && run != null &&
                Vector3.Distance(run.Commander.position, commanderStart) >= step.commanderDistance) Advance();
            else if (step.fallbackSeconds > 0 && stepElapsed >= step.fallbackSeconds) Advance();
        }

        private bool Ready(TutorialStep step)
        {
            if (activeStep == 0 && activeSequence.startDistanceFromCommander > 0 &&
                eventTarget != null && run != null &&
                Vector3.Distance(eventTarget.position, run.Commander.position) >
                activeSequence.startDistanceFromCommander) return false;
            if (string.IsNullOrEmpty(step.targetId)) return true;
            resolvedTarget = TutorialTarget.Find(step.targetId, eventTarget);
            return resolvedTarget != null && resolvedTarget.gameObject.activeInHierarchy;
        }

        private void BeginStep(TutorialStep step)
        {
            startedStep = true;
            stepElapsed = 0;
            stepEventCount = 0;
            stepStartedFrame = Time.frameCount;
            if (run != null) commanderStart = run.Commander.position;
            if (step.pauseGame && Time.timeScale > 0)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0;
                ownedPause = true;
            }
            KeyboardBlocked = !step.allowKeyboard;
            MovementWhilePaused = step.pauseGame && step.allowCommanderMovementWhilePaused;
            if (combat != null) combat.TutorialEnemySpeedMultiplier = step.enemyMovementMultiplier;
            spotlight.gameObject.SetActive(true);
            bubble.gameObject.SetActive(true);
            skipButton.gameObject.SetActive(true);
            titleText.text = step.title;
            bodyText.text = step.body;
            SetBubbleLayout(step.showAsTopBanner);
            progressText.text = step.advance == TutorialAdvance.Event && step.requiredEventCount > 1
                ? $"{activeStep + 1}/{activeSequence.steps.Length} · 0/{step.requiredEventCount}"
                : $"{activeStep + 1}/{activeSequence.steps.Length}";
            spotlight.BlockOutside = step.blockOutsideClicks;
            spotlight.BlockHole = step.advance == TutorialAdvance.NextButton;
            confirmation.SetActive(false);
        }

        private void UpdateVisuals(TutorialStep step)
        {
            if (!string.IsNullOrEmpty(step.targetId)) resolvedTarget = TutorialTarget.Find(step.targetId, eventTarget);
            Rect hole = TargetRect(step);
            spotlight.Draw(hole, step.holeShape, step.cornerRadius, style);
            Vector2 placement = step.showAsTopBanner ? TopBannerPosition() : BubblePosition(step, hole);
            bubble.anchoredPosition = placement;
            bubbleGroup.alpha = style.appearSeconds <= 0 ? 1 : Mathf.Clamp01(stepElapsed / style.appearSeconds);
            bool showArrow = style.showArrow && step.showArrow && hole.width > 0;
            arrow.gameObject.SetActive(showArrow);
            if (showArrow)
            {
                arrow.anchoredPosition = new Vector2(hole.center.x,
                    hole.yMax + style.bubbleGap + Mathf.Sin(Time.unscaledTime * style.arrowSpeed) * style.arrowBob);
            }
        }

        private Rect TargetRect(TutorialStep step)
        {
            if (resolvedTarget == null || canvasRect == null) return Rect.zero;
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            if (resolvedTarget.transform is RectTransform rectTarget)
            {
                rectTarget.GetWorldCorners(corners);
                Canvas targetCanvas = rectTarget.GetComponentInParent<Canvas>();
                Camera targetCamera = targetCanvas != null && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? targetCanvas.worldCamera : null;
                for (int i = 0; i < corners.Length; i++)
                    Include(RectTransformUtility.WorldToScreenPoint(targetCamera, corners[i]), camera, ref min, ref max);
            }
            else
            {
                Camera view = worldCamera != null ? worldCamera : Camera.main;
                if (view == null) return Rect.zero;
                Renderer[] renderers = resolvedTarget.GetComponentsInChildren<Renderer>();
                Bounds bounds = new Bounds(resolvedTarget.transform.position, Vector3.one);
                if (renderers.Length > 0)
                {
                    bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                }
                Vector3 ext = bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 world = bounds.center + Vector3.Scale(ext, new Vector3((i & 1) == 0 ? -1 : 1,
                        (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 screen = view.WorldToScreenPoint(world);
                    if (screen.z > 0) Include(screen, camera, ref min, ref max);
                }
            }
            if (float.IsInfinity(min.x)) return Rect.zero;
            min -= step.padding;
            max += step.padding;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Include(Vector2 screen, Camera camera, ref Vector2 min, ref Vector2 max)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, camera, out Vector2 local)) return;
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        private Vector2 BubblePosition(TutorialStep step, Rect hole)
        {
            Rect canvasBounds = canvasRect.rect;
            Vector2 half = style.bubbleSize * 0.5f;
            float gap = style.bubbleGap;
            TutorialBubblePlacement placement = step.bubblePlacement;
            if (placement == TutorialBubblePlacement.Auto)
                placement = hole.width <= 0 ? TutorialBubblePlacement.Center :
                    hole.center.x > canvasBounds.center.x && hole.xMin - canvasBounds.xMin >= half.x * 2 + gap
                        ? TutorialBubblePlacement.Left :
                    hole.center.x < canvasBounds.center.x && canvasBounds.xMax - hole.xMax >= half.x * 2 + gap
                        ? TutorialBubblePlacement.Right :
                    hole.yMin - canvasBounds.yMin > half.y * 2 + gap ? TutorialBubblePlacement.Below :
                    hole.yMax + half.y * 2 + gap < canvasBounds.yMax ? TutorialBubblePlacement.Above :
                    hole.center.x < canvasBounds.center.x ? TutorialBubblePlacement.Right : TutorialBubblePlacement.Left;
            Vector2 point = hole.center;
            switch (placement)
            {
                case TutorialBubblePlacement.Above: point.y = hole.yMax + gap + half.y; break;
                case TutorialBubblePlacement.Below: point.y = hole.yMin - gap - half.y; break;
                case TutorialBubblePlacement.Left: point.x = hole.xMin - gap - half.x; break;
                case TutorialBubblePlacement.Right: point.x = hole.xMax + gap + half.x; break;
                case TutorialBubblePlacement.Center: point = Vector2.zero; break;
            }
            if (hole.width <= 0 && placement == TutorialBubblePlacement.Below)
                point.y = canvasBounds.yMin + half.y + style.screenMargin;
            point.x = Mathf.Clamp(point.x, canvasBounds.xMin + half.x + style.screenMargin,
                canvasBounds.xMax - half.x - style.screenMargin);
            point.y = Mathf.Clamp(point.y, canvasBounds.yMin + half.y + style.screenMargin,
                canvasBounds.yMax - half.y - style.screenMargin);
            return point;
        }

        private Vector2 TopBannerPosition()
        {
            Rect bounds = canvasRect.rect;
            RectTransform panel = null;
            if (resolvedTarget != null)
            {
                ScrollRect scroll = resolvedTarget.GetComponentInParent<ScrollRect>();
                if (scroll != null) panel = scroll.transform as RectTransform;
            }
            if (panel != null)
            {
                panel.GetWorldCorners(corners);
                Canvas panelCanvas = panel.GetComponentInParent<Canvas>();
                Camera panelCamera = panelCanvas != null && panelCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? panelCanvas.worldCamera : null;
                Vector2 panelTopRight = RectTransformUtility.WorldToScreenPoint(panelCamera, corners[2]);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, panelTopRight, null,
                    out Vector2 localTopRight))
                    return new Vector2(localTopRight.x - bubble.rect.width * 0.5f - 160f,
                        localTopRight.y - bubble.rect.height * 0.5f - 6f);
            }
            return new Vector2(bounds.xMax - bubble.rect.width * 0.5f - style.screenMargin - 160f,
                bounds.yMax - bubble.rect.height * 0.5f - style.screenMargin);
        }

        private void SetBubbleLayout(bool topBanner)
        {
            bubble.sizeDelta = topBanner ? new Vector2(
                Mathf.Min(style.topBannerSize.x, canvasRect.rect.width * 0.48f), style.topBannerSize.y)
                : style.bubbleSize;
            RectTransform title = (RectTransform)titleText.transform;
            RectTransform body = (RectTransform)bodyText.transform;
            if (topBanner)
            {
                title.offsetMin = new Vector2(14, -34);
                title.offsetMax = new Vector2(-14, -4);
                body.offsetMin = new Vector2(14, -style.topBannerSize.y + 5);
                body.offsetMax = new Vector2(-14, -36);
                titleText.fontSize = style.topBannerTitleSize;
                bodyText.fontSize = style.topBannerBodySize;
                progressText.gameObject.SetActive(false);
            }
            else
            {
                title.offsetMin = Min(style.titleOffsets);
                title.offsetMax = Max(style.titleOffsets);
                body.offsetMin = Min(style.bodyOffsets);
                body.offsetMax = Max(style.bodyOffsets);
                titleText.fontSize = style.titleSize;
                bodyText.fontSize = style.bodySize;
                progressText.gameObject.SetActive(true);
            }
        }

        private void OnEvent(string key, Transform target)
        {
            if (activeSequence != null)
            {
                if (key == "BattleEnded") suspended.Clear();
                foreach (TutorialSequence candidate in sequences)
                {
                    if (candidate == null || candidate == activeSequence || !candidate.interruptCurrent ||
                        candidate.startEventKey != key || TutorialProgressStore.IsCompleted(candidate.tutorialId)) continue;
                    suspended.Push(new SuspendedTutorial { sequence = activeSequence, step = activeStep,
                        target = eventTarget, nextAvailable = forceNextAvailable });
                    RestoreModifiers();
                    HideOverlay();
                    activeSequence = candidate;
                    activeStep = 0;
                    eventTarget = target;
                    startedStep = false;
                    return;
                }
            }
            if (activeSequence != null && activeSequence.finishEventKey == key &&
                activeStep == activeSequence.steps.Length - 1)
                Stop(true);
            if (activeSequence == null)
            {
                foreach (TutorialSequence candidate in sequences)
                {
                    if (candidate == null || candidate.startEventKey != key ||
                        TutorialProgressStore.IsCompleted(candidate.tutorialId)) continue;
                    activeSequence = candidate;
                    activeStep = 0;
                    eventTarget = target;
                    startedStep = false;
                    break;
                }
                return;
            }
            if (activeSequence.cancelEventKey == key) { Stop(false); return; }
            if (activeSequence.jumpEventKey == key)
            {
                if (activeSequence.requireJumpTargetMatch && !MatchesTarget(target, eventTarget)) return;
                if (activeSequence.cancelIfJumpBeforeStart && activeStep == 0)
                { Stop(false); return; }
                if (activeStep < activeSequence.jumpToStep) GoTo(activeSequence.jumpToStep);
                forceNextAvailable = true;
                return;
            }
            TutorialStep step = activeSequence.steps[activeStep];
            if (step.advance == TutorialAdvance.Event && step.eventKey == key &&
                (!step.requireEventTargetMatch || MatchesHighlightedTarget(target)))
            {
                stepEventCount++;
                if (stepEventCount >= Mathf.Max(1, step.requiredEventCount)) Advance();
                else progressText.text = $"{activeStep + 1}/{activeSequence.steps.Length} · {stepEventCount}/{step.requiredEventCount}";
            }
        }

        private bool MatchesHighlightedTarget(Transform target)
        {
            if (target == null || activeSequence == null) return false;
            TutorialStep step = activeSequence.steps[activeStep];
            TutorialTarget highlighted = TutorialTarget.Find(step.targetId, eventTarget);
            Transform expected = highlighted != null ? highlighted.transform : eventTarget;
            return MatchesTarget(target, expected);
        }

        private static bool MatchesTarget(Transform target, Transform expected)
            => target != null && expected != null &&
               (target == expected || target.IsChildOf(expected) || expected.IsChildOf(target));

        private void OnTargetClicked(TutorialTarget target)
        {
            if (activeSequence == null || !startedStep) return;
            TutorialStep step = activeSequence.steps[activeStep];
            if (step.advance == TutorialAdvance.TargetClick && target.Id == step.targetId) Advance();
        }

        public void ForceStart(TutorialSequence sequence)
        {
            if (sequence == null || sequence.steps == null || sequence.steps.Length == 0) return;
            suspended.Clear();
            Stop(false);
            activeSequence = sequence;
            activeStep = 0;
            startedStep = false;
            revealRequested = false;
            eventTarget = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (debugKeys && showDebugWindow)
                debugWindow = GUILayout.Window(GetInstanceID(), debugWindow, DrawDebugWindow, "Tutorial Test");
        }

        private void DrawDebugWindow(int id)
        {
            GUILayout.Label($"Current: {(activeSequence != null ? activeSequence.tutorialId : "none")}");
            GUILayout.Label(activeSequence != null
                ? $"Step: {activeStep + 1}/{activeSequence.steps.Length} {(startedStep ? "active" : "waiting for target")}" 
                : "F5 replay default  |  F6 next  |  F7 clear records");
            if (GUILayout.Button("Next step (F6)") && activeSequence != null) Advance();
            if (GUILayout.Button("Clear completed records (F7)")) TutorialProgressStore.Clear();
            if (GUILayout.Button("Clear and replay default") && debugSequence != null)
            {
                TutorialProgressStore.Clear();
                ForceStart(debugSequence);
            }
            foreach (TutorialSequence sequence in sequences)
            {
                if (sequence == null) continue;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{sequence.tutorialId}  {(TutorialProgressStore.IsCompleted(sequence.tutorialId) ? "done" : "new")}");
                if (GUILayout.Button("Replay", GUILayout.Width(debugReplayButtonWidth))) ForceStart(sequence);
                GUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Close (F8)")) showDebugWindow = false;
            GUI.DragWindow();
        }
#endif

        private void Advance() => GoTo(activeStep + 1);
        private void GoTo(int index)
        {
            RestoreModifiers();
            if (activeSequence == null) return;
            if (index >= activeSequence.steps.Length) { Stop(true); return; }
            activeStep = index;
            startedStep = false;
            revealRequested = false;
            resolvedTarget = null;
            HideOverlay();
        }

        private void Stop(bool completed)
        {
            if (completed && activeSequence != null) TutorialProgressStore.Complete(activeSequence.tutorialId);
            RestoreModifiers();
            if (suspended.Count > 0)
            {
                SuspendedTutorial previous = suspended.Pop();
                activeSequence = previous.sequence;
                activeStep = previous.step;
                eventTarget = previous.target;
                forceNextAvailable = previous.nextAvailable;
            }
            else
            {
                activeSequence = null;
                eventTarget = null;
                forceNextAvailable = false;
            }
            startedStep = false;
            revealRequested = false;
            HideOverlay();
        }

        private void RestoreModifiers()
        {
            if (ownedPause) { Time.timeScale = previousTimeScale; ownedPause = false; }
            KeyboardBlocked = false;
            MovementWhilePaused = false;
            if (combat != null) combat.TutorialEnemySpeedMultiplier = 1f;
        }

        private void HideOverlay()
        {
            if (spotlight != null) spotlight.gameObject.SetActive(false);
            if (bubble != null) bubble.gameObject.SetActive(false);
            if (arrow != null) arrow.gameObject.SetActive(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            if (confirmation != null) confirmation.SetActive(false);
        }

        private void BuildOverlay()
        {
            GameObject root = new GameObject("Tutorial Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = style.sortingOrder;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = style.referenceResolution;
            canvasRect = root.GetComponent<RectTransform>();
            Stretch(canvasRect);
            RectTransform shade = MakeRect("Spotlight", canvasRect, Vector2.zero);
            Stretch(shade);
            shade.gameObject.AddComponent<Image>().color = Color.white;
            spotlight = shade.gameObject.AddComponent<TutorialSpotlight>();
            spotlight.Configure(style);
            bubble = MakeRect("Explanation", canvasRect, style.bubbleSize);
            bubble.gameObject.AddComponent<Image>().color = style.bubbleColor;
            bubbleGroup = bubble.gameObject.AddComponent<CanvasGroup>();
            titleText = Label("Title", bubble, style.titleSize, Min(style.titleOffsets), Max(style.titleOffsets));
            bodyText = Label("Body", bubble, style.bodySize, Min(style.bodyOffsets), Max(style.bodyOffsets));
            progressText = Label("Progress", bubble, style.progressSize, Min(style.progressOffsets), Max(style.progressOffsets), true);
            skipButton = Button(style.skipLabel, canvasRect, Min(style.skipButtonOffsets), Max(style.skipButtonOffsets),
                () => confirmation.SetActive(true), true);
            arrow = MakeRect("Arrow", canvasRect, style.arrowSize);
            var arrowText = arrow.gameObject.AddComponent<TextMeshProUGUI>();
            arrowText.text = style.arrowGlyph;
            arrowText.fontSize = style.arrowFontSize;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = style.outlineColor;
            arrowText.raycastTarget = false;
            confirmation = MakeRect("Skip Confirmation", canvasRect, style.confirmationSize).gameObject;
            confirmation.AddComponent<Image>().color = style.bubbleColor;
            RectTransform confirmRect = (RectTransform)confirmation.transform;
            Label("Question", confirmRect, style.bodySize, Min(style.confirmationQuestionOffsets), Max(style.confirmationQuestionOffsets)).text = style.skipQuestion;
            Button(style.skipLabel, confirmRect, Min(style.confirmButtonOffsets), Max(style.confirmButtonOffsets), () => Stop(true));
            Button(style.cancelLabel, confirmRect, Min(style.cancelButtonOffsets), Max(style.cancelButtonOffsets), () => confirmation.SetActive(false));
            confirmation.SetActive(false);
        }

        private static RectTransform MakeRect(string name, RectTransform parent, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            return rect;
        }

        private static Vector2 Min(Vector4 value) => new Vector2(value.x, value.y);
        private static Vector2 Max(Vector4 value) => new Vector2(value.z, value.w);

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private TMP_Text Label(string name, RectTransform parent, float fontSize, Vector2 min, Vector2 max, bool bottom = false)
        {
            RectTransform rect = MakeRect(name, parent, Vector2.zero);
            rect.anchorMin = bottom ? Vector2.zero : new Vector2(0, 1);
            rect.anchorMax = bottom ? Vector2.zero : new Vector2(1, 1);
            rect.offsetMin = min;
            rect.offsetMax = max;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = style.font;
            label.fontSize = fontSize;
            label.color = style.textColor;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        private Button Button(string name, RectTransform parent, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, bool topRight = false)
        {
            RectTransform rect = MakeRect(name, parent, Vector2.zero);
            rect.anchorMin = topRight ? Vector2.one : Vector2.zero;
            rect.anchorMax = rect.anchorMin;
            rect.offsetMin = min;
            rect.offsetMax = max;
            rect.gameObject.AddComponent<Image>().color = style.bubbleColor;
            Button button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            TMP_Text text = Label(name + " Text", rect, style.progressSize, Vector2.zero, Vector2.zero);
            Stretch((RectTransform)text.transform);
            text.text = name;
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }
    }
}
