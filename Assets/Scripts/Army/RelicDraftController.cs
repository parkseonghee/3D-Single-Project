using System;
using System.Collections.Generic;
using ArmySurvivor.Building;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    // Retains the existing scene component and CardMotion bindings.
    public sealed class RelicDraftController : MonoBehaviour
    {
        [Serializable] private sealed class CardView
        {
            public GameObject root;
            public TMP_Text title;
            public TMP_Text description;
            public Image icon;
            public CardMotion motion;
        }
        [SerializeField] private BuildingDefinition library;
        [SerializeField] private ManualDefinition[] manuals;
        [SerializeField] private RunController run;
        [SerializeField] private RecruitmentController recruitment;
        [SerializeField] private GameObject panel;
        [SerializeField] private CardView[] cards;
        [SerializeField] private Button skipButton;
        [SerializeField] private CardMotion.Config motionConfig = new CardMotion.Config();
        private readonly List<ManualDefinition> choices = new List<ManualDefinition>();
        private Action onComplete;
        private bool awaitingChoice, selecting, forceNext, ownsTimeScale;
        private float previousTimeScale;
        public ManualDefinition Selected => ManualManager.Selected;
        public bool IsOpen => awaitingChoice || selecting;
        public IReadOnlyList<ManualDefinition> Choices => choices;

        private void Start()
        {
            panel.SetActive(false);
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].motion.Configure(null, () => Choose(index), motionConfig);
            }
            if (skipButton != null) skipButton.onClick.AddListener(Skip);
        }
        public void OpenDebug()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (run.IsRunning || IsOpen) return;
            forceNext = true;
            run.BeginRun();
            forceNext = false;
#endif
        }
        public bool TryOpen(Action beginBattle)
        {
            if (IsOpen) return true;
            bool force = forceNext; forceNext = false;
            if (run.IsRunning || run.IsPaused || panel == null || cards == null || cards.Length == 0) return false;
            if (!force)
            {
                var travel = FindFirstObjectByType<SceneTravel>(FindObjectsInactive.Include);
                if (library == null || travel == null || travel.Village == null || !travel.Village.HasBuilding(library)) return false;
            }
            var pool = new List<ManualDefinition>();
            if (manuals != null)
                foreach (var manual in manuals)
                    if (manual != null && !pool.Contains(manual) && manual.IsEligible(recruitment)) pool.Add(manual);
            if (pool.Count == 0) return false;
            for (int i = pool.Count - 1; i > 0; i--)
            { int swap = UnityEngine.Random.Range(0, i + 1); (pool[i], pool[swap]) = (pool[swap], pool[i]); }
            choices.Clear();
            int count = Mathf.Min(3, Mathf.Min(cards.Length, pool.Count));
            for (int i = 0; i < cards.Length; i++)
            {
                var view = cards[i]; view.root.SetActive(i < count);
                if (i >= count) continue;
                var manual = pool[i]; choices.Add(manual);
                view.title.text = manual.displayName;
                view.description.text = manual.effect.Describe(manual.description) + "\n\n<b>플레이 변화</b>\n" + manual.playStyle;
                view.icon.sprite = manual.icon; view.icon.gameObject.SetActive(manual.icon != null);
                var rect = (RectTransform)view.root.transform;
                rect.anchoredPosition = new Vector2((i - (count - 1) * 0.5f) * (rect.rect.width + 36), rect.anchoredPosition.y);
            }
            ManualManager.Select(null);
            onComplete = beginBattle; awaitingChoice = true;
            previousTimeScale = Time.timeScale; ownsTimeScale = true;
            run.IsChoosingUpgrade = true; Time.timeScale = 0;
            panel.SetActive(true); panel.transform.SetAsLastSibling();
            if (skipButton != null) skipButton.interactable = true;
            for (int i = 0; i < count; i++) cards[i].motion.PlayEntrance(i);
            return true;
        }
        public void Choose(int index)
        {
            if (!awaitingChoice || selecting || index < 0 || index >= choices.Count) return;
            awaitingChoice = false; selecting = true;
            ManualManager.Select(choices[index]);
            ArmySurvivor.Tutorial.TutorialEvents.Raise("ManualSelected");
            if (skipButton != null) skipButton.interactable = false;
            foreach (var card in cards) if (card.root.activeSelf) card.motion.LockInteraction();
            cards[index].motion.PlaySelection(Finish);
        }
        public void Skip()
        {
            if (!awaitingChoice || selecting) return;
            ManualManager.Select(null); Finish();
        }
        private void Finish()
        {
            var callback = onComplete; onComplete = null;
            ClosePanel(); callback?.Invoke();
        }
        private void ClosePanel()
        {
            awaitingChoice = false; selecting = false;
            if (panel != null) panel.SetActive(false);
            if (!ownsTimeScale) return;
            run.IsChoosingUpgrade = false; Time.timeScale = previousTimeScale; ownsTimeScale = false;
        }
        private void OnDisable()
        {
            onComplete = null;
            if (IsOpen) ManualManager.Select(null);
            ClosePanel();
        }
    }
}
