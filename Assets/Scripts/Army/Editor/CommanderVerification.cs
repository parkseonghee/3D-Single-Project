#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using ArmySurvivor.Army;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.EditorTools
{
    public static class CommanderVerification
    {
        private const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static void Set(object o, string n, object v) => o.GetType().GetField(n, F).SetValue(o, v);
        private static object Get(object o, string n) => o.GetType().GetField(n, F).GetValue(o);
        private static object Call(object o, string n, params object[] args) => o.GetType().GetMethod(n, F).Invoke(o, args);
        private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        [MenuItem("Tools/Army Survivor/Verify Commander Support")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first");
            float scale = Time.timeScale;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            EnemyDefinition def = null;
            try
            {
                var root = new GameObject("Commander verification");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var run = root.AddComponent<RunController>();
                var recruit = root.AddComponent<RecruitmentController>();
                var exp = root.AddComponent<BattleExperience>();
                var combat = root.AddComponent<CombatController>();
                var support = root.AddComponent<CommanderProgression>();
                Func<string, UnitHealth> unit = name => {
                    var go = new GameObject(name); go.transform.SetParent(root.transform);
                    var hp = go.AddComponent<UnitHealth>(); hp.Initialize(100, Color.green); return hp;
                };
                var commander = unit("Commander"); var soldier = unit("Soldier"); var enemy = unit("Enemy");
                Set(run, "commander", commander.transform); Set(run, "recruitment", recruit);
                Set(run, "<IsRunning>k__BackingField", true);
                Set(run, "tactics", new[] { new RunController.Tactic { radius = 3 } });
                Set(run, "commanderProgression", support);
                recruit.Units.Add(soldier.transform, AssetDatabase.LoadAssetAtPath<UnitDefinition>("Assets/ArmyData/Soldier.asset"));
                Set(exp, "run", run); Set(exp, "recruitment", recruit); Set(exp, "combat", combat);
                Set(combat, "run", run); Set(combat, "recruitment", recruit); Set(combat, "experience", exp);
                Call(support, "Awake");
                Set(support, "font", AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Fonts/Village Korean SDF.asset"));
                Call(support, "Begin");
                for (int i = 0; i < 14; i++) support.GainPickup();
                Check(support.Level == 3 && support.Experience == 10 && support.Required == 110, "EXP rollover");
                run.IsChoosingUpgrade = true; Call(support, "Update");
                Check(!support.IsChoosing, "Must wait for other choice");
                run.IsChoosingUpgrade = false; Call(support, "OpenChoice");
                Check(support.IsChoosing && Time.timeScale == 0, "Pause for support choice");
                var choices = (IList)Get(support, "offered");
                Check(choices.Count == 3 && !Equals(choices[0], choices[1]) && !Equals(choices[0], choices[2]) && !Equals(choices[1], choices[2]), "Unique choices");
                Check(support.Choose(0) && !support.Choose(0) && !run.IsChoosingUpgrade, "Choice once and resume");
                var ranks = (int[])Get(support, "ranks"); Array.Clear(ranks, 0, ranks.Length);
                ranks[0] = ranks[1] = ranks[2] = ranks[3] = ranks[4] = ranks[5] = 1;
                Check(Mathf.Approximately(support.PickupMultiplier, 1.4f) && Mathf.Approximately(support.InvestmentMultiplier, 1.2f), "Pickup/education modifiers");
                Check(support.VictoryGold == 50, "Victory gold");
                var label = new GameObject("Bottle Label", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                label.transform.SetParent(root.transform); Set(exp, "bottleCount", label);
                var progress = new BattleExperience.UnitProgress(100, 2);
                ((IDictionary)Get(exp, "units")).Add(soldier.transform, progress);
                int commanderExp = support.Experience;
                exp.GrantBottles(1);
                Check(support.Experience == commanderExp, "Granted bottles do not duplicate commander EXP");
                Check(exp.Invest(soldier.transform) && progress.Experience == 120 && exp.Bottles == 0, "Education applies to actual investment");
                Check(support.Experience == commanderExp, "Investment does not duplicate commander EXP");
                commander.TakeDamage(75);
                Check(Mathf.Approximately(run.TeamMoveMultiplier, 1.5f), "Emergency retreat");
                Set(support, "retreatRemaining", 0f); commander.TakeDamage(1);
                Check(support.MoveMultiplier == 1, "Retreat cooldown");
                soldier.TakeDamage(200); Check((UnitHealth)Get(support, "reviveTarget") == soldier, "Reserve queues dead soldier");
                Set(support, "reviveRemaining", -1f); Set(support, "pending", 0); Call(support, "Update");
                Check(!soldier.IsDead && soldier.Current == 50, "Reserve revives at 50%");
                soldier.TakeDamage(200); Check(Get(support, "reviveTarget") == null, "Reserve only once");
                def = ScriptableObject.CreateInstance<EnemyDefinition>();
                var type = typeof(CombatController).GetNestedType("EnemyState", BindingFlags.NonPublic);
                var target = Activator.CreateInstance(type, true);
                Set(target, "root", enemy.transform); Set(target, "vitality", enemy); Set(target, "definition", def); Set(target, "isBoss", true);
                Call(combat, "ApplyDamage", target, 10f, Vector3.zero, 0f, false, null, false, false);
                Check(Mathf.Approximately(enemy.Current, 88), "Hunter applies +20% to bosses");
                Set(target, "isBoss", false);
                Call(combat, "ApplyDamage", target, 10f, Vector3.zero, 0f, false, null, false, false);
                Check(Mathf.Approximately(enemy.Current, 78), "Normal enemies unchanged");
                for (int i = 0; i < ranks.Length; i++) ranks[i] = i == 3 ? 1 : 3;
                Set(support, "pending", 1); Call(support, "OpenChoice");
                Check(!support.IsChoosing && (int)Get(support, "pending") == 0, "All maxed does not trap player");
                Call(support, "End");
                Check(support.Level == 1 && support.Experience == 0 && support.PickupMultiplier == 1 && support.VictoryGold == 0 && support.MoveMultiplier == 1, "End cleanup");
                Debug.Log("COMMANDER_SUPPORT_VERIFICATION_PASSED: EXP, selection queue, 6 support effects, revive once, caps, cleanup");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                if (def != null) UnityEngine.Object.DestroyImmediate(def);
                Time.timeScale = scale;
            }
        }
    }
}
#endif
