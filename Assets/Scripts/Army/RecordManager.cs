using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArmySurvivor.Army
{
    [Serializable] public sealed class SurvivalRecord
    {
        public float survivalSeconds;
        public int normalKills;
        public int eliteKills;
        public int bossKills;
        public int score;
        public string playedAtUtc;
    }

    [Serializable] internal sealed class SurvivalRecordFile
    {
        public SurvivalRecord best;
        public List<SurvivalRecord> recent = new List<SurvivalRecord>();
    }

    public static class RecordManager
    {
        private const string FileName = "survival_records.json";
        private static string PathName => Path.Combine(Application.persistentDataPath, FileName);

        private static SurvivalRecordFile Read()
        {
            try
            {
                if (File.Exists(PathName))
                {
                    var data = JsonUtility.FromJson<SurvivalRecordFile>(File.ReadAllText(PathName));
                    if (data != null)
                    {
                        // JsonUtility materializes a null nested class as an empty object.
                        if (data.best != null && string.IsNullOrEmpty(data.best.playedAtUtc)) data.best = null;
                        return data;
                    }
                }
            }
            catch (Exception error) { Debug.LogWarning("생존 기록 읽기 실패: " + error.Message); }
            return new SurvivalRecordFile();
        }

        public static SurvivalRecord Best => Read().best;
        public static IReadOnlyList<SurvivalRecord> Recent => Read().recent;

        public static bool Save(SurvivalRecord record)
        {
            var data = Read();
            bool isBest = data.best == null || record.survivalSeconds > data.best.survivalSeconds;
            if (isBest) data.best = record;
            record.playedAtUtc = DateTime.UtcNow.ToString("o");
            if (data.recent == null) data.recent = new List<SurvivalRecord>();
            data.recent.Insert(0, record);
            if (data.recent.Count > 5) data.recent.RemoveRange(5, data.recent.Count - 5);
            Write(data);
            return isBest;
        }

        public static void Clear() => Write(new SurvivalRecordFile());

        private static void Write(SurvivalRecordFile data)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temp = PathName + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(data, true));
                if (File.Exists(PathName)) File.Replace(temp, PathName, null);
                else File.Move(temp, PathName);
            }
            catch (Exception error) { Debug.LogWarning("생존 기록 저장 실패: " + error.Message); }
        }

        public static string TimeLabel(float seconds)
        {
            int total = Mathf.FloorToInt(Mathf.Max(0, seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
