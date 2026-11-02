using System.Collections.Generic;
using ArmySurvivor.Army;
using ArmySurvivor.Building;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ArmySurvivor.Tutorial;

namespace ArmySurvivor.Equipment
{
    public sealed partial class EquipmentUI : MonoBehaviour
    {
        EquipmentCatalog catalog;
        RunController run;
        BuildingPlacementController village;
        GameObject canvasRoot, modal, shortcut;
        TMP_Text shortcutText, currency, detail, pity, notice, title;
        Button one, ten, equip;
        ItemInspectView inspect;
        EquipmentItem selected;
        readonly List<TMP_Text> itemLabels = new List<TMP_Text>();
        readonly List<Button> resultButtons = new List<Button>();
        bool visible;
        SupplyDrawSequence drawSequence;
        static readonly Color Ink = new Color(0.055f, 0.075f, 0.11f, 1f);
        static readonly Color Gold = new Color(0.94f, 0.79f, 0.47f);
        public bool IsOpen => modal != null && modal.activeSelf;
        bool VillageMode => gameObject.scene.name == "VillageScene";
        bool Available => SceneManager.GetActiveScene() == gameObject.scene && (run == null || !run.IsRunning);
        bool CanDraw => Available && VillageMode && (TestMode || (village != null && village.HasBuilding(catalog.supplyBuilding)));
        bool TestMode
        {
            get
            {
#if UNITY_EDITOR
                return EquipmentInventory.IsTesting;
#else
                return false;
#endif
            }
        }
        bool HasTickets(int count)
        {
#if UNITY_EDITOR
            if (TestMode && EquipmentInventory.UnlimitedTestTickets) return true;
#endif
            return EquipmentInventory.Tickets >= count;
        }
        public bool IsDrawing => drawSequence != null && drawSequence.IsPlaying;

        public void Initialize(EquipmentCatalog data, RunController controller)
        {
            catalog = data; run = controller;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (village == null) village = root.GetComponentInChildren<BuildingPlacementController>(true);
            drawSequence = gameObject.AddComponent<SupplyDrawSequence>();
            Build(); EquipmentInventory.Changed += Refresh; Refresh();
        }
        RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        TMP_Text Label(string value, Transform parent, Vector2 pos, Vector2 size, float fontSize = 22)
        {
            var rect = Rect(value, parent, pos, size); var t = rect.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = catalog.font; t.text = value; t.fontSize = fontSize; t.color = Color.white;
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal; return t;
        }
        Button Button(string text, Transform parent, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(text, parent, pos, size); var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.21f, 0.27f, 0.98f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = Gold; colors.selectedColor = Color.white;
            colors.disabledColor = new Color(.4f,.4f,.4f,.65f); button.colors = colors;
            var label = Label(text, rect, new Vector2(12, -5), size - new Vector2(24, 10), 21);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            button.onClick.AddListener(action); return button;
        }
        Image TicketIcon(Transform parent, Vector2 position, float size)
        {
            var icon = Rect("Inferno Seal Icon", parent, position, Vector2.one * size).gameObject.AddComponent<Image>();
            icon.sprite = catalog.ticketIcon;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = catalog.ticketIcon != null;
            return icon;
        }
        void AddTicketCost(Button button, int count, float width)
        {
            button.GetComponentInChildren<TMP_Text>().rectTransform.sizeDelta = new Vector2(width - 115, 60);
            TicketIcon(button.transform, new Vector2(width - 108, -14), 42);
            var amount = Label("×" + count, button.transform, new Vector2(width - 60, -5), new Vector2(54, 60), 22);
            amount.alignment = TextAlignmentOptions.MidlineLeft;
        }
        void OpenEntry()
        {
            if (!Available) return;
            if (VillageMode && !CanDraw)
            {
                if (village != null) village.Select(catalog.supplyBuilding);
                return;
            }
            Open();
        }
        public void Open()
        {
            if (!Available || (VillageMode && !CanDraw)) return;
            if (village != null) village.Cancel();
            modal.SetActive(true);
            EnsureThumbnails(); Select(catalog.Find(EquipmentInventory.EquippedId) ?? catalog.items[0]); Refresh();
            if (VillageMode) TutorialEvents.Raise("SupplyOpened");
        }
        public void Close() { modal.SetActive(false); inspect.Clear(); HideOverlays(); }
        void Select(EquipmentItem item) { selected = item; inspect.Show(item); Refresh(); }
        public void Draw(int count)
        {
            if (drawSequence.IsPlaying || !CanDraw || !EquipmentInventory.TryDraw(catalog,count,out var results)) return;
            drawSequence.Play(catalog, () => { ShowResults(results); TutorialEvents.Raise("EquipmentDrawn"); });
        }
        void ShowResults(List<EquipmentItem> results)
        {
            ShowResultPanel(results);
            Select(results[results.Count-1]);
        }
        void Update()
        {
            if (canvasRoot == null) return;
            bool next = Available;
            if (visible != next) { visible = next; if (!next) Close(); canvasRoot.SetActive(next); }
            if (!next) return;
            if (drawSequence.IsPlaying) return;
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { if (!DismissOverlay()) Close(); }
            if (VillageMode)
            {
                shortcut.SetActive(CanDraw);
                if (CanDraw) shortcutText.text = $"왕국 보급소 · 인장 {EquipmentInventory.Tickets}";
            }
        }
        void Refresh() { RefreshDesign(); }
        void OnDestroy() { EquipmentInventory.Changed -= Refresh; foreach (var texture in thumbnailCache.Values) if (texture) Destroy(texture); }
        void OnDisable() { if (modal != null) Close(); }
    }
}
