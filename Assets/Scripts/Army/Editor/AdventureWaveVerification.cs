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
            for (int day = 1; day <= settings.finalDay; day++)
            {
                var stage = settings.GetStage(day);
                var rule = new AdventureRhythmRule(stage.enemy, stage.enemies, stage.survivalSeconds,
                    stage.spawnInterval, stage.maximumEnemies, settings, day);
                var seen = new HashSet<int>();
                int specialWindows = 0;
                bool wasSpecial = false;
                for (int step = 0; step < 1000; step++)
                {
                    var state = rule.Evaluate(stage.survivalSeconds * step / 1000f, false);
                    seen.Add(state.number);
                    Check(state.capacity <= settings.enemySafetyLimit && state.batch >= 0 && state.interval > 0, "Unsafe wave limits");
                    Check(rule.ChooseEnemy(0) != null, "Missing enemy");
                    bool special = rule.SpecialWaveActive;
                    if (special && !wasSpecial) specialWindows++;
                    wasSpecial = special;
                    if (special)
                    {
                        Check(state.batch == 0, "Special wave must not spawn ordinary batches");
                        Check(state.capacity >= Mathf.Min(settings.enemySafetyLimit, stage.maximumEnemies + settings.specialWaveCount), "Special wave needs room");
                        Check(rule.HealthMultiplier < 1 && rule.DamageMultiplier < 1, "Special burst must use its own weaker stats");
                    }
                    if (rule.CurrentPhase == AdventureRhythmRule.Phase.Reward || rule.CurrentPhase == AdventureRhythmRule.Phase.Rest)
                        Check(state.batch == 0, "Rest still spawns");
                }
                Check(seen.Count == (day <= 10 ? 2 : day <= 20 ? 3 : 4), "Day wave count " + day);
                Check(specialWindows == 1, "Expected one special window on Day " + day);
                Check(rule.Evaluate(stage.survivalSeconds + 10, true).batch == 0, "Boss phase spawns normal enemies");
                Check(settings.HealthScale(day + 1) > settings.HealthScale(day) && settings.DamageScale(day + 1) > settings.DamageScale(day), "Daily growth");
                rule.Evaluate(stage.survivalSeconds * .95f, false);
                string expected = day >= 21 ? "포위" : day >= 11 ? "양면 협공" : day >= 5 ? "갈라진 전선" : "단일 전선";
                Check(rule.PatternName == expected, "Pattern unlock " + day);
            }
            var first = settings.GetStage(1);
            var example = new AdventureRhythmRule(first.enemy, first.enemies, 90, 2.5f, 12, settings, 1);
            Check(!string.IsNullOrEmpty(AdventureRhythmRule.SpecialName(example.SelectedSpecialPattern)),
                "Special wave needs an announcement name");
            Check(example.EncirclementSpawnRadius > example.EncirclementReleaseRadius &&
                example.EncirclementClosingSpeed > 0 && example.SpecialWarningSeconds < 90 * settings.specialWaveDurationProgress,
                "Encirclement warning or closing parameters are invalid");
            float[] times = { 0, 15.01f, 30.01f, 40.01f, 45.01f, 60.01f, 70.01f, 90 };
            var phases = new[] { AdventureRhythmRule.Phase.Warmup, AdventureRhythmRule.Phase.Swarm, AdventureRhythmRule.Phase.Reward,
                AdventureRhythmRule.Phase.Tactical, AdventureRhythmRule.Phase.Special, AdventureRhythmRule.Phase.Rest,
                AdventureRhythmRule.Phase.Finale, AdventureRhythmRule.Phase.Boss };
            for (int i = 0; i < times.Length; i++) { example.Evaluate(times[i], false); Check(example.CurrentPhase == phases[i], "90s timeline " + times[i]); }
            example.Evaluate(20, false); Check(example.HealthMultiplier < 1 && example.DamageMultiplier < 1, "Swarm should be weak");
            example.Evaluate(42, false); Check(example.HealthMultiplier == 1 && example.DamageMultiplier == 1, "Tactical stats should be normal");
            example.Evaluate(20, false);
            var directions = new Vector3[7];
            for (int i = 0; i < directions.Length; i++) directions[i] = example.Direction(i);
            float spread = 0;
            for (int i = 0; i < directions.Length; i++)
                for (int j = i + 1; j < directions.Length; j++)
                    spread = Mathf.Max(spread, Vector3.Angle(directions[i], directions[j]));
            Check(spread >= settings.spawnArcDegrees * .7f, "Adventure spawn arc is too narrow");
            foreach (AdventureRhythmRule.SpecialPattern pattern in Enum.GetValues(typeof(AdventureRhythmRule.SpecialPattern)))
            {
                var formation = new AdventureRhythmRule(first.enemy, first.enemies, 90, 2.5f, 12, settings, 1, pattern);
                formation.Evaluate(50, false);
                Check(formation.SpecialWaveActive && formation.SelectedSpecialPattern == pattern, "Special pattern unavailable");
                var bearings = new Vector3[settings.specialWaveCount];
                int ranged = 0, hunters = 0, elites = 0, rings = 0;
                float latestDelay = 0;
                for (int i = 0; i < bearings.Length; i++)
                {
                    var order = formation.GetSpecialOrder(i);
                    bearings[i] = order.direction;
                    Check(Mathf.Abs(bearings[i].magnitude - 1) < .001f, "Invalid special direction");
                    Check(order.delay >= latestDelay && order.delay < 90 * settings.specialWaveDurationProgress,
                        "Special spawn delay is out of order: " + pattern);
                    Check(order.speed > 0 && order.health > 0 && order.radius >= 3, "Invalid special stats: " + pattern);
                    latestDelay = order.delay;
                    if (order.ranged) ranged++;
                    if (order.hunter) hunters++;
                    if (order.elite) elites++;
                    if (order.ring) rings++;
                }
                float widest = 0;
                for (int i = 0; i < bearings.Length; i++)
                    for (int j = i + 1; j < bearings.Length; j++)
                        widest = Mathf.Max(widest, Vector3.Angle(bearings[i], bearings[j]));
                Check(widest >= 40, "Special formation is too narrow: " + pattern);
                if (pattern == AdventureRhythmRule.SpecialPattern.RangedSiege) Check(ranged > 0 && rings > 0, "Ranged siege roles missing");
                if (pattern == AdventureRhythmRule.SpecialPattern.CommanderHunt) Check(hunters == bearings.Length, "Hunters missing");
                if (pattern == AdventureRhythmRule.SpecialPattern.EliteEscort || pattern == AdventureRhythmRule.SpecialPattern.EliteReinforcement)
                    Check(elites == 1, "Expected one elite in " + pattern);
                if (pattern == AdventureRhythmRule.SpecialPattern.Encirclement)
                    for (int i = 0; i < bearings.Length; i++)
                        Check(Mathf.Abs(Vector3.Angle(bearings[i], bearings[(i + 1) % bearings.Length]) - 360f / bearings.Length) < .01f,
                            "Encirclement enemies are not evenly spaced");
            }
            Debug.Log($"ADVENTURE_WAVES_VERIFICATION_PASSED: days 1-{settings.finalDay}, one special window, 16 formations, wave limits, timeline, scaling");
        }
    }
}
#endif
