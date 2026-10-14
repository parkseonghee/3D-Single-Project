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
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button adventureModeButton;
        [SerializeField] private TMP_Text survivalBestTimeText;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        private bool loading;

        private void Start()
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            // 버튼과 기록 문구는 StartScene에 배치하고 여기서는 동작만 연결한다.
            newGameButton.onClick = new Button.ButtonClickedEvent();
            newGameButton.onClick.AddListener(() => StartMode(GameMode.Survival));
            adventureModeButton.onClick = new Button.ButtonClickedEvent();
            adventureModeButton.onClick.AddListener(() => StartMode(GameMode.Adventure));

            var best = RecordManager.Best;
            survivalBestTimeText.text = "최고 생존 " + RecordManager.TimeLabel(best?.survivalSeconds ?? 0);
        }

        private void StartMode(GameMode mode)
        {
            if (loading) return;
            GameModeManager.Select(mode);
            LoadNewGame();
        }

        public void NewGame() => StartMode(GameMode.Survival);

        private void LoadNewGame()
        {
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                Debug.LogError("시작할 씬을 Build Settings와 Inspector에서 확인하세요.", this);
                return;
            }
            loading = true;
            ArmySurvivor.Saving.GameSave.ClearPending();
            newGameButton.interactable = false;
            adventureModeButton.interactable = false;
            SceneManager.LoadSceneAsync(gameScene);
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
