using System;
using System.Collections.Generic;
using ArmySurvivor.Army;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.UI
{
    // 씬에 배치된 환경설정 창(Settings Canvas 프리팹)의 동작. 값은 GameSettings에 저장/적용된다.
    public sealed class SettingsPanel : MonoBehaviour
    {
        [Serializable]
        public class Choice
        {
            public Button previous;
            public Button next;
            public TMP_Text value;
        }

        [Tooltip("열고 닫는 전체 화면 덮개(창과 어두운 배경)")]
        [SerializeField] private GameObject root;
        [Tooltip("게임 중에 열면 시간을 멈추고 닫을 때 되돌린다.")]
        [SerializeField] private bool pauseWhileOpen = true;
        [Header("소리")]
        [SerializeField] private Slider master;
        [SerializeField] private Slider bgm;
        [SerializeField] private Slider sfx;
        [SerializeField] private TMP_Text masterValue;
        [SerializeField] private TMP_Text bgmValue;
        [SerializeField] private TMP_Text sfxValue;
        [Header("화면")]
        [SerializeField] private Choice resolution;
        [SerializeField] private Choice displayMode;
        [SerializeField] private Choice vSync;
        [SerializeField] private Choice quality;
        [SerializeField] private Choice frameRate;
        [Header("게임")]
        [SerializeField] private Choice damageNumbers;
        [Tooltip("비워 두면 숨긴다.")]
        [SerializeField] private Button tutorialReset;
        [SerializeField] private TMP_Text tutorialResetLabel;
        [SerializeField] private string tutorialResetDoneText = "초기화했습니다";
        [SerializeField] private Button close;
        [Header("시작 화면으로")]
        [Tooltip("비워 두면 없음. 시작 화면 씬에서는 자동으로 숨긴다.")]
        [SerializeField] private Button toStart;
        [SerializeField] private TMP_Text toStartLabel;
        [SerializeField] private string startSceneName = "StartScene";
        [Tooltip("실수로 누르는 것을 막기 위해 첫 클릭에 보여 주는 확인 문구")]
        [SerializeField] private string toStartConfirmText = "한 번 더 누르면 이동";

        public static bool AnyOpen => openCount > 0;
        private static int openCount;
        public bool IsOpen => root != null && root.activeSelf;

        private List<Vector2Int> resolutions = new List<Vector2Int>();
        private float savedTimeScale = 1f;
        private bool ownsPause;
        private string tutorialResetText;
        private string toStartText;
        private bool toStartArmed;

        private static GameSettingsProfile Profile => GameSettings.Profile;

        private void Awake()
        {
            master.onValueChanged.AddListener(v => { GameSettings.MasterVolume = v; Refresh(); });
            bgm.onValueChanged.AddListener(v => { GameSettings.BgmVolume = v; Refresh(); });
            sfx.onValueChanged.AddListener(v => { GameSettings.SfxVolume = v; Refresh(); });
            Bind(resolution, StepResolution);
            Bind(displayMode, _ => GameSettings.Fullscreen = !GameSettings.Fullscreen);
            Bind(vSync, _ => GameSettings.VSync = !GameSettings.VSync);
            Bind(quality, d => GameSettings.Quality = Wrap(GameSettings.Quality + d, Profile.graphicsTiers.Length));
            Bind(frameRate, StepFrameRate);
            Bind(damageNumbers, _ => DamageNumbers.Enabled = !DamageNumbers.Enabled);
            if (tutorialReset != null)
            {
                tutorialResetText = tutorialResetLabel != null ? tutorialResetLabel.text : null;
                tutorialReset.onClick.AddListener(ResetTutorial);
            }
            close.onClick.AddListener(Close);
            if (toStart != null)
            {
                if (gameObject.scene.name == startSceneName)
                {
                    // 시작 화면에서는 필요 없으므로 숨기고 닫기 버튼을 가운데로 돌린다.
                    toStart.gameObject.SetActive(false);
                    var closeRect = (RectTransform)close.transform;
                    closeRect.anchoredPosition = new Vector2(0f, closeRect.anchoredPosition.y);
                }
                else
                {
                    toStartText = toStartLabel != null ? toStartLabel.text : null;
                    toStart.onClick.AddListener(ReturnToStart);
                }
            }
            root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (IsOpen) openCount = Mathf.Max(0, openCount - 1);
            if (ownsPause) Time.timeScale = savedTimeScale;
        }

        public void Open()
        {
            if (IsOpen) return;
            resolutions = GameSettings.Resolutions();
            if (tutorialResetLabel != null && tutorialResetText != null) tutorialResetLabel.text = tutorialResetText;
            if (tutorialReset != null) tutorialReset.interactable = true;
            toStartArmed = false;
            if (toStartLabel != null && toStartText != null) toStartLabel.text = toStartText;
            root.SetActive(true);
            openCount++;
            if (pauseWhileOpen)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                ownsPause = true;
            }
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            openCount = Mathf.Max(0, openCount - 1);
            if (ownsPause) { Time.timeScale = savedTimeScale; ownsPause = false; }
            GameSettings.Save();
        }

        private void Refresh()
        {
            master.SetValueWithoutNotify(GameSettings.MasterVolume);
            bgm.SetValueWithoutNotify(GameSettings.BgmVolume);
            sfx.SetValueWithoutNotify(GameSettings.SfxVolume);
            masterValue.text = Percent(GameSettings.MasterVolume);
            bgmValue.text = Percent(GameSettings.BgmVolume);
            sfxValue.text = Percent(GameSettings.SfxVolume);

            var size = GameSettings.Resolution;
            resolution.value.text = string.Format(Profile.resolutionFormat, size.x, size.y);
            displayMode.value.text = GameSettings.Fullscreen ? Profile.fullscreenText : Profile.windowedText;
            vSync.value.text = OnOff(GameSettings.VSync);
            quality.value.text = QualityLabel(GameSettings.Quality);
            int fps = GameSettings.FrameRate;
            frameRate.value.text = fps > 0 ? string.Format(Profile.frameRateFormat, fps) : Profile.unlimitedFrameRateText;
            // VSync가 켜져 있으면 프레임 제한은 적용되지 않으므로 잠근다.
            SetInteractable(frameRate, !GameSettings.VSync);
            SetInteractable(resolution, resolutions.Count > 1);
            damageNumbers.value.text = OnOff(DamageNumbers.Enabled);
        }

        private void Bind(Choice choice, Action<int> step)
        {
            choice.previous.onClick.AddListener(() => { step(-1); Refresh(); });
            choice.next.onClick.AddListener(() => { step(1); Refresh(); });
        }

        private void StepResolution(int delta)
        {
            if (resolutions.Count == 0) return;
            int index = Mathf.Max(0, resolutions.IndexOf(GameSettings.Resolution));
            GameSettings.Resolution = resolutions[Wrap(index + delta, resolutions.Count)];
        }

        private void StepFrameRate(int delta)
        {
            var options = Profile.frameRateOptions;
            if (options == null || options.Length == 0) return;
            int index = Mathf.Max(0, Array.IndexOf(options, GameSettings.FrameRate));
            GameSettings.FrameRate = options[Wrap(index + delta, options.Length)];
        }

        private void ResetTutorial()
        {
            ArmySurvivor.Tutorial.TutorialProgressStore.Clear();
            if (tutorialResetLabel != null) tutorialResetLabel.text = tutorialResetDoneText;
            tutorialReset.interactable = false;
        }

        private void ReturnToStart()
        {
            if (!toStartArmed)
            {
                toStartArmed = true;
                if (toStartLabel != null) toStartLabel.text = toStartConfirmText;
                return;
            }
            GameSettings.Save();
            // OnDestroy가 열 때 저장한 timeScale(전투 일시정지 중이면 0)로 되돌리지 않도록 먼저 소유권을 놓는다.
            ownsPause = false;
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(startSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        private static void SetInteractable(Choice choice, bool value)
        {
            choice.previous.interactable = value;
            choice.next.interactable = value;
        }

        private static int Wrap(int value, int count) => count <= 0 ? 0 : ((value % count) + count) % count;
        private static string Percent(float value) => string.Format(Profile.percentFormat, Mathf.RoundToInt(value * 100));
        private static string OnOff(bool value) => value ? Profile.onText : Profile.offText;

        private static string QualityLabel(int level)
        {
            var tiers = Profile.graphicsTiers;
            return tiers != null && level < tiers.Length ? tiers[level].label : level.ToString();
        }
    }
}
