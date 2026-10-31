#if UNITY_EDITOR
using System;
using ArmySurvivor.Army;
using ArmySurvivor.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArmySurvivor.Editor
{
    // Safe to run again: the onboarding sequences are refreshed and target components are reused.
    public static class TutorialSceneSetup
    {
        private const string Folder = "Assets/TutorialData";

        [MenuItem("Army Survivor/Tutorial/Install Example Sequences")]
        public static void Install()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "TutorialData");
            TutorialStyle style = LoadOrCreate<TutorialStyle>("TutorialStyle");
            if (style.spotlightShader == null)
            {
                style.spotlightShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/TutorialSpotlight.shader");
                EditorUtility.SetDirty(style);
            }
            if (style.font == null)
            {
                style.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/KMU Sungkok Serif SDF.asset");
                EditorUtility.SetDirty(style);
            }
            TutorialSequence village = LoadOrCreate<TutorialSequence>("FirstVillage");
            {
                village.tutorialId = "first_village_supply";
                village.startEventKey = "VillageEntered";
                village.finishEventKey = "BattleStarted";
                village.steps = new[]
                {
                    Step("VillageResources", "마을의 자원", "벼는 건설과 고용에 쓰고, 골드는 성장에 사용합니다.", TutorialAdvance.NextButton),
                    Step("OpenBuildingMenu", "건설 메뉴", "건설 메뉴를 열어 건물을 선택하세요.", TutorialAdvance.TargetClick),
                    Step("WorkshopButton", "병영 건설", "건설 메뉴에서 병영을 선택하고 빈 땅에 배치하세요.", TutorialAdvance.Event, "BuildingBuilt:Workshop"),
                    Step("SupplyDepotButton", "왕국 보급소 건설", "왕국 보급소를 선택하고 빈 땅에 배치하세요. 전투에서 얻은 인장으로 장비를 보급받을 수 있습니다.", TutorialAdvance.Event, "BuildingBuilt:SupplyDepot"),
                    Step("VillageReady", "전투 준비", "마을의 준비 버튼을 눌러 고용 화면으로 이동하세요.", TutorialAdvance.Event, "PreparationEntered"),
                    Step("SoldierHire", "병사 고용", "병사 고용 버튼을 5번 눌러 출전 병사 5명을 준비하세요.", TutorialAdvance.Event, "SoldierHired"),
                    Step("StartRun", "출전", "편성이 끝났으면 출전 버튼을 누르세요.", TutorialAdvance.TargetClick)
                };
                village.steps[village.steps.Length - 1].blockOutsideClicks = false;
                village.steps[2].hideWhenTargetMissing = true;
                village.steps[3].blockOutsideClicks = false;
                village.steps[3].showAsTopBanner = true;
                var equipmentCatalog = AssetDatabase.LoadAssetAtPath<ArmySurvivor.Equipment.EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
                village.steps[3].revealBuilding = equipmentCatalog != null ? equipmentCatalog.supplyBuilding : null;
                village.steps[5].requiredEventCount = 5;
                EditorUtility.SetDirty(village);
            }
            TutorialSequence battle = LoadOrCreate<TutorialSequence>("FirstBattle");
            {
                battle.tutorialId = "first_battle_supply";
                battle.startEventKey = "BattleStarted";
                battle.cancelEventKey = "BattleEnded";
                battle.cancelOnCommanderDeath = true;
                battle.steps = new[]
                {
                    Step("Commander", "지휘관 이동", "WASD로 지휘관을 움직여 보세요. 전투 중에도 적을 우클릭하면 집중 공격할 수 있습니다.", TutorialAdvance.CommanderDistance),
                    Step("Soldier", "자동 전투", "병사들은 가까운 적을 자동으로 공격합니다.", TutorialAdvance.NextButton),
                    Step("ExpBottle", "경험치 병", "몬스터가 떨어뜨린 경험치 병을 주우세요.", TutorialAdvance.Event, "ExpBottleCollected"),
                    Step("SoldierPortrait", "경험치 투자", "경험치 병을 얻으면 Ctrl+1~5로 해당 번호의 병사에게 투자하세요. 경험치가 차면 레벨업합니다.", TutorialAdvance.Event, "ExpBottleInvested")
                };
                battle.steps[0].commanderDistance = 2f;
                battle.steps[0].pauseGame = true;
                battle.steps[0].allowCommanderMovementWhilePaused = true;
                battle.steps[0].blockOutsideClicks = false;
                battle.steps[1].pauseGame = true;
                battle.steps[1].allowKeyboard = false;
                battle.steps[2].holeShape = TutorialHoleShape.Circle;
                battle.steps[2].blockOutsideClicks = false;
                battle.steps[3].pauseGame = true;
                battle.steps[3].blockOutsideClicks = false;
                EditorUtility.SetDirty(battle);
            }
            TutorialSequence ticket = LoadOrCreate<TutorialSequence>("FirstTicket");
            ticket.tutorialId = "first_ticket";
            ticket.startEventKey = "TicketDropped";
            ticket.interruptCurrent = true;
            ticket.cancelEventKey = "BattleEnded";
            ticket.cancelOnCommanderDeath = true;
            ticket.steps = new[]
            {
                Step("DrawTicket", "뽑기권 · 인장", "몬스터가 인장을 떨어뜨렸습니다. 인장으로 왕국 보급소에서 장비를 뽑을 수 있습니다. 화면을 클릭해 계속하세요.", TutorialAdvance.NextButton),
                Step("DrawTicket", "인장 획득", "인장 근처로 이동해 획득하세요.", TutorialAdvance.Event, "TicketCollected")
            };
            ticket.steps[0].pauseGame = true;
            ticket.steps[0].allowKeyboard = false;
            ticket.steps[0].holeShape = TutorialHoleShape.Circle;
            ticket.steps[1].blockOutsideClicks = false;
            ticket.steps[1].holeShape = TutorialHoleShape.Circle;
            EditorUtility.SetDirty(ticket);
            TutorialSequence draw = LoadOrCreate<TutorialSequence>("FirstSupplyDraw");
            draw.tutorialId = "first_supply_draw";
            draw.startEventKey = "FirstSupplyDrawUnlocked";
            draw.steps = new[]
            {
                Step("SupplyDepotEntry", "왕국 보급소", "Day 1을 마쳤습니다. 왕국 보급소를 열어 전투에서 얻은 인장을 사용해 보세요.", TutorialAdvance.Event, "SupplyOpened"),
                Step("SupplyDrawOne", "장비 보급", "1회 보급을 눌러 첫 장비를 뽑으세요.", TutorialAdvance.Event, "EquipmentDrawn"),
                Step(null, "획득한 장비", "보급받은 장비는 다음 출전의 준비 화면에서 장착할 수 있습니다.", TutorialAdvance.NextButton)
            };
            draw.steps[0].blockOutsideClicks = false;
            draw.steps[1].blockOutsideClicks = false;
            EditorUtility.SetDirty(draw);
            TutorialSequence library = LoadOrCreate<TutorialSequence>("FirstLibrary");
            if (library.steps == null || library.steps.Length == 0)
            {
                library.tutorialId = "first_library";
                library.startEventKey = "BuildingBuilt:Library";
                library.interruptCurrent = true;
                library.steps = new[]
                {
                    Step("Building:Library", "도서관", "도서관을 지으면 출전 전에 교범을 고를 수 있습니다.", TutorialAdvance.NextButton),
                    Step("LibraryManualDraft", "교범 선택", "출전마다 교범 하나를 골라 부대 규칙을 바꾸세요.", TutorialAdvance.Event, "ManualSelected")
                };
                library.steps[0].holeShape = TutorialHoleShape.Circle;
                library.steps[1].blockOutsideClicks = false;
                EditorUtility.SetDirty(library);
            }
            TutorialSequence focus = LoadOrCreate<TutorialSequence>("FirstEliteFocus");
            if (focus.steps == null || focus.steps.Length == 0)
            {
                focus.tutorialId = "first_elite_focus";
                focus.startEventKey = "EliteSpawned";
                focus.interruptCurrent = true;
                focus.startTargetId = "EliteEnemy";
                focus.startDistanceFromCommander = 12f;
                focus.cancelEventKey = "BattleEnded";
                focus.cancelOnCommanderDeath = true;
                focus.jumpEventKey = "EliteKilled";
                focus.requireJumpTargetMatch = true;
                focus.cancelIfJumpBeforeStart = true;
                focus.jumpToStep = 2;
                focus.steps = new[]
                {
                    Step("EliteEnemy", "정예 몬스터 집중 공격", "정예 몬스터를 마우스 오른쪽 버튼으로 클릭하세요. 병사들이 이 적을 우선 공격합니다.", TutorialAdvance.Event, "FocusTargetSet"),
                    Step("FocusMarker", "집중 공격 중", "표시된 정예 몬스터를 부대가 집중 공격하고 있습니다. 처치하거나 잠시 지켜보세요.", TutorialAdvance.Event, "FocusTargetKilled"),
                    Step(null, "집중 공격 해제", "빈 땅을 마우스 왼쪽 버튼으로 클릭하면 집중 공격이 해제되고 병사들이 다시 주변 적을 공격합니다.", TutorialAdvance.Event, "FocusTargetCleared")
                };
                focus.steps[0].requireEventTargetMatch = true;
                focus.steps[0].holeShape = TutorialHoleShape.Circle;
                focus.steps[1].holeShape = TutorialHoleShape.Circle;
                focus.steps[1].fallbackSeconds = 6f;
                focus.steps[2].bubblePlacement = TutorialBubblePlacement.Below;
                focus.steps[2].showNextButtonAfterJump = true;
                focus.steps[2].showNextWhenNoFocus = true;
                foreach (TutorialStep step in focus.steps)
                {
                    step.enemyMovementMultiplier = 0.3f;
                    step.blockOutsideClicks = true;
                    step.pauseGame = false;
                }
                focus.steps[2].blockOutsideClicks = false;
                EditorUtility.SetDirty(focus);
            }

            AssetDatabase.SaveAssets();
            // Saving newly created sequence assets may replace their in-memory instances.
            // Resolve persistent references again before serializing scene components.
            village = AssetDatabase.LoadAssetAtPath<TutorialSequence>(Folder + "/FirstVillage.asset");
            battle = AssetDatabase.LoadAssetAtPath<TutorialSequence>(Folder + "/FirstBattle.asset");
            library = AssetDatabase.LoadAssetAtPath<TutorialSequence>(Folder + "/FirstLibrary.asset");
            focus = AssetDatabase.LoadAssetAtPath<TutorialSequence>(Folder + "/FirstEliteFocus.asset");
            ticket = AssetDatabase.LoadAssetAtPath<TutorialSequence>(Folder + "/FirstTicket.asset");
            draw = AssetDatabase.LoadAssetAtPath<TutorialSequence>(Folder + "/FirstSupplyDraw.asset");
            ConfigureVillage(style, village, library, draw);
            ConfigureBattle(style, village, battle, library, focus, ticket);
            ConfigureStart(style.font);
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Single);
            Debug.Log("튜토리얼 예시 데이터와 씬 연결 완료");
        }

        private static TutorialStep Step(string target, string title, string body, TutorialAdvance advance, string eventKey = null)
        {
            return new TutorialStep { targetId = target, title = title, body = body, advance = advance,
                eventKey = eventKey, blockOutsideClicks = advance == TutorialAdvance.NextButton ||
                    advance == TutorialAdvance.TargetClick };
        }

        private static T LoadOrCreate<T>(string name) where T : ScriptableObject
        {
            string path = Folder + "/" + name + ".asset";
            T data = AssetDatabase.LoadAssetAtPath<T>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static Transform Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            return null;
        }

        private static void Target(Scene scene, string objectName, string id)
        {
            Transform transform = Find(scene, objectName);
            if (transform == null) { Debug.LogWarning("튜토리얼 대상 없음: " + scene.name + "/" + objectName); return; }
            TutorialTarget target = transform.GetComponent<TutorialTarget>();
            if (target == null) target = transform.gameObject.AddComponent<TutorialTarget>();
            SerializedObject so = new SerializedObject(target);
            so.FindProperty("targetId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(transform.gameObject.scene);
        }

        private static void ConfigureManager(Transform host, TutorialStyle style, string entryEvent,
            TutorialSequence[] sequences, TutorialSequence debug, Transform camera, Transform run)
        {
            if (host == null) throw new InvalidOperationException("튜토리얼 호스트가 없습니다.");
            TutorialManager manager = host.GetComponent<TutorialManager>();
            if (manager == null) manager = host.gameObject.AddComponent<TutorialManager>();
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("style").objectReferenceValue = style;
            so.FindProperty("sceneEntryEvent").stringValue = entryEvent;
            so.FindProperty("debugSequence").objectReferenceValue = debug;
            so.FindProperty("worldCamera").objectReferenceValue = camera != null ? camera.GetComponent<Camera>() : null;
            so.FindProperty("run").objectReferenceValue = run != null ? run.GetComponent<RunController>() : null;
            so.FindProperty("combat").objectReferenceValue = run != null ? run.GetComponent<CombatController>() : null;
            SerializedProperty array = so.FindProperty("sequences");
            array.arraySize = sequences.Length;
            for (int i = 0; i < sequences.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = sequences[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(host.gameObject.scene);
        }

        private static void ConfigureVillage(TutorialStyle style, TutorialSequence village, TutorialSequence library, TutorialSequence draw)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/VillageScene.unity", OpenSceneMode.Single);
            village = LoadOrCreate<TutorialSequence>("FirstVillage");
            library = LoadOrCreate<TutorialSequence>("FirstLibrary");
            draw = LoadOrCreate<TutorialSequence>("FirstSupplyDraw");
            ConfigureManager(Find(scene, "Building UI"), style, "VillageEntered", new[] { village, library, draw }, village,
                Find(scene, "Village Camera"), null);
            Target(scene, "Village Top Bar", "VillageResources");
            Target(scene, "Open Building Menu", "OpenBuildingMenu");
            Target(scene, "Workshop Button", "WorkshopButton");
            Target(scene, "Supply Depot Button", "SupplyDepotButton");
            // HorizontalLayoutGroup controls the positions, so change sibling order rather than offsets.
            RectTransform workshop = Find(scene, "Workshop Button") as RectTransform;
            RectTransform supply = Find(scene, "Supply Depot Button") as RectTransform;
            if (supply != null)
            {
                TMP_Text label = supply.GetComponentInChildren<TMP_Text>(true);
                var catalog = AssetDatabase.LoadAssetAtPath<ArmySurvivor.Equipment.EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
                if (label != null && catalog != null && catalog.supplyBuilding != null)
                {
                    label.text = $"왕국 보급소 {catalog.supplyBuilding.riceCost} 벼";
                    label.alignment = TextAlignmentOptions.Bottom;
                    label.margin = new Vector4(10, 0, 10, 20);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
            if (workshop != null && supply != null &&
                supply.GetSiblingIndex() != workshop.GetSiblingIndex() + 1)
            {
                supply.SetSiblingIndex(workshop.GetSiblingIndex() + 1);
                EditorSceneManager.MarkSceneDirty(scene);
            }
            Target(scene, "Ready Button", "VillageReady");
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBattle(TutorialStyle style, TutorialSequence village, TutorialSequence battle,
            TutorialSequence library, TutorialSequence focus, TutorialSequence ticket)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/PlayScene.unity", OpenSceneMode.Single);
            village = LoadOrCreate<TutorialSequence>("FirstVillage");
            battle = LoadOrCreate<TutorialSequence>("FirstBattle");
            library = LoadOrCreate<TutorialSequence>("FirstLibrary");
            focus = LoadOrCreate<TutorialSequence>("FirstEliteFocus");
            ticket = LoadOrCreate<TutorialSequence>("FirstTicket");
            Transform system = Find(scene, "Recruitment System");
            ConfigureManager(Find(scene, "Recruitment UI"), style, null,
                new[] { village, battle, library, focus, ticket }, battle, Find(scene, "Play Camera"), system);
            Target(scene, "Commander", "Commander");
            Target(scene, "Soldier Card", "SoldierHire");
            Target(scene, "Start Run Button", "StartRun");
            Target(scene, "Library Manual Draft", "LibraryManualDraft");
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureStart(TMP_FontAsset font)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Single);
            Transform panel = Find(scene, "Settings Panel");
            if (panel != null)
            {
                RectTransform panelRect = panel as RectTransform;
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 500);
                Transform close = Find(scene, "Close Settings");
                if (close is RectTransform closeRect) closeRect.anchoredPosition = new Vector2(0, -195);
                TutorialOptions options = panel.GetComponent<TutorialOptions>();
                if (options == null) options = panel.gameObject.AddComponent<TutorialOptions>();
                SerializedObject so = new SerializedObject(options);
                so.FindProperty("font").objectReferenceValue = font;
                so.FindProperty("position").vector2Value = new Vector2(0, -355);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
