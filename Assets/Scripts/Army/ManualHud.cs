using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public sealed class ManualHud : MonoBehaviour
    {
        [SerializeField] private ManualManager manager;
        [SerializeField] private RectTransform hudParent;
        [SerializeField] private TMP_FontAsset font;
        private RectTransform root, pipsRoot;
        private TMP_Text label, timeLabel, announcement;
        private Image timer;
        private readonly List<Image> pips = new List<Image>();
        private float announcementRemaining;
        private void Awake()
        {
            root = Rect("Manual Progress", hudParent, new Vector2(0, 210), new Vector2(420, 92));
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0);
            label = Text("Title", root, new Vector2(0, 28), new Vector2(420, 26), 20);
            pipsRoot = Rect("Investment Steps", root, new Vector2(0, 0), new Vector2(350, 18));
            var background = Rect("Duration", root, new Vector2(0, -23), new Vector2(260, 5)).gameObject.AddComponent<Image>();
            background.color = new Color(0.08f, 0.12f, 0.15f, 0.8f); background.raycastTarget = false;
            timer = Rect("Fill", background.transform, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            timer.color = new Color(1, 0.8f, 0.35f); timer.raycastTarget = false;
            timer.rectTransform.anchorMin = Vector2.zero; timer.rectTransform.anchorMax = Vector2.one;
            timer.rectTransform.offsetMin = timer.rectTransform.offsetMax = Vector2.zero;
            timeLabel = Text("Remaining", root, new Vector2(0, -42), new Vector2(420, 24), 16);
            timeLabel.enableAutoSizing = true; timeLabel.fontSizeMin = 12; timeLabel.fontSizeMax = 16;
            announcement = Text("Activation", hudParent, new Vector2(0, 140), new Vector2(700, 65), 40);
            announcement.text = "균등 교육!"; announcement.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }
        private void OnEnable() { manager.Changed += Refresh; manager.Activated += Announce; Refresh(); }
        private void OnDisable()
        {
            manager.Changed -= Refresh; manager.Activated -= Announce;
            if (root != null) root.gameObject.SetActive(false);
            if (announcement != null) announcement.gameObject.SetActive(false);
        }
        private void Refresh()
        {
            bool visible = manager.ActiveManual != null && manager.Run.IsRunning;
            root.gameObject.SetActive(visible);
            if (!visible) { announcement.gameObject.SetActive(false); return; }
            label.text = manager.ActiveManual.displayName;
            while (pips.Count < manager.Required)
            {
                var image = Rect("Step " + (pips.Count + 1), pipsRoot, Vector2.zero, new Vector2(22, 10)).gameObject.AddComponent<Image>();
                image.raycastTarget = false; pips.Add(image);
            }
            float spacing = Mathf.Min(34, 340f / Mathf.Max(1, manager.Required));
            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].gameObject.SetActive(i < manager.Required);
                pips[i].rectTransform.anchoredPosition = new Vector2((i - (manager.Required - 1) * 0.5f) * spacing, 0);
                pips[i].color = i < manager.Progress ? new Color(1, 0.8f, 0.35f) : new Color(0.3f, 0.38f, 0.42f);
            }
            timer.rectTransform.anchorMax = new Vector2(manager.Duration > 0 ? Mathf.Clamp01(manager.Remaining / manager.Duration) : 0, 1);
            timeLabel.text = manager.Status;
        }
        private void Announce() { announcement.text = manager.Announcement; announcementRemaining = 1.5f; announcement.alpha = 1; announcement.gameObject.SetActive(true); }
        private void Update()
        {
            if (announcementRemaining <= 0 || manager.Run.IsPaused || manager.Run.IsChoosingUpgrade) return;
            announcementRemaining -= Time.deltaTime;
            announcement.alpha = Mathf.Clamp01(announcementRemaining / 0.4f);
            if (announcementRemaining <= 0) announcement.gameObject.SetActive(false);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private TMP_Text Text(string name, Transform parent, Vector2 position, Vector2 size, float points)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = points; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1, 0.9f, 0.65f); text.raycastTarget = false;
            var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -1);
            return text;
        }
        private void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
            if (announcement != null) Destroy(announcement.gameObject);
        }
    }
}
