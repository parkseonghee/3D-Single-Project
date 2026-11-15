using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace ArmySurvivor.UI
{
    // 환경설정 값 저장/적용. 저장 슬롯과 별개로 PlayerPrefs에 둔다(새 게임·이어하기와 무관한 기기 설정).
    public static class GameSettings
    {
        private const string MasterKey = "Settings.MasterVolume";
        private const string BgmKey = "Settings.BgmVolume";
        private const string SfxKey = "Settings.SfxVolume";
        private const string FullscreenKey = "Settings.Fullscreen";
        private const string WidthKey = "Settings.ResolutionWidth";
        private const string HeightKey = "Settings.ResolutionHeight";
        private const string VSyncKey = "Settings.VSync";
        private const string QualityKey = "Settings.GraphicsTier";
        private const string FrameRateKey = "Settings.FrameRate";

        private static GameSettingsProfile profile;
        public static GameSettingsProfile Profile
        {
            get
            {
                if (profile == null) profile = Resources.Load<GameSettingsProfile>("GameSettingsProfile");
                if (profile == null) profile = ScriptableObject.CreateInstance<GameSettingsProfile>();
                return profile;
            }
        }

        public static event Action Changed;

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterKey, Profile.defaultMaster);
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); AudioListener.volume = MasterVolume; Notify(); }
        }
        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BgmKey, Profile.defaultBgm);
            set { PlayerPrefs.SetFloat(BgmKey, Mathf.Clamp01(value)); Notify(); }
        }
        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxKey, Profile.defaultSfx);
            set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); Notify(); }
        }
        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Profile.defaultFullscreen ? 1 : 0) == 1;
            set { PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); ApplyDisplay(); Notify(); }
        }
        public static bool VSync
        {
            get => PlayerPrefs.GetInt(VSyncKey, Profile.defaultVSync ? 1 : 0) == 1;
            set { PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0); ApplyFrame(); Notify(); }
        }
        public static int Quality
        {
            get
            {
                int count = Profile.graphicsTiers != null ? Profile.graphicsTiers.Length : 0;
                return Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, Profile.defaultGraphicsTier), 0, Mathf.Max(0, count - 1));
            }
            set { PlayerPrefs.SetInt(QualityKey, value); ApplyQuality(); Notify(); }
        }
        public static int FrameRate
        {
            get => PlayerPrefs.GetInt(FrameRateKey, Profile.defaultFrameRate);
            set { PlayerPrefs.SetInt(FrameRateKey, Mathf.Max(0, value)); ApplyFrame(); Notify(); }
        }

        // 저장된 해상도가 없으면 현재 모니터 해상도를 쓴다.
        public static Vector2Int Resolution
        {
            get
            {
                var current = Screen.currentResolution;
                return new Vector2Int(PlayerPrefs.GetInt(WidthKey, current.width), PlayerPrefs.GetInt(HeightKey, current.height));
            }
            set
            {
                PlayerPrefs.SetInt(WidthKey, value.x);
                PlayerPrefs.SetInt(HeightKey, value.y);
                ApplyDisplay();
                Notify();
            }
        }

        // 모니터가 지원하는 해상도(주사율만 다른 중복 제거, 낮은 해상도 제외)
        public static List<Vector2Int> Resolutions()
        {
            var list = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (r.height >= Profile.minResolutionHeight && !list.Contains(size)) list.Add(size);
            }
            var saved = Resolution;
            if (!list.Contains(saved)) list.Add(saved);
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return list;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void ApplyAll()
        {
            AudioListener.volume = MasterVolume;
            ApplyQuality();
            ApplyFrame();
            ApplyDisplay();
        }

        private static UniversalRenderPipelineAsset sourceAsset, runtimeAsset;

        // 프로젝트의 URP 에셋을 복사해 그 복사본에만 품질 단계를 적용한다. 원본 에셋과 Project Settings는 바뀌지 않는다.
        private static void ApplyQuality()
        {
            var tiers = Profile.graphicsTiers;
            if (tiers == null || tiers.Length == 0) return;
            if (runtimeAsset == null)
            {
                sourceAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if (sourceAsset == null) return;
                runtimeAsset = Object.Instantiate(sourceAsset);
                runtimeAsset.name = sourceAsset.name + " (Runtime)";
                QualitySettings.renderPipeline = runtimeAsset;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.playModeStateChanged += RestoreOnExit;
#endif
            }
            var tier = tiers[Quality];
            runtimeAsset.renderScale = tier.renderScale;
            runtimeAsset.msaaSampleCount = tier.msaa;
            runtimeAsset.shadowDistance = tier.shadowDistance;
            runtimeAsset.mainLightShadowmapResolution = tier.shadowResolution;
            runtimeAsset.shadowCascadeCount = tier.shadowCascades;
        }

#if UNITY_EDITOR
        // 에디터에서 플레이를 끝내면 품질 설정의 렌더 에셋을 원래대로 돌린다.
        private static void RestoreOnExit(UnityEditor.PlayModeStateChange state)
        {
            if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
            UnityEditor.EditorApplication.playModeStateChanged -= RestoreOnExit;
            QualitySettings.renderPipeline = sourceAsset;
            if (runtimeAsset != null) Object.Destroy(runtimeAsset);
            runtimeAsset = null;
        }
#endif

        private static void ApplyFrame()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = FrameRate > 0 ? FrameRate : -1;
        }

        // 에디터의 Game 뷰 크기는 바꾸지 않는다. 빌드에서만 해상도와 창 모드를 적용한다.
        private static void ApplyDisplay()
        {
#if !UNITY_EDITOR
            var size = Resolution;
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.width != size.x || Screen.height != size.y || Screen.fullScreenMode != mode)
                Screen.SetResolution(size.x, size.y, mode);
#endif
        }

        private static void Notify() => Changed?.Invoke();

        // 슬라이더를 끄는 동안 매번 디스크에 쓰지 않도록 설정 창을 닫을 때 한 번 저장한다.
        public static void Save() => PlayerPrefs.Save();
    }
}
