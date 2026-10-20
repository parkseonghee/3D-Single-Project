using System;
using UnityEngine;

namespace ArmySurvivor.Tutorial
{
    public static class TutorialEvents
    {
        public static event Action<string, Transform> Raised;
        public static void Raise(string key, Transform target = null)
        {
            if (!string.IsNullOrEmpty(key)) Raised?.Invoke(key, target);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Raised = null;
    }
}
