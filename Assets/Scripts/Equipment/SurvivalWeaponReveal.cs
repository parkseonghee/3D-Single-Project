using ArmySurvivor.Army;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArmySurvivor.Equipment
{
    // A seal pauses Survival and shows the actual equipment model before combat resumes.
    public sealed class SurvivalWeaponReveal : MonoBehaviour
    {
        private RunController run;
        private EquipmentCatalog catalog;
        private GameObject panel;
        private ItemInspectView inspect;
        private TMP_Text title;
        private TMP_Text description;
        private float previousTimeScale;
        private bool previousChoosing;
        private bool ownsPause;
        public bool IsOpen => panel != null && panel.activeSelf;

        public void Initialize(EquipmentCatalog equipment, RunController controller)
        {
            catalog = equipment;
            run = controller;
        }

        public void Show(EquipmentItem item)
        {
            if (item == null || run == null || IsOpen) return;
            if (panel == null) Build();
            previousTimeScale = Time.timeScale;
            previousChoosing = run.IsChoosingUpgrade;
            Time.timeScale = 0;
            run.IsChoosingUpgrade = true;
            ownsPause = true;
            title.text = item.grade + " 등급 · " + item.displayName;
            description.text = item.description + "\n\n즉시 장착되었습니다. 드래그: 회전 · 휠: 확대/축소";
            panel.SetActive(true);
            inspect.Show(item);
        }

        public void Close()
        {
            if (!ownsPause) return;
            if (inspect != null) inspect.Clear();
            if (panel != null) panel.SetActive(false);
            if (run != null) run.IsChoosingUpgrade = previousChoosing;
            Time.timeScale = previousTimeScale;
            ownsPause = false;
        }

        private void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        private void OnDisable() => Close();
        private void OnDestroy() => Close();

        private void Build()
        {
            panel = new GameObject("Survival Weapon Reveal", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            var root = (RectTransform)panel.transform;
            root.SetParent(transform, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 300;
            var scaler = panel.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.09f, 0.94f);

            Label("인장 보상 · 새 무기", root, new Vector2(0, 400), new Vector2(1200, 80), 43);
            title = Label("", root, new Vector2(0, 305), new Vector2(1100, 65), 36);
            var frame = Rect("3D 무기", root, new Vector2(0, 20), new Vector2(610, 485));
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0.07f, 0.12f, 0.19f, 1);
            var display = Rect("무기 모델 · 드래그하여 회전", frame, Vector2.zero, new Vector2(580, 455));
            display.gameObject.AddComponent<RawImage>();
            inspect = display.gameObject.AddComponent<ItemInspectView>();
            description = Label("", root, new Vector2(0, -265), new Vector2(1050, 90), 25);

            var close = Rect("전투 계속", root, new Vector2(0, -407), new Vector2(310, 72));
            var image = close.gameObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.20f, 0.31f, 1);
            var button = close.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Close);
            Label("전투 계속", close, Vector2.zero, new Vector2(300, 65), 29);
            panel.SetActive(false);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private TMP_Text Label(string value, Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var rect = Rect(value, parent, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (catalog != null && catalog.font != null) label.font = catalog.font;
            label.text = value;
            label.fontSize = fontSize;
            label.color = new Color(1f, 0.91f, 0.70f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }
    }
}
