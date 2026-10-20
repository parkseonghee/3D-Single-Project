using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArmySurvivor.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialTarget : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string targetId;
        private static readonly Dictionary<string, List<TutorialTarget>> Registry =
            new Dictionary<string, List<TutorialTarget>>();
        public static event Action<TutorialTarget> Clicked;
        public string Id => targetId;
        public bool IsUi => transform is RectTransform;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registry.Clear();
            Clicked = null;
        }

        private void OnEnable() => Register();
        private void OnDisable() => Unregister();
        private void OnDestroy() => Unregister();

        public void SetId(string value)
        {
            if (targetId == value) return;
            Unregister();
            targetId = value;
            if (isActiveAndEnabled) Register();
        }

        private void Register()
        {
            if (string.IsNullOrEmpty(targetId)) return;
            if (!Registry.TryGetValue(targetId, out var list))
                Registry[targetId] = list = new List<TutorialTarget>();
            if (!list.Contains(this)) list.Add(this);
        }

        private void Unregister()
        {
            if (string.IsNullOrEmpty(targetId) || !Registry.TryGetValue(targetId, out var list)) return;
            list.Remove(this);
            if (list.Count == 0) Registry.Remove(targetId);
        }

        public static TutorialTarget Find(string id, Transform preferred = null)
        {
            if (string.IsNullOrEmpty(id) || !Registry.TryGetValue(id, out var list)) return null;
            if (preferred != null)
                foreach (TutorialTarget candidate in list)
                    if (candidate != null && candidate.isActiveAndEnabled &&
                        (candidate.transform == preferred || preferred.IsChildOf(candidate.transform)))
                        return candidate;
            foreach (TutorialTarget candidate in list)
                if (candidate != null && candidate.isActiveAndEnabled && candidate.gameObject.activeInHierarchy)
                    return candidate;
            return null;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);
    }
}
