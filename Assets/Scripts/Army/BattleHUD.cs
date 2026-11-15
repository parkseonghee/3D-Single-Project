using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] private RunController run;
        [SerializeField] private TMP_Text commanderHealth;
        [SerializeField] private Image commanderFill;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private GameObject pausePanel;
        [Tooltip("일시정지 메뉴의 환경설정 버튼과 씬의 설정 창")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private ArmySurvivor.UI.SettingsPanel settings;
        private float savedTimeScale = 1;

        private void Start()
        {
            pauseButton.onClick.AddListener(Pause);
            resumeButton.onClick.AddListener(Resume);
            if (settingsButton != null && settings != null) settingsButton.onClick.AddListener(settings.Open);
            pausePanel.SetActive(false);
        }

        private void OnEnable() => run.RunEnded += Resume;
        private void OnDisable()
        {
            run.RunEnded -= Resume;
            Resume();
        }

        private void Update()
        {
            if (!run.IsRunning) return;
            var health = run.Commander.GetComponent<UnitHealth>();
            if (health == null) return;
            commanderHealth.text = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Maximum)}";
            commanderFill.rectTransform.anchorMax = new Vector2(health.Current / health.Maximum, 1);
        }

        public void Pause()
        {
            if (!run.IsRunning || run.IsChoosingUpgrade || run.IsPaused) return;
            savedTimeScale = Time.timeScale;
            run.IsPaused = true;
            Time.timeScale = 0;
            pausePanel.SetActive(true);
            pausePanel.transform.SetAsLastSibling();
        }

        // ESC: 일시정지 중이면 계속하기, 전투 중이면 일시정지. 전투가 아니면 false를 돌려 설정 창을 열게 한다.
        public bool HandleEscape()
        {
            if (run.IsPaused) { Resume(); return true; }
            if (!run.IsRunning) return false;
            Pause(); // 카드 선택 중에는 열리지 않지만 ESC를 소비해 설정 창이 끼어들지 않게 한다.
            return true;
        }

        public void Resume()
        {
            if (run.IsPaused) Time.timeScale = savedTimeScale;
            run.IsPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }
}
