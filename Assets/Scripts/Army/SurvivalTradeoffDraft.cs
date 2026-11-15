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
            Vector2 size = settings.tradeoffCardSize;
            float top = settings.tradeoffCardY + size.y * 0.5f;
            float bottom = settings.tradeoffCardY - size.y * 0.5f;
            Label("생존 계약 · 대가를 선택하세요", root, new Vector2(0, top + 115),
                new Vector2(1200, 80), 42, font);
            Label("세 가지 중 하나를 고르면 전투가 시작됩니다", root, new Vector2(0, top + 60),
                new Vector2(1200, 44), 25, font);
            int count = Mathf.Min(3, pool.Count);
            for (int i = 0; i < count; i++)
            {
                var tradeoff = pool[i];
                float x = (i - (count - 1) * 0.5f) * settings.tradeoffCardSpacing;
                var card = Rect("Tradeoff " + tradeoff.title, root, new Vector2(x, settings.tradeoffCardY), size);
                var image = card.gameObject.AddComponent<Image>();
                image.sprite = tradeoff.card;
                image.preserveAspect = true;
                image.color = tradeoff.card != null ? Color.white : new Color(0.07f, 0.13f, 0.21f, 1f);
                var button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                var tint = button.colors;
                tint.highlightedColor = new Color(1f, 0.95f, 0.82f);
                tint.pressedColor = new Color(0.85f, 0.85f, 0.85f);
                button.colors = tint;
                button.onClick.AddListener(() => Choose(tradeoff));

                // 리본과 설명 칸은 카드 높이 대비 비율로 잡아 카드 크기를 바꿔도 그림에 맞는다.
                float halfTitle = settings.tradeoffTitleHeight * 0.5f;
                var titleBand = new Vector2(tradeoff.cardTitleCenter - halfTitle, tradeoff.cardTitleCenter + halfTitle);
                var title = Label(tradeoff.title, card, BandCenter(titleBand, size),
                    BandSize(titleBand, size, settings.tradeoffTitleWidth), 30, font);
                title.color = tradeoff.cardDarkRibbon ? settings.tradeoffDarkRibbonTitleColor : settings.tradeoffTitleColor;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                FitText(title, 18);
                string body = string.IsNullOrEmpty(settings.tradeoffDescriptionLineBreak) || tradeoff.description == null
                    ? tradeoff.description
                    : tradeoff.description.Replace(settings.tradeoffDescriptionLineBreak, "\n");
                var description = Label(body, card, BandCenter(settings.tradeoffDescriptionBand, size),
                    BandSize(settings.tradeoffDescriptionBand, size, settings.tradeoffDescriptionWidth), 22, font);
                description.color = settings.tradeoffDescriptionColor;
                FitText(description, 14);
                Label("선택", card, new Vector2(0, bottom - settings.tradeoffCardY - 32), new Vector2(size.x, 44), 25, font);
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

        private static Vector2 BandCenter(Vector2 band, Vector2 card) =>
            new Vector2(0, card.y * (0.5f - (band.x + band.y) * 0.5f));

        private static Vector2 BandSize(Vector2 band, Vector2 card, float widthRatio) =>
            new Vector2(card.x * widthRatio, card.y * Mathf.Abs(band.y - band.x));

        private static void FitText(TMP_Text label, float minimum)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = minimum;
        }

        private static TMP_Text Label(string text, Transform parent, Vector2 position,
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
            return label;
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
