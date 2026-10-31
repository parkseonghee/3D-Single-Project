using UnityEngine;

namespace ArmySurvivor.Army
{
    public enum GameMode { Survival, Adventure }

    // Scene changes do not destroy static state. Unlock progress is stored separately from a run.
    public static class GameModeManager
    {
        public const int SurvivalUnlockDay = 5;
        private const string HighestAdventureDayKey = "ArmySurvivor.HighestAdventureDay";

        public static GameMode Current { get; private set; } = GameMode.Survival;
        public static bool SurvivalUnlocked => PlayerPrefs.GetInt(HighestAdventureDayKey, 0) >= SurvivalUnlockDay;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = GameMode.Survival;

        public static void Select(GameMode mode) => Current = mode;

        public static void RecordAdventureDay(int day)
        {
            if (Current != GameMode.Adventure || day < 1 || day <= PlayerPrefs.GetInt(HighestAdventureDayKey, 0)) return;
            PlayerPrefs.SetInt(HighestAdventureDayKey, day);
            PlayerPrefs.Save();
        }
    }
}
