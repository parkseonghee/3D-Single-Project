#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // 출전 미리보기(기획서 7.1) 데이터 검증. Day 1~finalDay 전체를 확인한다.
    public static class BattlePreviewVerification
    {
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

        [MenuItem("Tools/Army Survivor/Verify Battle Preview")]
        public static void Run()
        {
            var settings = AssetDatabase.LoadAssetAtPath<DaySettings>("Assets/CombatData/DaySettings.asset");
            Check(settings != null, "DaySettings missing");
            Check(settings.battlefieldModifiers != null && settings.battlefieldModifiers.Length > 1, "Need 2+ battlefield modifiers");
            foreach (var modifier in settings.battlefieldModifiers)
            {
                Check(modifier != null, "Null modifier slot");
                Check(!string.IsNullOrEmpty(modifier.displayName) && !string.IsNullOrEmpty(modifier.description), modifier.name + " text missing");
                Check(modifier.multiplier > 0, modifier.name + " multiplier must be positive");
                if (modifier.effect == BattlefieldModifierDefinition.Effect.EnemySpawnWeight)
                    Check(modifier.targets != null && modifier.targets.Length > 0, modifier.name + " needs targets");
            }
            var shares = new List<DaySettings.EnemyShare>();
            var used = new HashSet<BattlefieldModifierDefinition>();
            BattlefieldModifierDefinition previous = null;
            for (int day = 1; day <= settings.finalDay; day++)
            {
                var stage = settings.GetStage(day);
                var modifier = settings.GetBattlefieldModifier(day);
                Check(modifier == settings.GetBattlefieldModifier(day), $"Day {day} modifier not deterministic");
                if (day < settings.firstModifierDay) Check(modifier == null, $"Day {day} should have no modifier");
                else
                {
                    Check(modifier != null && modifier.IsAvailable(stage, day), $"Day {day} invalid modifier");
                    Check(modifier != previous, $"Day {day} repeats previous modifier");
                    used.Add(modifier);
                }
                previous = modifier;

                settings.GetEnemyShares(day, modifier, shares);
                Check(shares.Count > 0, $"Day {day} has no enemies");
                float total = 0;
                bool boosted = false;
                foreach (var item in shares)
                {
                    Check(item.enemy != null && !string.IsNullOrEmpty(item.enemy.displayName), $"Day {day} enemy displayName missing");
                    total += item.share;
                    boosted |= item.boosted;
                }
                Check(Mathf.Abs(total - 1f) < .001f, $"Day {day} shares sum {total}");
                bool weightModifier = modifier != null && modifier.effect == BattlefieldModifierDefinition.Effect.EnemySpawnWeight;
                Check(boosted == weightModifier, $"Day {day} boosted marker mismatch");
                if (day % settings.bossInterval == 0)
                    Check(stage.boss != null && !string.IsNullOrEmpty(stage.boss.displayName), $"Day {day} boss name missing");
            }
            Debug.Log($"BATTLE_PREVIEW_VERIFICATION_PASSED: days 1-{settings.finalDay}, {used.Count}/{settings.battlefieldModifiers.Length} modifiers used, no back-to-back repeats");
        }
    }
}
#endif
