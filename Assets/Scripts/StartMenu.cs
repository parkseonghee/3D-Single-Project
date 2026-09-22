using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArmySurvivor.UI
{
    public class StartMenu : MonoBehaviour
    {
        [SerializeField] private string gameScene;
        [SerializeField] private Button newGameButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullscreenToggle;
        private bool loading;

        private void Start()
        {
            volumeSlider.SetValueWithoutNotify(AudioListener.volume);
            fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        }

        public void NewGame()
        {
            if (loading) return;
            if (!Application.CanStreamedLevelBeLoaded(gameScene))
            {
                Debug.LogError("시작할 씬을 Build Settings와 Inspector에서 확인하세요.", this);
                return;
            }
            loading = true;
            ArmySurvivor.Saving.GameSave.ClearPending();
            newGameButton.interactable = false;
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
