using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public sealed class DiabloHud : MonoBehaviour
    {
        [SerializeField] private CanvasGroup[] originalHud;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField, Range(0.2f, 0.3f)] private float fadeSeconds = 0.25f;
        [SerializeField, Min(0.1f)] private float titleSeconds = 1;
        private CanvasGroup group;
        private RectTransform hpFill, timeFill;
        private TMP_Text hpText, timeText, meteorText, title;
        private DiabloController source;
        private UnitHealth health;
        private float blend, titleElapsed;
        private float[] oldAlpha;
        private bool[] oldInteraction, oldRaycasts;
        private bool visible, captured;
        private Sprite circle;
        private Texture2D circleTexture;

        public void Show(DiabloController controller, UnitHealth commanderHealth)
        {
            EnsureUI();
            source = controller;
            if (health != null) health.OnHpChanged -= UpdateHealth;
            health = commanderHealth; health.OnHpChanged += UpdateHealth; UpdateHealth(health);
            if (!captured)
            {
                int count = originalHud == null ? 0 : originalHud.Length;
                oldAlpha = new float[count]; oldInteraction = new bool[count]; oldRaycasts = new bool[count];
                for (int i = 0; i < count; i++) if (originalHud[i] != null)
                { oldAlpha[i] = originalHud[i].alpha; oldInteraction[i] = originalHud[i].interactable; oldRaycasts[i] = originalHud[i].blocksRaycasts; }
                captured = true;
            }
            visible = true; titleElapsed = 0;
        }

        public void Hide()
        {
            visible = false;
            if (health != null) health.OnHpChanged -= UpdateHealth;
            health = null;
        }

        private void UpdateHealth(UnitHealth value)
        {
            hpFill.anchorMax = new Vector2(1, value.Maximum > 0 ? Mathf.Clamp01(value.Current / value.Maximum) : 0);
            hpText.text = $"{Mathf.CeilToInt(value.Current)} / {Mathf.CeilToInt(value.Maximum)}";
        }

        private void Update()
        {
            if (group == null) return;
            blend = Mathf.MoveTowards(blend, visible ? 1 : 0, Time.unscaledDeltaTime / fadeSeconds);
            group.alpha = blend;
            if (captured)
            {
                for (int i = 0; i < originalHud.Length; i++) if (originalHud[i] != null)
                {
                    originalHud[i].alpha = oldAlpha[i] * (1 - blend);
                    originalHud[i].interactable = blend == 0 && oldInteraction[i];
                    originalHud[i].blocksRaycasts = blend == 0 && oldRaycasts[i];
                }
                if (!visible && blend <= 0) captured = false;
            }
            if (visible && source != null)
            {
                timeFill.anchorMax = new Vector2(Mathf.Clamp01(source.Remaining / source.Duration), 1);
                timeText.text = $"DIABLO  {source.Remaining:0.0}초";
                meteorText.text = $"자동 메테오\n{source.MeteorCooldown:0.0}초";
            }
            titleElapsed += Time.unscaledDeltaTime;
            title.alpha = visible ? Mathf.Clamp01(1 - titleElapsed / titleSeconds) : 0;
        }

        private void EnsureUI()
        {
            if (group != null) return;
            var root = new GameObject("Diablo HUD", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            group = root.GetComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
            circleTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                pixels[y * 128 + x] = new Color(1, 1, 1, Mathf.Clamp01(63.5f - Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f))));
            circleTexture.SetPixels(pixels); circleTexture.Apply();
            circle = Sprite.Create(circleTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            var orb = Rect("Health Orb Frame", root.transform, new Vector2(155, 135), new Vector2(170, 170), Vector2.zero);
            var border = orb.gameObject.AddComponent<Image>(); border.sprite = circle; border.color = new Color(0.48f, 0.35f, 0.19f); border.raycastTarget = false;
            var mask = Rect("Health Orb", orb, Vector2.zero, new Vector2(150, 150), new Vector2(0.5f, 0.5f));
            var maskImage = mask.gameObject.AddComponent<Image>(); maskImage.sprite = circle; maskImage.color = new Color(0.08f, 0.005f, 0.008f); maskImage.raycastTarget = false;
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            hpFill = Fill("Blood", mask, new Color(0.72f, 0.025f, 0.045f));
            hpText = Label("HP", orb, new Vector2(0, -108), new Vector2(250, 35), "", 24);
            var slots = Rect("Skills", root.transform, new Vector2(0, 90), new Vector2(400, 110), new Vector2(0.5f, 0));
            for (int i = 0; i < 2; i++)
            {
                var slot = Rect("Skill " + i, slots, new Vector2((i - 0.5f) * 195, 0), new Vector2(180, 100), new Vector2(0.5f, 0.5f));
                var image = slot.gameObject.AddComponent<Image>(); image.color = new Color(0.12f, 0.025f, 0.028f, 0.96f); image.raycastTarget = false;
                var text = Label("Label", slot, Vector2.zero, new Vector2(170, 90), i == 0 ? "좌클릭\n3갈래 화염" : "", 25);
                if (i == 1) meteorText = text;
            }
            var bar = Rect("Duration", slots, new Vector2(0, 80), new Vector2(380, 12), new Vector2(0.5f, 0.5f));
            var background = bar.gameObject.AddComponent<Image>(); background.color = new Color(0.08f, 0.01f, 0.01f); background.raycastTarget = false;
            timeFill = Fill("Fill", bar, new Color(0.85f, 0.25f, 0.05f));
            timeText = Label("Remaining", slots, new Vector2(0, 111), new Vector2(400, 44), "", 26);
            title = Label("DIABLO", root.transform, new Vector2(0, 100), new Vector2(1000, 180), "DIABLO", 115);
            title.color = new Color(1, 0.18f, 0.04f);
        }
        private RectTransform Fill(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }
        private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size, Vector2 anchor)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize)
        {
            var rect = Rect(name, parent, position, size, new Vector2(0.5f, 0.5f));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center; label.color = new Color(0.94f, 0.8f, 0.62f); label.raycastTarget = false; return label;
        }
        private void OnDisable()
        {
            Hide();
            if (captured) for (int i = 0; i < originalHud.Length; i++) if (originalHud[i] != null)
            { originalHud[i].alpha = oldAlpha[i]; originalHud[i].interactable = oldInteraction[i]; originalHud[i].blocksRaycasts = oldRaycasts[i]; }
            captured = false; blend = 0; if (group != null) group.alpha = 0;
        }
        private void OnDestroy() { if (circle != null) Destroy(circle); if (circleTexture != null) Destroy(circleTexture); }
    }
}
