using System.Collections.Generic;
using ArmySurvivor.Tutorial;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Equipment
{
    // Figma 11:5467 / 11:5850 / 11:6085. Existing inventory and 3D inspect stay authoritative.
    public sealed partial class EquipmentUI
    {
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
        static readonly Color Muted = Hex("81929F"), Foreground = Hex("E8ECED");
        readonly List<Button> rows = new List<Button>();
        readonly List<RawImage> thumbnails = new List<RawImage>();
        readonly List<GameObject> rowSelection = new List<GameObject>();
        readonly Dictionary<EquipmentItem, Texture2D> thumbnailCache = new Dictionary<EquipmentItem, Texture2D>();
        TMP_Text gradeText, itemName, ownership, shortageOne, shortageTen, listTitle, listCount, rates;
        GameObject probabilityPanel, resultsPanel;
        RectTransform listContent;
        bool ownedOnly;

        Image Box(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var im = Rect(name, parent, new Vector2(x,-y), new Vector2(w,h)).gameObject.AddComponent<Image>();
            im.color = color; im.raycastTarget = false; return im;
        }
        TMP_Text Text(string value, Transform parent, float x, float y, float w, float h, float size, Color color)
        {
            var t=Label(value,parent,new Vector2(x,-y),new Vector2(w,h),size);
            t.color=color; return t;
        }
        RawImage Art(string name, Transform parent, float x,float y,float w,float h)
        {
            var r=Rect(name,parent,new Vector2(x,-y),new Vector2(w,h)).gameObject.AddComponent<RawImage>();
            r.texture=Resources.Load<Texture2D>("SupplyDesign/"+name); r.raycastTarget=false;
            r.enabled=r.texture!=null; return r;
        }
        void Border(Transform parent,float w,float h,Color c)
        {
            Box("Top",parent,0,0,w,1,c); Box("Bottom",parent,0,h-1,w,1,c);
            Box("Left",parent,0,0,1,h,c); Box("Right",parent,w-1,0,1,h,c);
        }
        Button StyledButton(string caption, Transform parent, float x,float y,float w,float h,
            UnityEngine.Events.UnityAction click, bool gold=false, bool plain=false)
        {
            var b=Button(caption,parent,new Vector2(x,-y),new Vector2(w,h),click);
            b.image.color=Color.white;
            var colors=b.colors;
            colors.normalColor=plain ? new Color(0,0,0,0) : gold ? Hex("B7A06A") : Hex("25333F");
            colors.highlightedColor=gold ? Hex("D6C498") : Hex("344554");
            colors.pressedColor=gold ? Hex("8D7A50") : Hex("17212A");
            colors.selectedColor=colors.normalColor;
            colors.disabledColor=Hex("262F37"); b.colors=colors;
            var t=b.GetComponentInChildren<TMP_Text>(); t.color=gold ? Hex("181C21") : Foreground;
            t.fontSize=h>=80 ? 30 : 21;
            if (!plain) Border(b.transform,w,h,gold ? Hex("BBA572") : Hex("536878"));
            return b;
        }
        void Build()
        {
            canvasRoot=new GameObject("Equipment Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform,false);
            var canvas=canvasRoot.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=180;
            var scaler=canvasRoot.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var entry=Button("",canvasRoot.transform,new Vector2(28,-105),new Vector2(285,58),OpenEntry);
            shortcut=entry.gameObject; shortcutText=entry.GetComponentInChildren<TMP_Text>();
            shortcut.AddComponent<TutorialTarget>().SetId("SupplyDepotEntry");
            // A fitted 16:9 composition keeps all four corners accessible on other aspect ratios.
            var backdrop=Box("Supply and Equipment",canvasRoot.transform,0,0,1920,1080,Hex("090F16"));
            modal=backdrop.gameObject; backdrop.raycastTarget=true;
            var bgRect=backdrop.rectTransform; bgRect.anchorMin=Vector2.zero; bgRect.anchorMax=Vector2.one;
            bgRect.offsetMin=bgRect.offsetMax=Vector2.zero;
            var panel=Rect("Figma Composition",bgRect,Vector2.zero,new Vector2(1920,1080));
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);
            panel.gameObject.AddComponent<SupplyUIFit>();
            Art("Armory",panel,0,0,1920,1080);
            var shade=Box("Readability gradient",panel,0,0,1920,1080,Color.white);
            var gradient=shade.gameObject.AddComponent<SupplyUIGradient>(); gradient.horizontal=true;
            gradient.colors=new[]{new Color(7/255f,14/255f,24/255f,.937f),new Color(8/255f,15/255f,23/255f,.314f),new Color(8/255f,15/255f,23/255f,.145f),new Color(7/255f,14/255f,23/255f,.941f)};
            gradient.stops=new[]{0f,.3f,.64f,1f};
            var lower=Box("Bottom gradient",panel,0,740,1920,340,Color.white).gameObject.AddComponent<SupplyUIGradient>();
            lower.colors=new[]{new Color(9/255f,17/255f,27/255f,0),new Color(9/255f,17/255f,27/255f,.92f),Hex("080E16")}; lower.stops=new[]{0f,.45f,1f};
            Art("Decoration",panel,458,164,948,660);
            Box("Header rule",panel,48,130,1824,1,new Color(.41f,.47f,.51f,.31f));
            var back=StyledButton("돌아가기",panel,48,48,145,58,Close,plain:true);
            back.GetComponentInChildren<TMP_Text>().rectTransform.anchoredPosition=new Vector2(44,-5);
            Art("Back",back.transform,0,14,30,30);
            Box("Header separator",panel,204,61,1,30,Hex("344554"));
            Art("Crest",panel,235,56,39,39);
            title=Text("왕국 보급소",panel,302,53,850,54,32,Foreground);
            var money=Box("Supply seal",panel,1552,48,320,58,new Color(.047f,.078f,.118f,.85f));
            Border(money.transform,320,58,Hex("485664")); TicketIcon(money.transform,new Vector2(20,-8),42);
            Text("보급 인장",money.transform,70,12,130,36,22,Hex("BDCBD2"));
            currency=Text("0",money.transform,203,8,96,44,30,Foreground); currency.alignment=TextAlignmentOptions.MidlineRight;
            listTitle=Text("보급 장비",panel,48,163,250,42,28,Foreground);
            listCount=Text("",panel,300,172,118,32,18,Muted); listCount.alignment=TextAlignmentOptions.TopRight;
            var viewport=Rect("Equipment List",panel,new Vector2(48,-212),new Vector2(370,620));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal=false; scroll.movementType=ScrollRect.MovementType.Clamped; scroll.viewport=viewport;
            var hit=viewport.gameObject.AddComponent<Image>(); hit.color=Color.clear;
            listContent=Rect("Rows",viewport,Vector2.zero,new Vector2(370,620)); scroll.content=listContent;
            for(int i=0;i<catalog.items.Length;i++)
            {
                var item=catalog.items[i];
                var b=StyledButton("",listContent,0,i*78,370,74,()=>Select(item)); rows.Add(b);
                var thumb=Rect("Existing weapon thumbnail",b.transform,new Vector2(8,-6),new Vector2(64,62)).gameObject.AddComponent<RawImage>();
                thumb.raycastTarget=false; thumbnails.Add(thumb);
                var selection=Rect("Selected marker",b.transform,Vector2.zero,new Vector2(370,74));
                Box("Gold edge",selection,0,0,3,74,Hex("E3D1A3")); Art("CrestSmall",selection,340,44,17,17);rowSelection.Add(selection.gameObject);
                var badge=Box("Grade badge",b.transform,84,12,23,23,new Color(.2f,.25f,.3f,.4f));Border(badge.transform,23,23,GradeColor(item.grade));
                Text(item.grade.ToString(),badge.transform,0,1,23,22,15,GradeColor(item.grade)).alignment=TextAlignmentOptions.Top;
                var t=b.GetComponentInChildren<TMP_Text>(); t.rectTransform.anchoredPosition=new Vector2(115,-12); t.rectTransform.sizeDelta=new Vector2(243,57); t.fontSize=19;
                t.alignment=TextAlignmentOptions.TopLeft; t.overflowMode=TextOverflowModes.Ellipsis; itemLabels.Add(t);
            }
            Text("미보유 장비도 관찰할 수 있습니다.",panel,48,846,370,32,17,Muted);
            Text("/",panel,470,169,15,32,26,Hex("C3AA70")); Text("장비 관찰",panel,495,175,200,30,18,Muted);
            // Keep the original live 3D model, transparent rendering, drag and wheel behavior.
            var preview=Rect("3D Item • drag to rotate",panel,new Vector2(480,-150),new Vector2(830,650));
            preview.gameObject.AddComponent<RawImage>(); inspect=preview.gameObject.AddComponent<ItemInspectView>();
            Art("Rotate",panel,553,815,24,24); Text("드래그하여 회전 · 휠로 확대/축소",panel,589,814,465,36,18,Hex("BDCBD2"));
            var reset=StyledButton("시점 초기화",panel,1185,804,150,42,()=>inspect.ResetView(),plain:true);
            reset.GetComponentInChildren<TMP_Text>().fontSize=18; reset.GetComponentInChildren<TMP_Text>().rectTransform.anchoredPosition=new Vector2(28,-5); Art("Reset",reset.transform,0,12,18,18);
            var gradeBadge=Box("Detail grade badge",panel,1432,251,42,42,new Color(.4f,.35f,.2f,.09f));Border(gradeBadge.transform,42,42,Gold);
            gradeText=Text("",panel,1432,251,440,48,27,Gold);
            Box("Grade divider",panel,1645,271,98,1,new Color(.76f,.67f,.44f,.36f));
            itemName=Text("",panel,1432,320,440,160,59,Foreground); itemName.fontStyle=FontStyles.Bold;
            itemName.enableAutoSizing=true; itemName.fontSizeMin=42; itemName.fontSizeMax=59;
            detail=Text("",panel,1432,493,440,105,24,Hex("BDCBD2"));
            Art("Eye",panel,1432,608,22,22); ownership=Text("",panel,1464,603,408,44,22,Muted);
            Box("Detail divider",panel,1432,678,48,2,Hex("C3AA70"));
            notice=Text("",panel,1432,702,440,112,22,Muted);
            equip=StyledButton("장착",panel,1432,802,440,58,()=>
            { if(Available && !VillageMode && selected!=null) EquipmentInventory.Equip(selected.id==EquipmentInventory.EquippedId?null:selected.id); });
            Box("Action rule",panel,48,888,1824,1,new Color(.41f,.47f,.51f,.31f));
            var probability=StyledButton("확률 안내",panel,48,916,166,54,()=>{RefreshDesign();probabilityPanel.SetActive(true);});
            probability.GetComponentInChildren<TMP_Text>().rectTransform.anchoredPosition=new Vector2(49,-5); Art("Info",probability.transform,24,16,21,21);
            var owned=StyledButton("보유 장비",panel,230,916,166,54,()=>{ownedOnly=!ownedOnly;RefreshDesign();});
            owned.GetComponentInChildren<TMP_Text>().rectTransform.anchoredPosition=new Vector2(49,-5); Art("Backpack",owned.transform,24,16,21,21);
            Text("관찰 중인 장비와 관계없이 전체 장비 풀에서 무작위 보급됩니다.",panel,48,1003,1000,34,18,Muted);
            pity=Text("",panel,1152,846,720,34,21,Gold);
            one=StyledButton("1회 보급",panel,1152,888,348,90,()=>Draw(1));
            ten=StyledButton("10회 보급",panel,1524,888,348,90,()=>Draw(10),gold:true);
            one.gameObject.AddComponent<TutorialTarget>().SetId("SupplyDrawOne");
            Cost(one,1); Cost(ten,10);
            shortageOne=Text("",panel,1152,988,348,40,19,Hex("D58088")); shortageOne.alignment=TextAlignmentOptions.Top;
            shortageTen=Text("",panel,1524,988,348,40,19,Hex("D58088")); shortageTen.alignment=TextAlignmentOptions.Top;
            BuildProbability(panel); BuildResults(panel);
            modal.SetActive(false); canvasRoot.SetActive(false);
        }
        void Cost(Button b,int count)
        {
            var t=b.GetComponentInChildren<TMP_Text>(); t.rectTransform.anchoredPosition=new Vector2(26,-5);t.rectTransform.sizeDelta=new Vector2(205,80);
            TicketIcon(b.transform,new Vector2(245,-28),34);
            var cost=Text("×"+count,b.transform,285,26,60,40,23,count==10?Hex("181C21"):Foreground);
            cost.alignment=TextAlignmentOptions.MidlineLeft;
        }
        void BuildProbability(Transform parent)
        {
            var veil=Box("Probability Modal",parent,0,0,1920,1080,new Color(0,.02f,.04f,.8f)); veil.raycastTarget=true; probabilityPanel=veil.gameObject;
            var card=Box("Probability Card",veil.transform,564,172,792,668,Hex("121F2B")); Border(card.transform,792,668,Hex("53616C"));
            Art("Crest",card.transform,44,47,30,30); Text("확률 안내",card.transform,90,43,500,50,32,Foreground);
            StyledButton("닫기 ×",card.transform,660,40,100,50,()=>probabilityPanel.SetActive(false),plain:true);
            Box("Divider",card.transform,44,112,704,1,Hex("344554"));
            Text("전체 장비 풀의 등급별 기본 보급 확률입니다.",card.transform,44,144,704,44,24,Hex("BDCBD2"));
            Box("Table heading",card.transform,44,198,704,46,Hex("1B2B38"));
            Text("장비 등급",card.transform,64,208,300,30,18,Muted); Text("기본 확률",card.transform,618,208,130,30,18,Muted);
            rates=Text("",card.transform,64,260,650,258,26,Foreground);
            Text("기본 확률이며 보장 적용 회차에는 달라질 수 있습니다.",card.transform,44,540,704,42,21,Hex("BDCBD2"));
            Text("보장 횟수는 보급 화면의 남은 횟수에 표시됩니다.",card.transform,44,601,704,35,20,Gold);
            probabilityPanel.SetActive(false);
        }
        void BuildResults(Transform parent)
        {
            var veil=Box("Supply Results",parent,0,0,1920,1080,new Color(0,.02f,.04f,.94f)); veil.raycastTarget=true; resultsPanel=veil.gameObject;
            Text("보급 완료",veil.transform,160,170,1600,85,52,Gold).alignment=TextAlignmentOptions.Top;
            Text("장비를 선택하면 3D로 관찰할 수 있습니다.",veil.transform,160,260,1600,50,24,Muted).alignment=TextAlignmentOptions.Top;
            for(int i=0;i<10;i++)
            {
                var b=StyledButton("",veil.transform,160+i%5*324,350+i/5*180,300,152,()=>{});
                b.GetComponentInChildren<TMP_Text>().fontSize=24; resultButtons.Add(b);
            }
            StyledButton("확인",veil.transform,800,795,320,70,()=>resultsPanel.SetActive(false),gold:true);
            resultsPanel.SetActive(false);
        }
        void ShowResultPanel(List<EquipmentItem> rewards)
        {
            for(int i=0;i<resultButtons.Count;i++)
            {
                var b=resultButtons[i]; b.gameObject.SetActive(i<rewards.Count); if(i>=rewards.Count) continue;
                var item=rewards[i]; b.GetComponentInChildren<TMP_Text>().text=$"<color=#{ColorUtility.ToHtmlStringRGB(GradeColor(item.grade))}>{item.grade}급 장비</color>\n{item.displayName}";
                b.onClick.RemoveAllListeners(); b.onClick.AddListener(()=>{resultsPanel.SetActive(false);Select(item);});
            }
            resultsPanel.SetActive(true);
        }
        void HideOverlays() { if(probabilityPanel)probabilityPanel.SetActive(false);if(resultsPanel)resultsPanel.SetActive(false); }
        bool DismissOverlay() { if(probabilityPanel.activeSelf || resultsPanel.activeSelf) {HideOverlays();return true;}return false; }
        static Color GradeColor(ItemGrade grade) => grade==ItemGrade.S ? Hex("D4B979") : grade==ItemGrade.A ? Hex("B399D4") : grade==ItemGrade.B ? Hex("77B9B7") : Hex("A5ADB5");
        void RefreshDesign()
        {
            if(catalog==null || currency==null)return;
            var equipped=catalog.Find(EquipmentInventory.EquippedId);
            if(!VillageMode)shortcutText.text="장비 1칸 · "+(equipped!=null?equipped.displayName:"비어 있음");
            title.text=TestMode?"왕국 보급소  /  TEST · 저장 제외":VillageMode?"왕국 보급소":"출전 준비 · 지휘관 장비";
            currency.text=EquipmentInventory.Tickets.ToString();
            one.gameObject.SetActive(VillageMode);ten.gameObject.SetActive(VillageMode);
            one.interactable=CanDraw && HasTickets(1) && !IsDrawing;ten.interactable=CanDraw && HasTickets(10) && !IsDrawing;
            one.GetComponentInChildren<TMP_Text>().color=one.interactable?Foreground:Muted;
            ten.GetComponentInChildren<TMP_Text>().color=ten.interactable?Hex("181C21"):Muted;
            foreach(var t in one.GetComponentsInChildren<TMP_Text>()) t.color=one.interactable?Foreground:Muted;
            foreach(var t in ten.GetComponentsInChildren<TMP_Text>()) t.color=ten.interactable?Hex("181C21"):Muted;
            shortageOne.text=VillageMode&&!HasTickets(1)?$"보급 인장 {1-EquipmentInventory.Tickets}개 부족":"";
            shortageTen.text=VillageMode&&!HasTickets(10)?$"보급 인장 {10-EquipmentInventory.Tickets}개 부족":"";
            pity.text=VillageMode?$"<color=#B399D4>{Mathf.Max(1,catalog.aGuarantee-EquipmentInventory.SinceA)}회 이내 A급 이상 보장</color><pos=400>{(catalog.HasSRewards?$"{Mathf.Max(1,catalog.sGuarantee-EquipmentInventory.SinceS)}회 이내 S급 보장":"S급 미등록")}":"";
            rates.text=$"<color=#D4B979>S급 장비</color><pos=550>{catalog.sRate:P1}\n\n<color=#B399D4>A급 장비</color><pos=550>{catalog.aRate:P1}\n\n<color=#77B9B7>B급 장비</color><pos=550>{catalog.bRate:P1}\n\n<color=#A5ADB5>C급 장비</color><pos=550>{catalog.CRate:P1}";
            int count=0;
            for(int i=0;i<rows.Count;i++)
            {
                var item=catalog.items[i];int owned=EquipmentInventory.Count(item.id);bool show=!ownedOnly||owned>0;
                rows[i].gameObject.SetActive(show);if(!show)continue;
                ((RectTransform)rows[i].transform).anchoredPosition=new Vector2(0,-count++*78);
                var c=rows[i].colors;c.normalColor=item==selected?Hex("283744"):new Color(.063f,.102f,.141f,.78f);rows[i].colors=c;
                rowSelection[i].SetActive(item==selected);
                itemLabels[i].text=$"{item.displayName}\n<size=16><color=#81929F>{(owned>0?$"보유 {owned}개":"미보유")}{(item.id==EquipmentInventory.EquippedId?" · 장착 중":"")}</color></size>";
            }
            listContent.sizeDelta=new Vector2(370,Mathf.Max(620,count*78));listTitle.text=ownedOnly?"보유 장비":"보급 장비";listCount.text=$"전체 {count}종";
            if(selected==null)return;
            gradeText.text=$"<pos=12>{selected.grade}<pos=56><size=24>{selected.grade}급 장비</size>";gradeText.color=GradeColor(selected.grade);
            itemName.text=selected.displayName; detail.text=selected.description;
            ownership.text=EquipmentInventory.Count(selected.id)>0?$"보유 {EquipmentInventory.Count(selected.id)}개":"미보유 · 미리보기";
            notice.text=VillageMode?"획득한 장비는 출전 준비에서\n장착할 수 있습니다.":"장착 상태는 저장 슬롯에 기록됩니다.\n현재 장비 능력치는 적용되지 않습니다.";
            equip.gameObject.SetActive(!VillageMode);equip.interactable=Available&&EquipmentInventory.Count(selected.id)>0;
            equip.GetComponentInChildren<TMP_Text>().text=selected.id==EquipmentInventory.EquippedId?"장착 해제":"지휘관 공용 슬롯에 장착";
        }
        void EnsureThumbnails()
        {
            for(int i=0;i<catalog.items.Length;i++)
            {
                var item=catalog.items[i];
                if(!thumbnailCache.TryGetValue(item,out var texture))
                {
                    var source=new GameObject("Thumbnail Capture",typeof(RectTransform),typeof(RawImage),typeof(ItemInspectView));
                    source.transform.SetParent(modal.transform,false);
                    var view=source.GetComponent<ItemInspectView>(); view.Show(item);
                    var render=source.GetComponent<RawImage>().texture as RenderTexture;
                    if(render!=null)
                    {
                        var previous=RenderTexture.active;
                        try {RenderTexture.active=render;texture=new Texture2D(render.width,render.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,render.width,render.height),0,0);texture.Apply();thumbnailCache[item]=texture;}
                        finally {RenderTexture.active=previous;}
                    }
                    source.SetActive(false);Destroy(source);
                }
                thumbnails[i].texture=texture; thumbnails[i].enabled=texture!=null;
            }
        }
    }
}
