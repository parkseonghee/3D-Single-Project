var pairs=new[]{
"Commander|animation_infantry/Infantry/infantry_06_death_A.FBX",
"Soldier|animation_infantry/Infantry/infantry_06_death_A.FBX",
"Archer|animation_infantry/Archer/archer_06_death_A.FBX",
"Shield|animation_infantry/Shield/shield_06_death_A.FBX",
"Mage|animation_infantry/Staff/staff_06_death_A.FBX",
"Cavalry|animation_cavalry/cavalry/cavalry_06_death_A.FBX"};
var sb=new System.Text.StringBuilder();
foreach(var pair in pairs){
 var parts=pair.Split('|'); UnityEngine.AnimationClip source=null;
 foreach(var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/animation/"+parts[1]))
  if(a is UnityEngine.AnimationClip && !a.name.StartsWith("__preview__")){source=(UnityEngine.AnimationClip)a;break;}
 if(source==null)throw new System.Exception("No death clip: "+pair);
 string path="Assets/Animations/Combat/"+parts[0]+"Death.anim";
 var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(path);
 if(clip==null){clip=UnityEngine.Object.Instantiate(source);clip.name=parts[0]+"Death";UnityEditor.AssetDatabase.CreateAsset(clip,path);}
 var settings=UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;UnityEditor.AnimationUtility.SetAnimationClipSettings(clip,settings);
 UnityEditor.AnimationUtility.SetAnimationEvents(clip,new UnityEngine.AnimationEvent[0]);
 var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/Army/"+parts[0]+"Idle.controller");
 var machine=controller.layers[0].stateMachine; UnityEditor.Animations.AnimatorState death=null;
 foreach(var s in machine.states)if(s.state.name=="Death")death=s.state;
 if(death==null)death=machine.AddState("Death"); death.motion=clip;
 UnityEditor.EditorUtility.SetDirty(controller); UnityEditor.EditorUtility.SetDirty(clip);
 sb.AppendLine(parts[0]+" death="+clip.length);
}
UnityEngine.AnimationClip attackSource=null;
foreach(var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Monster/Big/FBX/Orc.fbx"))if(a.name=="CharacterArmature|Weapon")attackSource=(UnityEngine.AnimationClip)a;
var attack=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/Animations/Combat/OrcAttack.anim");
if(attack==null){attack=UnityEngine.Object.Instantiate(attackSource);attack.name="OrcAttack";UnityEditor.AssetDatabase.CreateAsset(attack,"Assets/Animations/Combat/OrcAttack.anim");}
var aset=UnityEditor.AnimationUtility.GetAnimationClipSettings(attack);aset.loopTime=false;UnityEditor.AnimationUtility.SetAnimationClipSettings(attack,aset);UnityEditor.AnimationUtility.SetAnimationEvents(attack,new UnityEngine.AnimationEvent[0]);
var orc=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animations/Combat/Orc.controller");
var sm=orc.layers[0].stateMachine; UnityEditor.Animations.AnimatorState atk=null;
foreach(var s in sm.states)if(s.state.name=="Attack")atk=s.state;
if(atk==null){atk=sm.AddState("Attack"); var exit=atk.AddTransition(sm.defaultState);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0.05f;}
atk.motion=attack;
UnityEditor.EditorUtility.SetDirty(orc);UnityEditor.EditorUtility.SetDirty(attack);UnityEditor.AssetDatabase.SaveAssets();
return sb.ToString()+"Orc attack="+attack.length;
