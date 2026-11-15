using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ArmySurvivor.Army;

namespace ArmySurvivor.UI
{
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] private string gameScene;
        [SerializeField] private string survivalScene = "PlayScene";
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button adventureModeButton;
        [Header("이어하기 (모험 자동 저장)")]
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueStatus;
        [SerializeField] private string continueFormat = "Day {0} 아침 · 재도전권 {1}";
        [SerializeField] private string battleQuitText = " (전투 중 종료 · 이어하면 재도전권 1장 사용)";
        [SerializeField] private string campaignEndedText = "전투 중 종료되어 남은 재도전권이 없습니다. 회차가 종료되었습니다.";
        [Header("새 모험 확인 (이어할 모험이 있을 때)")]
        [SerializeField] private GameObject newAdventureConfirm;
        [SerializeField] private TMP_Text newAdventureMessage;
        [SerializeField] private Button newAdventureConfirmButton;
        [SerializeField] private Button newAdventureCancelButton;
        [SerializeField, TextArea] private string newAdventureFormat = "진행 중인 모험(Day {0}, 재도전권 {1})이 삭제됩니다.\n새 모험을 시작할까요?";
        [SerializeField] private TMP_Text survivalBestTimeText;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle damageNumbersToggle;
        private bool loading;

        private void Start()
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            if (damageNumbersToggle != null)
            {
                damageNumbersToggle.SetIsOnWithoutNotify(DamageNumbers.Enabled);
                damageNumbersToggle.onValueChanged.AddListener(value => DamageNumbers.Enabled = value);
            }
            // 버튼과 기록 문구는 StartScene에 배치하고 여기서는 동작만 연결한다.
            newGameButton.onClick = new Button.ButtonClickedEvent();
            newGameButton.onClick.AddListener(() => StartMode(GameMode.Survival));
            adventureModeButton.onClick = new Button.ButtonClickedEvent();
            adventureModeButton.onClick.AddListener(AdventureClicked);
            if (newAdventureConfirm != null)
            {
                newAdventureConfirm.SetActive(false);
                newAdventureConfirmButton.onClick.AddListener(ConfirmNewAdventure);
                newAdventureCancelButton.onClick.AddListener(() => newAdventureConfirm.SetActive(false));
            }
            if (continueButton != null)
            {
                // 씬에 남아 있던 슬롯 창 연결을 지우고 자동 저장 이어하기로 바꾼다.
                continueButton.onClick = new Button.ButtonClickedEvent();
                continueButton.onClick.AddListener(Continue);
                RefreshContinue();
            }

            bool survivalUnlocked = GameModeManager.SurvivalUnlocked;
            newGameButton.interactable = survivalUnlocked;
            if (survivalUnlocked)
            {
                var best = RecordManager.Best;
                survivalBestTimeText.text = "최고 생존 " + RecordManager.TimeLabel(best?.survivalSeconds ?? 0);
            }
            else survivalBestTimeText.text = $"모험 Day {GameModeManager.SurvivalUnlockDay} 도달 시 해금";
        }

        private void StartMode(GameMode mode)
        {
            if (loading || (mode == GameMode.Survival && !GameModeManager.SurvivalUnlocked)) return;
            GameModeManager.Select(mode);
            LoadNewGame(mode == GameMode.Survival ? survivalScene : gameScene);
        }

        public void NewGame() => StartMode(GameMode.Survival);

        // 자동 저장이 있으면 덮어쓰기 전에 확인한다. 확인 창이 배치되지 않았으면 바로 시작한다.
        private void AdventureClicked()
        {
            var info = ArmySurvivor.Saving.GameSave.AutoSaveSummary();
            if (loading || !info.HasValue || newAdventureConfirm == null) { StartMode(GameMode.Adventure); return; }
            newAdventureMessage.text = string.Format(newAdventureFormat, info.Value.day, info.Value.tokens);
            newAdventureConfirm.transform.SetAsLastSibling();
            newAdventureConfirm.SetActive(true);
        }

        private void ConfirmNewAdventure()
        {
            newAdventureConfirm.SetActive(false);
            ArmySurvivor.Saving.GameSave.DeleteAutoSave();
            StartMode(GameMode.Adventure);
        }

        private void RefreshContinue()
        {
            var info = ArmySurvivor.Saving.GameSave.AutoSaveSummary();
            continueButton.interactable = !loading && info.HasValue;
            if (continueStatus != null)
                continueStatus.text = info.HasValue
                    ? string.Format(continueFormat, info.Value.day, info.Value.tokens) + (info.Value.battleInProgress ? battleQuitText : "")
                    : "";
        }

        public void Continue()
        {
            if (loading) return;
            switch (ArmySurvivor.Saving.GameSave.LoadAutoSave(gameScene))
            {
                case ArmySurvivor.Saving.GameSave.ContinueResult.CampaignEnded:
                    RefreshContinue();
                    if (continueStatus != null) continueStatus.text = campaignEndedText;
                    return;
                case ArmySurvivor.Saving.GameSave.ContinueResult.Missing:
                    RefreshContinue();
                    return;
            }
            loading = true;
            newGameButton.interactable = adventureModeButton.interactable = continueButton.interactable = false;
        }

        private void LoadNewGame(string scene)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError("시작할 씬을 Build Settings와 Inspector에서 확인하세요.", this);
                return;
            }
            loading = true;
            ArmySurvivor.Saving.GameSave.ClearPending();
            newGameButton.interactable = false;
            adventureModeButton.interactable = false;
            SceneManager.LoadSceneAsync(scene);
        }

        public void OpenSettings()
        {
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
        }
        public void CloseSettings() => settingsPanel.SetActive(false);
        public void SetVolume(float value) => AudioListener.volume = value;
        public void SetFullscreen(bool value) => Screen.fullScreen = value;

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
