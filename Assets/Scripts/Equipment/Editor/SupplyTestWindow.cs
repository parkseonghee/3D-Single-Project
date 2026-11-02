using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArmySurvivor.Equipment.Editor
{
    public sealed class SupplyTestWindow : EditorWindow
    {
        const string Pending = "SupplyTest.PendingStart";
        const string VillagePath = "Assets/Scenes/VillageScene.unity";
        EquipmentUI UI => FindObjectsByType<EquipmentUI>(FindObjectsSortMode.None) is var all
            ? System.Array.Find(all, x => x.gameObject.scene == SceneManager.GetActiveScene()) : null;

        [MenuItem("Army Survivor/Equipment/Open Draw Test Window")]
        public static void OpenWindow() => GetWindow<SupplyTestWindow>("보급 뽑기 테스트");

        void OnEnable()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayState;
        }
        void OnDisable()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= PlayState;
        }
        void PlayState(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                SessionState.SetBool(Pending, false);
                EquipmentInventory.EndTest();
            }
        }
        void Tick()
        {
            if (SessionState.GetBool(Pending, false) && EditorApplication.isPlaying && !EditorApplication.isCompiling)
            {
                var ui = UI;
                if (ui != null && SceneManager.GetActiveScene().name == "VillageScene")
                {
                    SessionState.SetBool(Pending, false);
                    EquipmentInventory.BeginTest();
                    ui.Open();
                }
            }
            if (EditorApplication.isPlaying) Repaint();
        }
        void StartVillage()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(VillagePath);
            SessionState.SetBool(Pending, true);
            EditorApplication.isPlaying = true;
        }
        void OnGUI()
        {
            EditorGUILayout.LabelField("왕국 보급소 테스트", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("실제 보급 UI와 성문 연출을 사용합니다. 테스트 장비/인장은 저장에서 제외되며, 테스트 종료 시 이전 장비 상태로 복구됩니다.", MessageType.Info);
            if (!EditorApplication.isPlaying)
            {
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                    if (GUILayout.Button("VillageScene에서 테스트 시작", GUILayout.Height(34))) StartVillage();
                EditorGUILayout.LabelField("수정한 씬이 있으면 저장 여부를 먼저 묻습니다.");
                return;
            }
            var ui = UI;
            if (SceneManager.GetActiveScene().name != "VillageScene" || ui == null)
            {
                EditorGUILayout.HelpBox("Play Mode를 종료한 뒤 위 시작 버튼을 사용하세요. VillageScene 초기화 중이면 잠시 기다려 주세요.", MessageType.Warning);
                return;
            }
            if (!EquipmentInventory.IsTesting)
            {
                if (GUILayout.Button("테스트 모드 시작 + 보급 UI 열기")) { EquipmentInventory.BeginTest(); ui.Open(); }
                return;
            }
            EditorGUILayout.LabelField($"인장 {EquipmentInventory.Tickets} | A 누적 {EquipmentInventory.SinceA} | S 누적 {EquipmentInventory.SinceS}");
            EditorGUILayout.LabelField(ui.IsDrawing ? "성문 연출 재생 중 · Game 창에서 ESC로 건너뛰기" : "테스트 준비 완료");
            using (new EditorGUI.DisabledScope(ui.IsDrawing))
            {
                bool unlimited = EditorGUILayout.Toggle("무제한 인장", EquipmentInventory.UnlimitedTestTickets);
                if (unlimited != EquipmentInventory.UnlimitedTestTickets)
                {
                    EquipmentInventory.UnlimitedTestTickets = unlimited;
                    Configure(EquipmentInventory.Tickets, EquipmentInventory.SinceA, EquipmentInventory.SinceS);
                }
                if (GUILayout.Button("보급 UI 다시 열기")) ui.Open();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("1회 뽑기 + 연출")) { ui.Open(); ui.Draw(1); }
                if (GUILayout.Button("10회 뽑기 + 연출")) { ui.Open(); ui.Draw(10); }
                EditorGUILayout.EndHorizontal();
                var catalog = Resources.Load<EquipmentCatalog>("EquipmentCatalog");
                using (new EditorGUI.DisabledScope(catalog == null))
                {
                    if (GUILayout.Button("A급 이상 보장 직전")) Configure(1000, catalog.aGuarantee - 1, 0);
                    if (GUILayout.Button("S급 보장 직전")) Configure(1000, 0, catalog.sGuarantee - 1);
                }
                if (GUILayout.Button("인장 0개 · 부족 상태 확인"))
                {
                    EquipmentInventory.UnlimitedTestTickets = false;
                    Configure(0, EquipmentInventory.SinceA, EquipmentInventory.SinceS);
                    ui.Open();
                }
                if (GUILayout.Button("인장 1,000개 · 보장 카운터 초기화")) Configure(1000, 0, 0);
                if (GUILayout.Button("테스트 종료 · 이전 장비 상태 복구")) { ui.Close(); EquipmentInventory.EndTest(); }
            }
        }
        static void Configure(int tickets, int a, int s) => EquipmentInventory.ConfigureTest(tickets, a, s);
    }
}
