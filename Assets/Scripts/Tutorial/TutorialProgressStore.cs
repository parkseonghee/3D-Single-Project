using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArmySurvivor.Tutorial
{
    [Serializable]
    internal sealed class TutorialProgressFile
    {
        public int version = 1;
        public List<string> completed = new List<string>();
    }

    public static class TutorialProgressStore
    {
        private static TutorialProgressFile cache;
        private static string PathName =>
            Path.Combine(Application.persistentDataPath, "tutorial_progress.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => cache = null;

        private static TutorialProgressFile Data
        {
            get
            {
                if (cache != null) return cache;
                try
                {
                    if (File.Exists(PathName))
                    {
                        var data = JsonUtility.FromJson<TutorialProgressFile>(File.ReadAllText(PathName));
                        if (data != null && data.version == 1 && data.completed != null)
                            return cache = data;
                    }
                }
                catch (Exception error) { Debug.LogWarning("튜토리얼 기록 읽기 실패: " + error.Message); }
                return cache = new TutorialProgressFile();
            }
        }

        public static bool IsCompleted(string id) =>
            !string.IsNullOrEmpty(id) && Data.completed.Contains(id);

        public static void Complete(string id)
        {
            if (string.IsNullOrEmpty(id) || Data.completed.Contains(id)) return;
            Data.completed.Add(id);
            Write();
        }

        public static void Clear()
        {
            cache = new TutorialProgressFile();
            Write();
        }

        private static void Write()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporary = PathName + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(Data, true));
                if (File.Exists(PathName)) File.Replace(temporary, PathName, null);
                else File.Move(temporary, PathName);
            }
            catch (Exception error) { Debug.LogWarning("튜토리얼 기록 저장 실패: " + error.Message); }
        }
    }
}
