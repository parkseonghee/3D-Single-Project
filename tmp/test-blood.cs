UnityEditor.EditorApplication.isPaused=true;
var run=UnityEngine.Object.FindFirstObjectByType<ArmySurvivor.Army.RunController>();run.BeginRun();
var c=UnityEngine.Object.FindFirstObjectByType<ArmySurvivor.Army.CombatController>();var f=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var d=UnityEditor.AssetDatabase.LoadAssetAtPath<ArmySurvivor.Army.EnemyDefinition>("Assets/CombatData/Orc.asset");
typeof(ArmySurvivor.Army.CombatController).GetMethod("Spawn",f,null,new[]{typeof(ArmySurvivor.Army.EnemyDefinition),typeof(bool)},null).Invoke(c,new object[]{d,false});
var list=(System.Collections.IList)typeof(ArmySurvivor.Army.CombatController).GetField("enemies",f).GetValue(c);var target=list[0];
var hit=typeof(ArmySurvivor.Army.CombatController).GetMethod("ApplyDamage",f);
var root=(UnityEngine.Transform)typeof(ArmySurvivor.Army.CombatController).GetField("combatRoot",f).GetValue(c);
for(int i=0;i<2;i++)hit.Invoke(c,new object[]{target,1f,UnityEngine.Vector3.forward,0f});
hit.Invoke(c,new object[]{target,10000f,UnityEngine.Vector3.forward,0f});
hit.Invoke(c,new object[]{target,1f,UnityEngine.Vector3.forward,0f});
var effects=root.GetComponentsInChildren<UnityEngine.ParticleSystem>();if(effects.Length!=3)throw new System.Exception("Expected 3 effects, got "+effects.Length);
foreach(var p in effects){p.Simulate(0.1f,true,true);if(p.particleCount==0)throw new System.Exception("No visible particles");}
return "PASS: 2 hits + killing hit = 3 blood effects; dead target ignored; particles emitted="+effects[0].particleCount;
