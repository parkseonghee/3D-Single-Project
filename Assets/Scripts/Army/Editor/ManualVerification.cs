#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using ArmySurvivor.Army;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArmySurvivor.EditorTools
{
    // Runs in an isolated preview scene; never loads, saves or modifies user scenes/assets.
    public static class ManualVerification
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static int checks;
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); checks++; }
        private static void Near(float actual, float expected, string message) => Check(Mathf.Abs(actual-expected)<0.01f, message+": "+actual+" expected "+expected);
        private static void Set(object obj, string name, object value) => obj.GetType().GetField(name, Flags).SetValue(obj, value);
        private static object Get(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
        private static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, Flags).Invoke(obj, args);

        [MenuItem("Tools/Army Survivor/Verify Manuals")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before running manual verification.");
            checks = 0;
            using (var f = new Fixture())
            {
                f.VerifyAssets();
                f.VerifyFire(); f.VerifyWall(); f.VerifyRelay(); f.VerifyPursuit();
                f.VerifyCounter(); f.VerifyFocus(); f.VerifyEqual();
            }
            Debug.Log("MANUAL_VERIFICATION_PASSED checks="+checks);
        }

        private sealed class Fixture : IDisposable
        {
            private readonly UnityEngine.SceneManagement.Scene scene;
            private readonly GameObject root;
            private readonly RunController run;
            private readonly RecruitmentController recruitment;
            private readonly BattleExperience experience;
            private readonly CombatController combat;
            private readonly ManualManager manager;
            private readonly Dictionary<string, Transform> units = new Dictionary<string, Transform>();
            private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
            private readonly Type enemyType = typeof(CombatController).GetNestedType("EnemyState", BindingFlags.NonPublic);
            private ManualEffect active;
            public Fixture()
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                root = new GameObject("Manual Verification Fixture");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                run = root.AddComponent<RunController>(); recruitment = root.AddComponent<RecruitmentController>();
                experience = root.AddComponent<BattleExperience>(); combat = root.AddComponent<CombatController>();
                manager = root.AddComponent<ManualManager>();
                Set(run,"commander", Unit("Commander",1000));
                Set(run,"recruitment",recruitment);
                Set(run,"<IsRunning>k__BackingField",true);
                Set(run,"formationRing",root.AddComponent<LineRenderer>());
                var tactics = new RunController.Tactic[3];
                for(int i=0;i<3;i++) tactics[i]=new RunController.Tactic { selected=Child("Tactic"+i),radius=3,damageMultiplier=1 };
                Set(run,"tactics",tactics);
                var ground = Child("Ground").AddComponent<BoxCollider>(); ground.size=Vector3.one*100;
                Set(run,"ground",ground);
                Set(recruitment,"soldiersRoot",Child("Soldiers").transform);
                Set(experience,"run",run); Set(experience,"combat",combat); Set(experience,"recruitment",recruitment);
                Set(combat,"run",run); Set(combat,"recruitment",recruitment); Set(combat,"experience",experience);
                Call(manager,"Awake");
                foreach(var name in new[]{"Soldier","Archer","Mage","Shield","Cavalry"})
                {
                    var unit=Unit(name,1000); unit.SetParent(recruitment.SoldiersRoot,false); units.Add(name,unit);
                    recruitment.Units.Add(unit,AssetDatabase.LoadAssetAtPath<UnitDefinition>("Assets/ArmyData/"+name+".asset"));
                    ((IDictionary)Get(experience,"units")).Add(unit,new BattleExperience.UnitProgress(100,2));
                }
            }
            private GameObject Child(string name)
            { var obj=new GameObject(name); obj.transform.SetParent(root.transform,false); return obj; }
            private Transform Unit(string name,float hp)
            { var obj=Child(name); obj.AddComponent<UnitHealth>().Initialize(hp,Color.green); return obj.transform; }
            private object Enemy(float hp=500, float x=0)
            {
                var target=Unit("Enemy",hp); target.position=new Vector3(x,0,0);
                var enemy=Activator.CreateInstance(enemyType,true);
                var definition=ScriptableObject.CreateInstance<EnemyDefinition>(); temporary.Add(definition);
                Set(enemy,"root",target); Set(enemy,"vitality",target.GetComponent<UnitHealth>()); Set(enemy,"definition",definition);
                ((IList)Get(combat,"enemies")).Add(enemy); return enemy;
            }
            private float Hp(object enemy) => ((Transform)Get(enemy,"root")).GetComponent<UnitHealth>().Current;
            private void Hit(object target,string type,float damage=10,bool charge=false)
                => Call(combat,"ApplyDamage",target,damage,Vector3.zero,0f,false,units[type],false,charge);
            private TacticalManualEffect Start(string name)
            {
                Stop(); ((IList)Get(combat,"enemies")).Clear();
                var data=Object.Instantiate(AssetDatabase.LoadAssetAtPath<TacticalManualEffect>("Assets/ManualData/"+name+"Effect.asset"));
                temporary.Add(data); data.burstEffect=null; data.burningEffect=null;
                active=data.CreateEffect(manager); active.OnBattleStart(); return data;
            }
            private void Stop()
            {
                if(active==null)return; active.OnBattleEnd(); active=null;
                Check(((IDictionary)Get(combat,"manualBuffs")).Count==0,"buff cleanup");
                Check(((IDictionary)Get(combat,"manualCharges")).Count==0,"charge cleanup");
                Check(Get(combat,"OnManualEligibleHit")==null,"hit event unsubscribe");
            }
            public void VerifyAssets()
            {
                foreach(var name in new[]{"EqualEducation","FireVolley","WallBreak","RelayVanguard","Pursuit","FormationCounter","FocusedEducation"})
                {
                    var data=AssetDatabase.LoadAssetAtPath<ManualDefinition>("Assets/ManualData/"+name+".asset");
                    Check(data!=null&&data.effect!=null&&data.IsEligible(recruitment),"asset/roster: "+name);
                    Check(!string.IsNullOrEmpty(data.effect.Describe(data.description)),"description: "+name);
                    if(data.effect is TacticalManualEffect effect) Check(effect.burstEffect!=null,"VFX ref: "+name);
                }
                units["Mage"].gameObject.SetActive(false);
                Check(!AssetDatabase.LoadAssetAtPath<ManualDefinition>("Assets/ManualData/FireVolley.asset").IsEligible(recruitment),"missing class filtered");
                units["Mage"].gameObject.SetActive(true);
            }
            public void VerifyFire()
            {
                Start("FireVolley"); var a=Enemy();var b=Enemy(500,1);
                Hit(a,"Mage");active.Tick(.5f);Near(Hp(a),484,"mage ignition DOT");Near(Hp(b),500,"no early spread");
                Hit(a,"Archer");active.Tick(.5f);Near(Hp(b),494,"archer spreads DOT");
                active.Tick(5);float ended=Hp(b);active.Tick(2);Near(Hp(b),ended,"burn expiry");Stop();
            }
            public void VerifyWall()
            {
                var hp=units["Cavalry"].GetComponent<UnitHealth>();hp.GiveShield(100,30,null);
                Start("WallBreak");Near(hp.Shield,140,"manual shield coexists");
                var a=Enemy();var b=Enemy(500,1);Hit(a,"Cavalry",10,true);
                Near(hp.Shield,100,"charge consumes manual shield first");Near(Hp(b),440,"shield shockwave");
                active.Tick(8);Near(hp.Shield,140,"shield resupply");Stop();Near(hp.Shield,100,"other shield preserved");
            }
            public void VerifyRelay()
            {
                Start("RelayVanguard");var a=Enemy();Hit(a,"Soldier");Hit(a,"Shield");Near(Hp(a),474,"alternating bonus");
                Hit(a,"Shield");Near(Hp(a),464,"same class no bonus");active.Tick(1);Hit(a,"Soldier");Near(Hp(a),448,"rearmed relay");Stop();
            }
            public void VerifyPursuit()
            {
                Start("Pursuit");var a=Enemy(30);Hit(a,"Archer");Near((float)Get(a,"suppressionRemaining"),3,"manual suppression");
                Hit(a,"Cavalry",50,true);Near((float)Call(combat,"ConsumeManualCharge",units["Cavalry"]),1.75f,"next charge empowered");
                Near((float)Call(combat,"ConsumeManualCharge",units["Cavalry"]),1,"charge bonus single use");Stop();
            }
            public void VerifyCounter()
            {
                Start("FormationCounter");run.SelectTactic(0);var a=Enemy();
                var hp=run.Commander.GetComponent<UnitHealth>();hp.GiveShield(50,30,null);hp.TakeDamage(20);
                run.SelectTactic(2);Near(Hp(a),470,"shield absorption counter");run.SelectTactic(0);run.SelectTactic(2);
                Near(Hp(a),470,"stored damage spent once");Stop();Check(Get(run,"OnFormationChanged")==null,"formation unsubscribe");
            }
            private void Invest(string type) => ((Action<Transform>)Get(experience,"OnExpBottleInvested"))?.Invoke(units[type]);
            public void VerifyFocus()
            {
                Start("FocusedEducation");Invest("Soldier");Invest("Archer");Invest("Soldier");Near(manager.Progress,1,"focus chain reset");
                Invest("Soldier");Invest("Soldier");
                Near(((Vector2)Call(combat,"ManualBonus",units["Soldier"])).x,.6f,"focus target buff");
                Near(((Vector2)Call(combat,"ManualBonus",units["Archer"])).x,0,"focus excludes other soldiers");
                active.Tick(8.1f);Near(((Vector2)Call(combat,"ManualBonus",units["Soldier"])).x,0,"focus expiry");Stop();
                Check(Get(experience,"OnExpBottleInvested")==null,"focus unsubscribe");
            }
            public void VerifyEqual()
            {
                var data=Object.Instantiate(AssetDatabase.LoadAssetAtPath<EqualEducationEffect>("Assets/ManualData/EqualEducationEffect.asset"));
                temporary.Add(data);data.buffEffect=null;active=data.CreateEffect(manager);active.OnBattleStart();
                Invest("Soldier");Invest("Soldier");Near(manager.Progress,1,"equal duplicate reset");Invest("Archer");Invest("Mage");
                Near(manager.Remaining,8,"equal activated");Near(((Vector2)Call(combat,"ManualBonus",units["Cavalry"])).x,.3f,"equal all soldiers");
                active.Tick(3);Invest("Soldier");Invest("Archer");Invest("Mage");Near(manager.Remaining,8,"equal refresh");
                Near(((Vector2)Call(combat,"ManualBonus",units["Soldier"])).x,.3f,"equal no stacking");
                active.Tick(9);Near(((Vector2)Call(combat,"ManualBonus",units["Soldier"])).x,0,"equal expiry");Stop();
            }
            public void Dispose()
            {
                Stop();
                foreach(var data in temporary)if(data!=null)Object.DestroyImmediate(data);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
#endif
