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
        private float savedTimeScale = 1;

        private void Start()
        {
            pauseButton.onClick.AddListener(Pause);
            resumeButton.onClick.AddListener(Resume);
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

        public void Resume()
        {
            if (run.IsPaused) Time.timeScale = savedTimeScale;
            run.IsPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
        }
    }
}
