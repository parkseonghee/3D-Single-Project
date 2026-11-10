#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public sealed class SpecialWaveTestWindow : EditorWindow
    {
        private AdventureRhythmRule.SpecialPattern pattern;

        [MenuItem("Tools/Army Survivor/Special Wave Test")]
        private static void Open() => GetWindow<SpecialWaveTestWindow>("Special Wave Test");

        private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;
        private void OnDisable() => EditorApplication.playModeStateChanged -= OnPlayModeChanged;

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                AdventureRhythmRule.TestPatternOverride = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("모험 모드 특별 웨이브", EditorStyles.boldLabel);
            pattern = (AdventureRhythmRule.SpecialPattern)EditorGUILayout.EnumPopup("패턴", pattern);
            EditorGUILayout.HelpBox(AdventureRhythmRule.SpecialName(pattern) +
                " 웨이브. 기존 전투를 유지한 채 선택 패턴을 시험할 수 있습니다.", MessageType.Info);

            if (GUILayout.Button("다음 모험 전투 패턴 고정"))
                AdventureRhythmRule.TestPatternOverride = pattern;
            EditorGUILayout.LabelField("다음 전투", AdventureRhythmRule.TestPatternOverride.HasValue
                ? AdventureRhythmRule.SpecialName(AdventureRhythmRule.TestPatternOverride.Value) : "무작위");
            if (GUILayout.Button("고정 해제")) AdventureRhythmRule.TestPatternOverride = null;

            EditorGUILayout.Space();
            if (GUILayout.Button("16종 자동 검증")) AdventureWaveVerification.Run();
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play 후 모험 모드에 진입하면 현재 전투에도 바로 소환할 수 있습니다.", MessageType.None);
                return;
            }

            var combat = Object.FindFirstObjectByType<CombatController>();
            EditorGUILayout.LabelField(combat != null ? combat.SpecialWaveTestStatus : "전투 컨트롤러가 없습니다.");
            using (new EditorGUI.DisabledScope(combat == null))
            {
                if (GUILayout.Button("선택한 웨이브 지금 소환") && !combat.TriggerSpecialWaveForTesting(pattern))
                    Debug.LogWarning("모험 모드 전투를 시작한 다음 시도하세요.");
            }
            EditorGUILayout.HelpBox("정예/원거리 패턴은 Day 11 이후, 해당 몬스터가 포함된 출전에서 확인하세요. " +
                "반복 소환 시 필드 몬스터 안전 상한에 걸릴 수 있습니다.", MessageType.None);
            if (Event.current.type == EventType.Repaint) Repaint();
        }
    }
}
#endif
