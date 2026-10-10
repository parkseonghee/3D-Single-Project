using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public sealed class OperatorHud : MonoBehaviour
    {
        [SerializeField] private BattleExperience experience;
        [SerializeField] private RunController run;
        [SerializeField] private RectTransform slotArea;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Button bottleButton;
        [SerializeField] private string portraitLayer = "UnitPortrait";
        [SerializeField, Range(64, 137)] private float slotWidth = 120;
        private sealed class Slot
        {
            public Transform unit;
            public UnitHealth health;
            public RectTransform root;
            public RawImage portrait;
            public UnitPortraitCache.Portrait textures;
            public TMP_Text level;
            public Image hp;
            public PortraitSelectionRing ring;
            public Button button;
            public System.Action<UnitHealth> healthChanged;
        }
        private readonly List<Slot> slots = new List<Slot>();
        private readonly Dictionary<Camera, int> cameraMasks = new Dictionary<Camera, int>();
        private UnitPortraitCache cache;
        private ScrollRect scroll;
        private RectTransform content;
        private Coroutine rebuild;
        private int selected = -1;
        public Transform SelectedUnit => selected >= 0 && selected < slots.Count ? slots[selected].unit : null;
        public int SlotCount => slots.Count;
        public int PortraitCaptureCount => cache == null ? 0 : cache.CaptureCount;
        public event System.Action<Transform> SelectionChanged;

        private void Awake()
        {
            int layer = LayerMask.NameToLayer(portraitLayer);
            if (layer < 0)
            {
                Debug.LogError("UnitPortrait 레이어를 추가해주세요.", this);
                enabled = false;
                return;
            }
            foreach (var camera in Camera.allCameras)
            {
                cameraMasks[camera] = camera.cullingMask;
                camera.cullingMask &= ~(1 << layer);
            }
            cache = new UnitPortraitCache(layer);
            scroll = slotArea.gameObject.AddComponent<ScrollRect>();
            slotArea.gameObject.AddComponent<RectMask2D>();
            content = Rect("Slots", slotArea, Vector2.zero, new Vector2(0, 116));
            content.anchorMin = content.anchorMax = new Vector2(0, 0.5f);
            content.pivot = new Vector2(0, 0.5f);
            scroll.viewport = slotArea;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25;
            if (bottleButton != null) bottleButton.onClick.AddListener(FeedSelected);
        }

        private void OnEnable()
        {
            experience.RosterChanged += ScheduleRebuild;
            experience.UnitLeveledUp += LevelChanged;
            ScheduleRebuild();
        }

        private void ScheduleRebuild()
        {
            if (!isActiveAndEnabled || cache == null) return;
            if (rebuild != null) StopCoroutine(rebuild);
            rebuild = StartCoroutine(RebuildAfterInitialization());
        }

        private IEnumerator RebuildAfterInitialization()
        {
            // All RunStarted subscribers (including combat HP initialization) finish first.
            yield return null;
            ClearSlots();
            foreach (var unit in experience.TrackedUnits)
            {
                var definition = experience.DefinitionFor(unit);
                if (definition == null) continue;
                int index = slots.Count;
                var slot = new Slot { unit = unit, health = unit.GetComponent<UnitHealth>() };
                slot.root = Rect("Operator " + (index + 1) + " " + definition.displayName,
                    content, new Vector2(slotWidth * 0.5f + 12 + index * (slotWidth + 14), 0), new Vector2(slotWidth, 100));
                slot.root.anchorMin = slot.root.anchorMax = new Vector2(0, 0.5f);
                var hit = slot.root.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                slot.button = slot.root.gameObject.AddComponent<Button>();
                slot.button.transition = Selectable.Transition.None;
                slot.button.onClick.AddListener(() => Select(index));
                var ringRect = Rect("Selection Ring", slot.root, new Vector2(0, -2), new Vector2(94, 94));
                slot.ring = ringRect.gameObject.AddComponent<PortraitSelectionRing>();
                slot.ring.color = new Color(1, 0.85f, 0.45f);
                slot.ring.raycastTarget = false;
                slot.portrait = Rect("Portrait", slot.root, new Vector2(0, -1), new Vector2(88, 88)).gameObject.AddComponent<RawImage>();
                slot.portrait.raycastTarget = false;
                slot.textures = cache.Get(definition);
                if (slot.textures != null) slot.portrait.texture = slot.textures.normal;
                var key = Rect("Keycap", slot.root, new Vector2(0, 44), new Vector2(24, 18));
                var keyBackground = key.gameObject.AddComponent<Image>();
                keyBackground.color = new Color(0.06f, 0.1f, 0.12f, 0.85f);
                keyBackground.raycastTarget = false;
                Label("Key", key, Vector2.zero, new Vector2(24, 18), index < 5 ? (index + 1).ToString() : "", 14);
                if (index >= 5) key.gameObject.SetActive(false);
                var badge = Rect("Level Badge", slot.root, new Vector2(-35, 26), new Vector2(43, 19));
                var badgeImage = badge.gameObject.AddComponent<Image>();
                badgeImage.color = new Color(0.04f, 0.09f, 0.13f, 0.85f);
                badgeImage.raycastTarget = false;
                slot.level = Label("Level", badge, Vector2.zero, new Vector2(43, 19), "Lv." + experience.GetProgress(unit).Level, 14);
                var hpBackground = Rect("HP", slot.root, new Vector2(0, -45), new Vector2(88, 5)).gameObject.AddComponent<Image>();
                hpBackground.color = new Color(0.02f, 0.05f, 0.06f, 0.9f);
                hpBackground.raycastTarget = false;
                slot.hp = Rect("Fill", hpBackground.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
                slot.hp.raycastTarget = false;
                slot.hp.rectTransform.anchorMin = Vector2.zero;
                slot.hp.rectTransform.anchorMax = Vector2.one;
                slot.hp.rectTransform.offsetMin = slot.hp.rectTransform.offsetMax = Vector2.zero;
                slot.healthChanged = health => UpdateHealth(slot);
                if (slot.health != null) slot.health.OnHpChanged += slot.healthChanged;
                slots.Add(slot);
                UpdateHealth(slot);
            }
            content.sizeDelta = new Vector2(24 + slots.Count * (slotWidth + 14), 116);
            if (slots.Count > 0) Select(0);
            rebuild = null;
        }

        private void UpdateHealth(Slot slot)
        {
            float ratio = slot.health != null && slot.health.Maximum > 0 ? slot.health.Current / slot.health.Maximum : 0;
            bool dead = slot.health == null || slot.health.IsDead;
            slot.hp.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1);
            slot.hp.color = ratio <= 0.3f ? new Color(1, 0.2f, 0.22f) : new Color(0.35f, 0.88f, 0.95f);
            if (slot.textures != null) slot.portrait.texture = dead ? slot.textures.dead : slot.textures.normal;
            slot.portrait.color = new Color(1, 1, 1, dead ? 0.4f : 1);
            slot.button.interactable = !dead;
            if (dead && SelectedUnit == slot.unit)
            {
                int next = slots.FindIndex(s => s.health != null && !s.health.IsDead);
                Select(next);
            }
        }

        private void LevelChanged(Transform unit, int level)
        {
            foreach (var slot in slots) if (slot.unit == unit) slot.level.text = "Lv." + level;
        }

        public void Select(int index)
        {
            if (index >= slots.Count || index < -1) return;
            if (index >= 0 && (slots[index].health == null || slots[index].health.IsDead)) return;
            selected = index;
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].root.localScale = Vector3.one * (i == selected ? 1.15f : 1);
                slots[i].ring.enabled = i == selected;
            }
            if (selected >= 0 && content.rect.width > slotArea.rect.width)
            {
                float center = slots[selected].root.anchoredPosition.x;
                float offset = Mathf.Clamp(center - slotArea.rect.width * 0.5f, 0, content.rect.width - slotArea.rect.width);
                content.anchoredPosition = new Vector2(-offset, 0);
            }
            SelectionChanged?.Invoke(SelectedUnit);
        }

        public void FeedSelected()
        {
            if (SelectedUnit != null && slots[selected].health != null && !slots[selected].health.IsDead)
                experience.Invest(SelectedUnit);
        }

        private void Update()
        {
            // Only input is polled. HP and levels are updated exclusively by events.
            if (!run.IsRunning || run.IsPaused || experience.IsChoosingUpgrade || Keyboard.current == null) return;
            for (int i = 0; i < Mathf.Min(5, slots.Count); i++)
                if (Keyboard.current[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Select(i);
        }

        private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text, float sizeInPoints)
        {
            var label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = sizeInPoints;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.98f, 0.95f, 0.84f);
            label.raycastTarget = false;
            return label;
        }

        private void ClearSlots()
        {
            foreach (var slot in slots)
            {
                if (slot.health != null) slot.health.OnHpChanged -= slot.healthChanged;
                if (slot.root != null) { slot.root.gameObject.SetActive(false); Destroy(slot.root.gameObject); }
            }
            slots.Clear();
            selected = -1;
        }

        private void OnDisable()
        {
            experience.RosterChanged -= ScheduleRebuild;
            experience.UnitLeveledUp -= LevelChanged;
            if (rebuild != null) StopCoroutine(rebuild);
            rebuild = null;
            ClearSlots();
        }

        private void OnDestroy()
        {
            if (bottleButton != null) bottleButton.onClick.RemoveListener(FeedSelected);
            cache?.Dispose();
            foreach (var pair in cameraMasks) if (pair.Key != null) pair.Key.cullingMask = pair.Value;
        }
    }
}
