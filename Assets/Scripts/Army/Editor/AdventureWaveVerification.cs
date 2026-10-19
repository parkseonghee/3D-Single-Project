#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Army
{
    public static class AdventureWaveVerification
    {
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        [MenuItem("Tools/Army Survivor/Verify Adventure Waves")]
        public static void Run()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DaySettings>("Assets/CombatData/DaySettings.asset");
            Check(settings != null, "Settings missing");
            for (int day = 1; day <= 100; day++)
            {
                var stage = settings.GetStage(day);
                var rule = new AdventureRhythmRule(stage.enemy, stage.enemies, stage.survivalSeconds,
                    stage.spawnInterval, stage.maximumEnemies, settings, day);
                var seen = new HashSet<int>();
                for (int step = 0; step < 1000; step++)
                {
                    var state = rule.Evaluate(stage.survivalSeconds * step / 1000f, false);
                    seen.Add(state.number);
                    Check(state.capacity <= settings.enemySafetyLimit && state.batch >= 0 && state.interval > 0, "Unsafe wave limits");
                    Check(rule.ChooseEnemy(0) != null, "Missing enemy");
                    if (rule.CurrentPhase == AdventureRhythmRule.Phase.Reward || rule.CurrentPhase == AdventureRhythmRule.Phase.Rest)
                        Check(state.batch == 0, "Rest still spawns");
                }
                Check(seen.Count == (day <= 10 ? 2 : day <= 20 ? 3 : 4), "Day wave count " + day);
                Check(rule.Evaluate(stage.survivalSeconds + 10, true).batch == 0, "Boss phase spawns normal enemies");
                Check(settings.HealthScale(day + 1) > settings.HealthScale(day) && settings.DamageScale(day + 1) > settings.DamageScale(day), "Daily growth");
                rule.Evaluate(stage.survivalSeconds * .95f, false);
                string expected = day >= 21 ? "포위" : day >= 11 ? "양면 협공" : day >= 5 ? "갈라진 전선" : "단일 전선";
                Check(rule.PatternName == expected, "Pattern unlock " + day);
            }
            var first = settings.GetStage(1);
            var example = new AdventureRhythmRule(first.enemy, first.enemies, 90, 2.5f, 12, settings, 1);
            float[] times = { 0, 15.01f, 30.01f, 40.01f, 60.01f, 70.01f, 90 };
            var phases = new[] { AdventureRhythmRule.Phase.Warmup, AdventureRhythmRule.Phase.Swarm, AdventureRhythmRule.Phase.Reward,
                AdventureRhythmRule.Phase.Tactical, AdventureRhythmRule.Phase.Rest, AdventureRhythmRule.Phase.Finale, AdventureRhythmRule.Phase.Boss };
            for (int i = 0; i < times.Length; i++) { example.Evaluate(times[i], false); Check(example.CurrentPhase == phases[i], "90s timeline " + times[i]); }
            example.Evaluate(20, false); Check(example.HealthMultiplier < 1 && example.DamageMultiplier < 1, "Swarm should be weak");
            example.Evaluate(50, false); Check(example.HealthMultiplier == 1 && example.DamageMultiplier == 1, "Tactical stats should be normal");
            Debug.Log("ADVENTURE_WAVES_VERIFICATION_PASSED: days 1-100, wave limits, timeline, rest, patterns, scaling, boss phase");
        }
    }
}
#endif
