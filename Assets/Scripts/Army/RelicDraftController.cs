using System.Collections.Generic;
using ArmySurvivor.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    public sealed class RelicDraftController : MonoBehaviour
    {
        [System.Serializable]
        private sealed class CardView
        {
            public GameObject root;
            public TMP_Text title;
            public TMP_Text description;
            public Image icon;
            public CardMotion motion;
        }

        [Header("건물과 유물 데이터")]
        [SerializeField] private BuildingDefinition library;
        [SerializeField] private RelicDefinition[] relics;
        [Header("전투 연결")]
        [SerializeField] private RunController run;
        [SerializeField] private BattleExperience experience;
        [SerializeField] private RecruitmentController recruitment;
        [Header("씬에 배치한 선택 UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private CardView[] cards;
        [SerializeField] private CardMotion.Config motionConfig = new CardMotion.Config();

        private readonly List<RelicDefinition> choices = new List<RelicDefinition>();
        private bool awaitingChoice;
        private bool ownsTimeScale;
        private float previousTimeScale;
        private int libraryLevel;
        public RelicDefinition Selected { get; private set; }

        private void Start()
        {
            panel.SetActive(false);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].motion.Configure(null, () => Choose(index), motionConfig);
            }
        }

        private void OnEnable()
        {
            if (run != null) run.RunEnded += EndBattle;
        }

        private void OnDisable()
        {
            if (run != null) run.RunEnded -= EndBattle;
            awaitingChoice = false;
            Selected = null;
            ClosePanel();
        }

        public bool TryOpen()
        {
            if (awaitingChoice) return true;
            if (GameModeManager.Current != GameMode.Adventure || !run.IsRunning ||
                run.IsPaused || run.IsChoosingUpgrade || library == null ||
                panel == null || cards == null || cards.Length == 0) return false;

            // 전투 씬 진입 시 마을 루트가 비활성화되므로 저장된 건물 상태까지 찾는다.
            SceneTravel travel = FindFirstObjectByType<SceneTravel>(FindObjectsInactive.Include);
            if (travel == null || travel.Village == null || !travel.Village.HasBuilding(library)) return false;
            libraryLevel = travel.Village.GetHighestLevel(library);

            // 후보 에셋을 복제하지 않고 목록만 섞어 같은 카드가 중복되지 않게 한다.
            var pool = new List<RelicDefinition>();
            foreach (RelicDefinition relic in relics)
                if (relic != null && !pool.Contains(relic)) pool.Add(relic);
            if (pool.Count == 0) return false;
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int swap = Random.Range(0, i + 1);
                (pool[i], pool[swap]) = (pool[swap], pool[i]);
            }

            choices.Clear();
            int count = Mathf.Min(cards.Length, pool.Count);
            for (int i = 0; i < cards.Length; i++)
            {
                CardView view = cards[i];
                bool visible = i < count;
                view.root.SetActive(visible);
                if (!visible) continue;
                RelicDefinition relic = pool[i];
                choices.Add(relic);
                view.title.text = relic.displayName;
                view.description.text = relic.DescriptionAt(libraryLevel);
                view.icon.sprite = relic.icon;
                view.icon.gameObject.SetActive(relic.icon != null);
            }
            awaitingChoice = true;
            previousTimeScale = Time.timeScale;
            ownsTimeScale = true;
            run.IsChoosingUpgrade = true;
            Time.timeScale = 0f;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            for (int i = 0; i < count; i++) cards[i].motion.PlayEntrance(i);
            return true;
        }

        private void Choose(int index)
        {
            if (!awaitingChoice || index < 0 || index >= choices.Count) return;
            awaitingChoice = false;
            Selected = choices[index];
            for (int i = 0; i < choices.Count; i++) cards[i].motion.LockInteraction();
            cards[index].motion.PlaySelection(() =>
            {
                int remaining = choices.Count - 1;
                if (remaining == 0) { ApplyAndResume(); return; }
                for (int i = 0; i < choices.Count; i++)
                    if (i != index) cards[i].motion.FadeOut(() =>
                    {
                        remaining--;
                        if (remaining == 0) ApplyAndResume();
                    });
            });
        }

        private void ApplyAndResume()
        {
            // 전투 진입 시 RunStarted 구독자들이 체력·경험치를 초기화한 뒤이므로 효과만 부여한다.
            if (run.IsRunning && Selected != null) Selected.Apply(run, experience, recruitment, libraryLevel);
            else Selected = null;
            ClosePanel();
        }

        private void EndBattle()
        {
            Selected = null;
            choices.Clear();
            awaitingChoice = false;
            ClosePanel();
        }

        private void ClosePanel()
        {
            if (panel != null) panel.SetActive(false);
            if (!ownsTimeScale) return;
            run.IsChoosingUpgrade = false;
            Time.timeScale = previousTimeScale;
            ownsTimeScale = false;
        }
    }
}
