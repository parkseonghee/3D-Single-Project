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
    public sealed class EquipmentUI : MonoBehaviour
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
        bool CanDraw => Available && VillageMode && village != null && village.HasBuilding(catalog.supplyBuilding);

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
        void Build()
        {
            canvasRoot = new GameObject("Equipment Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 180;
            var scaler = canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            var entry = Button("", canvasRoot.transform, new Vector2(28, -105), new Vector2(285, 58), OpenEntry);
            shortcut = entry.gameObject; shortcutText = entry.GetComponentInChildren<TMP_Text>();
            var entryTarget = shortcut.AddComponent<TutorialTarget>();
            entryTarget.SetId("SupplyDepotEntry");
            var panel = Rect("Supply and Equipment", canvasRoot.transform, Vector2.zero, new Vector2(1600, 900));
            panel.anchorMax = Vector2.one; panel.anchorMin = Vector2.zero; panel.offsetMin = panel.offsetMax = Vector2.zero;
            modal = panel.gameObject; modal.AddComponent<Image>().color = Ink;
            Button("← 돌아가기", panel, new Vector2(30,-24), new Vector2(190,52), Close);
            title = Label("왕국 보급소", panel, new Vector2(250,-28), new Vector2(700,55), 34);
            var currencyLabel = Label("뽑기권", panel, new Vector2(1190,-28), new Vector2(100,52), 26);
            currencyLabel.alignment = TextAlignmentOptions.MidlineLeft;
            TicketIcon(panel, new Vector2(1290,-26), 56);
            currency = Label("", panel, new Vector2(1354,-28), new Vector2(215,52), 26);
            currency.alignment = TextAlignmentOptions.MidlineLeft;
            Label("보유 장비 · 선택하여 3D 관찰", panel, new Vector2(35,-120), new Vector2(420,40), 23);
            for (int i = 0; i < catalog.items.Length; i++)
            {
                var item = catalog.items[i];
                float spacing = Mathf.Min(92f, 500f / Mathf.Max(1, catalog.items.Length));
                var button = Button("", panel, new Vector2(35,-175-i*spacing), new Vector2(360,spacing-12), () => Select(item));
                if (catalog.items.Length >= 8) button.GetComponentInChildren<TMP_Text>().fontSize = 18;
                itemLabels.Add(button.GetComponentInChildren<TMP_Text>());
            }
            var preview = Rect("3D Item • drag to rotate", panel, new Vector2(420,-130), new Vector2(610,560));
            preview.gameObject.AddComponent<RawImage>().color = Color.white;
            inspect = preview.gameObject.AddComponent<ItemInspectView>();
            Label("드래그: 회전   ·   휠: 확대/축소", panel, new Vector2(470,-670), new Vector2(550,40), 20);
            Button("시점 초기화", panel, new Vector2(610,-718), new Vector2(215,44), () => inspect.ResetView());
            detail = Label("", panel, new Vector2(1080,-165), new Vector2(470,280), 27);
            equip = Button("장착", panel, new Vector2(1080,-470), new Vector2(460,58), () =>
            {
                if (!Available || VillageMode || selected == null) return;
                EquipmentInventory.Equip(selected.id == EquipmentInventory.EquippedId ? null : selected.id);
            });
            notice = Label("", panel, new Vector2(1080,-545), new Vector2(460,100), 20);
            pity = Label("", panel, new Vector2(1080,-660), new Vector2(470,90), 20);
            one = Button("1회 보급", panel, new Vector2(1070,-790), new Vector2(225,70), () => Draw(1));
            one.gameObject.AddComponent<TutorialTarget>().SetId("SupplyDrawOne");
            ten = Button("10회 보급", panel, new Vector2(1310,-790), new Vector2(255,70), () => Draw(10));
            AddTicketCost(one, 1, 225);
            AddTicketCost(ten, 10, 255);
            for (int i=0;i<10;i++)
            {
                var b=Button("", panel, new Vector2(30+(i%5)*195,-785-(i/5)*49), new Vector2(185,43), () => {});
                b.GetComponentInChildren<TMP_Text>().fontSize=16; b.gameObject.SetActive(false); resultButtons.Add(b);
            }
            modal.SetActive(false);
            canvasRoot.SetActive(false);
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
            Select(catalog.Find(EquipmentInventory.EquippedId) ?? catalog.items[0]); Refresh();
            if (VillageMode) TutorialEvents.Raise("SupplyOpened");
        }
        public void Close() { modal.SetActive(false); inspect.Clear(); }
        void Select(EquipmentItem item) { selected = item; inspect.Show(item); Refresh(); }
        public void Draw(int count)
        {
            if (drawSequence.IsPlaying || !CanDraw || !EquipmentInventory.TryDraw(catalog,count,out var results)) return;
            drawSequence.Play(catalog, () => { ShowResults(results); TutorialEvents.Raise("EquipmentDrawn"); });
        }
        void ShowResults(List<EquipmentItem> results)
        {
            foreach (var button in resultButtons) button.gameObject.SetActive(false);
            for (int i=0;i<results.Count;i++)
            {
                var item = results[i]; var b=resultButtons[i]; b.gameObject.SetActive(true);
                b.GetComponentInChildren<TMP_Text>().text=$"{item.grade} · {item.displayName}";
                b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => Select(item));
            }
            Select(results[results.Count-1]);
        }
        void Update()
        {
            if (canvasRoot == null) return;
            bool next = Available;
            if (visible != next) { visible = next; if (!next) Close(); canvasRoot.SetActive(next); }
            if (!next) return;
            if (drawSequence.IsPlaying) return;
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
            if (VillageMode)
            {
                shortcut.SetActive(CanDraw);
                if (CanDraw) shortcutText.text = $"왕국 보급소 · 인장 {EquipmentInventory.Tickets}";
            }
        }
        void Refresh()
        {
            if (catalog == null || currency == null) return;
            var equipped = catalog.Find(EquipmentInventory.EquippedId);
            if (!VillageMode) shortcutText.text = "장비 1칸 · " + (equipped != null ? equipped.displayName : "비어 있음");
            title.text = VillageMode ? "왕국 보급소 · 장비 보급" : "출전 준비 · 지휘관 장비";
            currency.text = EquipmentInventory.Tickets.ToString();
            one.gameObject.SetActive(VillageMode); ten.gameObject.SetActive(VillageMode);
            one.interactable = CanDraw && EquipmentInventory.Tickets >= 1;
            ten.interactable = CanDraw && EquipmentInventory.Tickets >= 10;
            string sGuarantee = catalog.HasSRewards ? $"S까지 {Mathf.Max(1,catalog.sGuarantee-EquipmentInventory.SinceS)}회" : "S 미등록 · 출현 안 함";
            pity.text = $"A 이상까지 최대 {Mathf.Max(1,catalog.aGuarantee-EquipmentInventory.SinceA)}회 · {sGuarantee}\n기본 확률 S {catalog.sRate:P1} / A {catalog.aRate:P1}\nB {catalog.bRate:P1} / C {catalog.CRate:P1} · 보장 별도";
            for (int i=0;i<itemLabels.Count;i++)
            {
                var item=catalog.items[i]; var count=EquipmentInventory.Count(item.id);
                itemLabels[i].text=$"{item.grade}  {item.displayName}\n{(count>0 ? $"보유 {count}개" : "미보유 · 미리 보기")}{(item.id==EquipmentInventory.EquippedId ? "  [장착]" : "")}";
            }
            if (selected == null) return;
            detail.text=$"<color=#EACB7E>{selected.grade} 등급</color>\n<size=34>{selected.displayName}</size>\n\n<size=22>{selected.description}</size>";
            equip.gameObject.SetActive(!VillageMode);
            equip.interactable=Available && EquipmentInventory.Count(selected.id)>0;
            equip.GetComponentInChildren<TMP_Text>().text=selected.id==EquipmentInventory.EquippedId ? "장착 해제" : "지휘관 공용 슬롯에 장착";
            notice.text=VillageMode ? "획득 장비는 준비 단계에서 장착합니다.\n중복 획득은 보유 수량으로 누적됩니다.\n현재 장비 능력치는 적용되지 않습니다." : "장착 상태는 저장 슬롯에 기록됩니다.\n현재 장비 능력치는 적용되지 않습니다.";
        }
        void OnDestroy() { EquipmentInventory.Changed -= Refresh; }
        void OnDisable() { if (modal != null) Close(); }
    }
}
