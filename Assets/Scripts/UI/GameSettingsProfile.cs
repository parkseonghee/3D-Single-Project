using System;
using UnityEngine;

namespace ArmySurvivor.UI
{
    // 환경설정의 기본값과 선택지. Resources/GameSettingsProfile 에셋에서 조정한다.
    [CreateAssetMenu(menuName = "Army Survivor/UI/Game Settings Profile")]
    public sealed class GameSettingsProfile : ScriptableObject
    {
        [Header("기본 음량 (0~1)")]
        [Range(0, 1)] public float defaultMaster = 0.8f;
        [Range(0, 1)] public float defaultBgm = 0.7f;
        [Range(0, 1)] public float defaultSfx = 0.8f;

        [Header("화면")]
        public bool defaultFullscreen = true;
        public bool defaultVSync = true;
        [Tooltip("해상도 목록에서 이보다 낮은 세로 해상도는 숨긴다.")]
        [Min(0)] public int minResolutionHeight = 720;

        [Serializable]
        public class GraphicsTier
        {
            public string label;
            [Range(0.5f, 2f)] public float renderScale = 1f;
            [Tooltip("안티에일리어싱 샘플 수 (1, 2, 4, 8)")]
            public int msaa = 1;
            [Min(0)] public float shadowDistance = 50f;
            public int shadowResolution = 2048;
            [Range(1, 4)] public int shadowCascades = 4;
        }

        [Header("그래픽 품질 (실행 중 URP 설정 복사본에만 적용, 원본 에셋은 바뀌지 않음)")]
        public GraphicsTier[] graphicsTiers =
        {
            new GraphicsTier { label = "낮음", renderScale = 0.75f, msaa = 1, shadowDistance = 25, shadowResolution = 1024, shadowCascades = 1 },
            new GraphicsTier { label = "중간", renderScale = 1f, msaa = 1, shadowDistance = 40, shadowResolution = 2048, shadowCascades = 2 },
            new GraphicsTier { label = "높음", renderScale = 1f, msaa = 1, shadowDistance = 50, shadowResolution = 2048, shadowCascades = 4 },
            new GraphicsTier { label = "최고", renderScale = 1f, msaa = 4, shadowDistance = 60, shadowResolution = 4096, shadowCascades = 4 },
        };
        [Tooltip("처음 실행할 때의 품질 단계 (graphicsTiers 순서)")]
        [Min(0)] public int defaultGraphicsTier = 2;

        [Header("프레임 제한 (0 = 제한 없음). VSync를 켜면 모니터 주사율을 따른다.")]
        public int[] frameRateOptions = { 30, 60, 120, 144, 0 };
        public int defaultFrameRate = 60;

        [Header("표시 문구")]
        public string percentFormat = "{0}%";
        public string resolutionFormat = "{0} × {1}";
        public string frameRateFormat = "{0} FPS";
        public string unlimitedFrameRateText = "제한 없음";
        public string onText = "켜짐";
        public string offText = "꺼짐";
        public string fullscreenText = "전체 화면";
        public string windowedText = "창 모드";
    }
}
