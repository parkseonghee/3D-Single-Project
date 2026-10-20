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
    // Safe to run again: existing ScriptableObjects are left editable and target components are reused.
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
            if (village.steps == null || village.steps.Length == 0)
            {
                village.tutorialId = "first_village";
                village.startEventKey = "VillageEntered";
                village.finishEventKey = "BattleStarted";
                village.steps = new[]
                {
                    Step("VillageResources", "마을의 자원", "벼는 건설과 고용에 쓰고, 골드는 성장에 사용합니다.", TutorialAdvance.NextButton),
                    Step("OpenBuildingMenu", "건설 메뉴", "건설 메뉴를 열어 건물을 선택하세요.", TutorialAdvance.TargetClick),
                    Step("WorkshopButton", "병영 건설", "건설 메뉴에서 병영을 선택하고 빈 땅에 배치하세요.", TutorialAdvance.Event, "BuildingBuilt:Workshop"),
                    Step("VillageReady", "전투 준비", "마을의 준비 버튼을 눌러 고용 화면으로 이동하세요.", TutorialAdvance.Event, "PreparationEntered"),
                    Step("SoldierHire", "병사 고용", "병사 고용 버튼을 5번 눌러 출전 병사 5명을 준비하세요.", TutorialAdvance.Event, "SoldierHired"),
                    Step("SoldierHire", "부대 편성", "병사 카드의 -를 눌러 대기시키고 +로 다시 편성해 보세요.", TutorialAdvance.Event, "FormationChanged"),
                    Step("StartRun", "출전", "편성이 끝났으면 출전 버튼을 누르세요.", TutorialAdvance.TargetClick)
                };
                village.steps[village.steps.Length - 1].blockOutsideClicks = false;
                village.steps[2].hideWhenTargetMissing = true;
                village.steps[4].requiredEventCount = 5;
                EditorUtility.SetDirty(village);
            }
            TutorialSequence battle = LoadOrCreate<TutorialSequence>("FirstBattle");
            if (battle.steps == null || battle.steps.Length == 0)
            {
                battle.tutorialId = "first_battle";
                battle.startEventKey = "BattleStarted";
                battle.cancelEventKey = "BattleEnded";
                battle.cancelOnCommanderDeath = true;
                battle.steps = new[]
                {
                    Step("Commander", "지휘관 조작", "WASD로 지휘관을 움직여 부대를 이끕니다. 화면을 클릭하면 전투가 시작됩니다.", TutorialAdvance.NextButton),
                    Step("Commander", "지휘관 이동", "WASD로 지휘관을 움직여 보세요. 전투 중에도 적을 우클릭하면 집중 공격할 수 있습니다.", TutorialAdvance.CommanderDistance),
                    Step("Soldier", "자동 전투", "병사들은 가까운 적을 자동으로 공격합니다.", TutorialAdvance.NextButton),
                    Step("ExpBottle", "경험치 병", "몬스터가 떨어뜨린 경험치 병을 주우세요.", TutorialAdvance.Event, "ExpBottleCollected"),
                    Step("SoldierPortrait", "경험치 투자", "경험치 병을 얻으면 Ctrl+1~5로 해당 번호의 병사에게 투자하세요. 경험치가 차면 레벨업합니다.", TutorialAdvance.Event, "ExpBottleInvested")
                };
                battle.steps[0].pauseGame = true;
                battle.steps[0].allowKeyboard = false;
                battle.steps[1].commanderDistance = 2f;
                battle.steps[1].blockOutsideClicks = false;
                battle.steps[2].pauseGame = true;
                battle.steps[2].allowKeyboard = false;
                battle.steps[3].holeShape = TutorialHoleShape.Circle;
                battle.steps[3].blockOutsideClicks = false;
                battle.steps[4].blockOutsideClicks = false;
                EditorUtility.SetDirty(battle);
            }
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
            ConfigureVillage(style, village, library);
            ConfigureBattle(style, village, battle, library, focus);
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

        private static void ConfigureVillage(TutorialStyle style, TutorialSequence village, TutorialSequence library)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/VillageScene.unity", OpenSceneMode.Single);
            ConfigureManager(Find(scene, "Building UI"), style, "VillageEntered", new[] { village, library }, village,
                Find(scene, "Village Camera"), null);
            Target(scene, "Village Top Bar", "VillageResources");
            Target(scene, "Open Building Menu", "OpenBuildingMenu");
            Target(scene, "Workshop Button", "WorkshopButton");
            Target(scene, "Ready Button", "VillageReady");
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBattle(TutorialStyle style, TutorialSequence village, TutorialSequence battle,
            TutorialSequence library, TutorialSequence focus)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/PlayScene.unity", OpenSceneMode.Single);
            Transform system = Find(scene, "Recruitment System");
            ConfigureManager(Find(scene, "Recruitment UI"), style, null,
                new[] { village, battle, library, focus }, battle, Find(scene, "Play Camera"), system);
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
