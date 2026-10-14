using UnityEngine;

namespace ArmySurvivor.Army
{
    public enum GameMode { Survival, Adventure }

    // Scene changes do not destroy static state. New sessions start in Survival.
    public static class GameModeManager
    {
        public static GameMode Current { get; private set; } = GameMode.Survival;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = GameMode.Survival;

        public static void Select(GameMode mode) => Current = mode;
    }
}
