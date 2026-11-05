using System;
using System.Collections.Generic;
using ArmySurvivor.Equipment;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    // Survival-only run modifier. The catalog lives in SurvivalSettings, not in scene objects.
    public sealed class SurvivalTradeoffDraft : MonoBehaviour
    {
        public static SurvivalSettings.Tradeoff Selected { get; private set; }
        public static float CommanderHealth => Selected?.commanderHealth ?? 1f;
        public static float SoldierHealth => Selected?.soldierHealth ?? 1f;
        public static float Damage => Selected?.damage ?? 1f;
        public static float AttackSpeed => Selected?.attackSpeed ?? 1f;
        public static float MoveSpeed => Selected?.moveSpeed ?? 1f;
        public static float SpawnInterval => Selected?.spawnInterval ?? 1f;
        public static float PickupRadius => Selected?.pickupRadius ?? 1f;
        public static float Experience => Selected?.experience ?? 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Selected = null;

        private SurvivalSettings settings;
        private RunController run;
        private GameObject panel;
        private Action onComplete;
        private bool choosing;
        private float previousTimeScale;

        public void Configure(SurvivalSettings value)
        {
            settings = value;
            run = GetComponent<RunController>();
        }

        public bool TryOpen(Action beginBattle)
        {
            if (settings?.openingTradeoffs == null || run == null || choosing) return false;
            var pool = new List<SurvivalSettings.Tradeoff>();
            foreach (var choice in settings.openingTradeoffs)
                if (choice != null && !string.IsNullOrWhiteSpace(choice.title)) pool.Add(choice);
            if (pool.Count == 0) return false;
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[swap]) = (pool[swap], pool[i]);
            }
            Selected = null;
            onComplete = beginBattle;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0;
            run.IsChoosingUpgrade = true;
            choosing = true;
            BuildPanel(pool);
            return true;
        }

        private void BuildPanel(List<SurvivalSettings.Tradeoff> pool)
        {
            panel = new GameObject("Survival Tradeoff Draft", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            panel.transform.SetParent(transform, false);
            var root = (RectTransform)panel.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var canvas = panel.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 250;
            var scaler = panel.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            panel.GetComponent<Image>().color = new Color(0.03f, 0.08f, 0.11f, 0.94f);

            var catalog = Resources.Load<EquipmentCatalog>("EquipmentCatalog");
            TMP_FontAsset font = catalog != null ? catalog.font : null;
            Label("생존 계약 · 대가를 선택하세요", root, new Vector2(0, 300),
                new Vector2(1200, 90), 42, font);
            Label("세 가지 중 하나를 고르면 전투가 시작됩니다", root, new Vector2(0, 230),
                new Vector2(1200, 50), 25, font);
            int count = Mathf.Min(3, pool.Count);
            for (int i = 0; i < count; i++)
            {
                var tradeoff = pool[i];
                float x = (i - (count - 1) * 0.5f) * 480;
                var card = Rect("Tradeoff " + tradeoff.title, root, new Vector2(x, -30),
                    new Vector2(440, 440));
                var image = card.gameObject.AddComponent<Image>();
                image.color = new Color(0.07f, 0.13f, 0.21f, 1f);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => Choose(tradeoff));
                Label(tradeoff.title, card, new Vector2(0, 130), new Vector2(390, 70), 34, font);
                Label(tradeoff.description, card, new Vector2(0, -5), new Vector2(390, 230), 25, font);
                Label("선택", card, new Vector2(0, -175), new Vector2(390, 50), 25, font);
            }
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

        private static void Label(string text, Transform parent, Vector2 position,
            Vector2 size, float fontSize, TMP_FontAsset font)
        {
            var rect = Rect(text, parent, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            if (font != null) label.font = font;
            label.fontSize = fontSize;
            label.color = new Color(1f, 0.91f, 0.70f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
        }

        private void Choose(SurvivalSettings.Tradeoff tradeoff)
        {
            if (!choosing) return;
            Selected = tradeoff;
            var callback = onComplete;
            Close();
            callback?.Invoke();
        }

        private void Close()
        {
            if (!choosing) return;
            choosing = false;
            onComplete = null;
            run.IsChoosingUpgrade = false;
            Time.timeScale = previousTimeScale;
            if (panel != null) Destroy(panel);
            panel = null;
        }

        public void ResetChoice() { Close(); Selected = null; }
        private void OnDestroy() => Close();
    }
}
