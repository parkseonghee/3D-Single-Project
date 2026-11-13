using System;
using UnityEngine;

namespace ArmySurvivor.Saving
{
    // 모험 회차 상태(기획서 8 재도전권). 씬 전환 사이에 유지되는 실행 중 값이며,
    // 디스크의 기준값은 자동 저장 파일(GameSave.AutoSavePath)이다.
    public static class CampaignState
    {
        public static string Id { get; private set; }
        public static int RetryTokens { get; private set; }
        public static int MaxRetryTokens { get; private set; }
        public static bool Active => !string.IsNullOrEmpty(Id);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Clear();

        public static void Clear()
        {
            Id = null;
            RetryTokens = MaxRetryTokens = 0;
        }

        public static void StartNew(int tokens)
        {
            Id = Guid.NewGuid().ToString("N");
            RetryTokens = MaxRetryTokens = Mathf.Max(0, tokens);
        }

        public static void Restore(string id, int tokens, int maxTokens)
        {
            Id = id;
            MaxRetryTokens = Mathf.Max(0, maxTokens);
            RetryTokens = Mathf.Clamp(tokens, 0, MaxRetryTokens);
        }

        internal static void SetTokens(int tokens) => RetryTokens = Mathf.Clamp(tokens, 0, MaxRetryTokens);
    }
}
