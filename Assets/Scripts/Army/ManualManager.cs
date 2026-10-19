using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArmySurvivor.Army
{
    [RequireComponent(typeof(RunController), typeof(BattleExperience), typeof(CombatController))]
    [DefaultExecutionOrder(100)]
    public sealed class ManualManager : MonoBehaviour
    {
        // Selection is available across scene loads; runtime buffs never persist across battles.
        public static ManualDefinition Selected { get; private set; }
        public static void Select(ManualDefinition manual) => Selected = manual;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSelection() => Selected = null;
        public BattleExperience Experience { get; private set; }
        public CombatController Combat { get; private set; }
        public RunController Run { get; private set; }
        public ManualDefinition ActiveManual { get; private set; }
        public readonly HashSet<int> InvestedIds = new HashSet<int>();
        public int Required { get; private set; }
        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public int Stacks { get; private set; }
        public string Status { get; private set; }
        public string Announcement { get; private set; }
        public float Progress { get; private set; }
        public event Action Changed;
        public event Action Activated;
        [SerializeField] private bool debugKeysEnabled = true;
        [SerializeField, Min(1)] private int debugBottleCount = 30;
        private ManualEffect effect;
        private readonly List<GameObject> visuals = new List<GameObject>();

        private void Awake()
        { Run = GetComponent<RunController>(); Experience = GetComponent<BattleExperience>(); Combat = GetComponent<CombatController>(); }
        private void OnEnable() { Run.RunStarted += Begin; Run.RunEnded += End; }
        private void OnDisable() { Run.RunStarted -= Begin; Run.RunEnded -= End; End(); }
        private void Begin()
        {
            EndEffect();
            ActiveManual = Selected;
            if (ActiveManual != null && ActiveManual.effect != null)
            { effect = ActiveManual.effect.CreateEffect(this); effect.OnBattleStart(); }
            Changed?.Invoke();
        }
        private void EndEffect()
        {
            effect?.OnBattleEnd(); effect = null;
            foreach (var visual in visuals) if (visual != null) Destroy(visual);
            visuals.Clear(); InvestedIds.Clear(); Required = 0; Remaining = 0; Duration = 0; Stacks = 0;
            Status = null; Progress = 0;
        }
        private void End() { EndEffect(); ActiveManual = null; Selected = null; Changed?.Invoke(); }
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugKeysEnabled && Keyboard.current != null)
            {
                if (Keyboard.current.f8Key.wasPressedThisFrame && !Run.IsRunning)
                    GetComponent<RelicDraftController>()?.OpenDebug();
                if (Keyboard.current.f9Key.wasPressedThisFrame) Experience.GrantBottles(debugBottleCount);
            }
#endif
            if (Run.IsRunning && !Run.IsPaused && !Run.IsChoosingUpgrade) effect?.Tick(Time.deltaTime);
        }
        public void Publish(IEnumerable<int> ids, int required, float remaining, float duration, int stacks)
        {
            InvestedIds.Clear(); foreach (int id in ids) InvestedIds.Add(id);
            Required = required; Remaining = remaining; Duration = duration; Stacks = stacks;
            Progress = InvestedIds.Count;
            Status = remaining > 0 ? $"강화 {remaining:0.0}초" + (stacks > 1 ? $" · {stacks}중첩" : "")
                : $"서로 다른 병사 {InvestedIds.Count}/{required}";
            Changed?.Invoke();
        }
        public void PublishStatus(string status, float progress = 0, int required = 0, float remaining = 0, float duration = 0)
        {
            InvestedIds.Clear(); Status = status; Progress = progress; Required = required;
            Remaining = remaining; Duration = duration; Stacks = 0; Changed?.Invoke();
        }
        public void Announce(string text) { Announcement = text; Activated?.Invoke(); }
        public void MarkPortraits(IEnumerable<int> ids)
        { InvestedIds.Clear(); foreach (int id in ids) InvestedIds.Add(id); Changed?.Invoke(); }
        public GameObject PlayVisual(GameObject prefab, Vector3 position, Vector3 scale, float lifetime, Transform parent = null)
        {
            if (prefab == null) return null;
            visuals.RemoveAll(item => item == null);
            var visual = Instantiate(prefab, position, prefab.transform.rotation, parent);
            visual.transform.localScale = scale;
            visuals.Add(visual); Destroy(visual, Mathf.Max(0.1f, lifetime));
            return visual;
        }
        public void PlayActivation(EqualEducationEffect data)
        {
            visuals.RemoveAll(item => item == null);
            if (data.buffEffect != null)
                foreach (var unit in Experience.TrackedUnits)
                {
                    if (unit == null || !unit.gameObject.activeInHierarchy ||
                        (unit.TryGetComponent<UnitHealth>(out var health) && health.IsDead)) continue;
                    var visual = Instantiate(data.buffEffect, unit);
                    visual.transform.localPosition = data.effectOffset;
                    visual.transform.localScale = data.effectScale;
                    visuals.Add(visual); Destroy(visual, data.effectLifetime);
                }
            Announce("균등 교육!");
        }
    }
}
