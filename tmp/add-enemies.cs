var names=new[]{"Ninja","Orc_Skull","Yeti"};
var hp=new[]{40f,90f,180f};var damage=new[]{8f,16f,24f};var speed=new[]{3.6f,2f,1.4f};var interval=new[]{1f,1.4f,2.2f};
var stageData=UnityEditor.AssetDatabase.LoadAssetAtPath<ArmySurvivor.Army.DaySettings>("Assets/CombatData/DaySettings.asset");
var definitions=new ArmySurvivor.Army.EnemyDefinition[3,3];var report=new System.Text.StringBuilder();
for(int i=0;i<names.Length;i++){
 string name=names[i];string sourcePath="Assets/Monster/Big/FBX/"+name+".fbx";
 string controllerPath="Assets/Animations/Combat/"+name+".controller";
 if(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(controllerPath)==null)UnityEditor.AssetDatabase.CopyAsset("Assets/Animations/Combat/Orc.controller",controllerPath);
 var controller=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
 foreach(var child in controller.layers[0].stateMachine.states){
  var state=child.state;string sourceName=state.name=="Attack"?(name=="Yeti"?"Punch":"Weapon"):state.name=="Walk"?(name=="Ninja"?"Run":"Walk"):state.name;
  UnityEngine.AnimationClip source=null;foreach(var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sourcePath))if(a is UnityEngine.AnimationClip && a.name=="CharacterArmature|"+sourceName)source=(UnityEngine.AnimationClip)a;
  if(source==null)throw new System.Exception("Missing clip "+name+" "+sourceName);
  string clipPath="Assets/Animations/Combat/"+name+state.name+".anim";
  var clip=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(clipPath);
  if(clip==null){clip=UnityEngine.Object.Instantiate(source);clip.name=name+state.name;UnityEditor.AssetDatabase.CreateAsset(clip,clipPath);}
  var setting=UnityEditor.AnimationUtility.GetAnimationClipSettings(clip);setting.loopTime=state.name=="Walk"||state.name=="Idle";UnityEditor.AnimationUtility.SetAnimationClipSettings(clip,setting);
  UnityEditor.AnimationUtility.SetAnimationEvents(clip,new UnityEngine.AnimationEvent[0]);state.motion=clip;state.speed=name=="Yeti"&&state.name=="Attack"?0.75f:1;
  UnityEditor.EditorUtility.SetDirty(state);UnityEditor.EditorUtility.SetDirty(clip);
 }
 UnityEditor.EditorUtility.SetDirty(controller);
 var model=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(sourcePath));
 UnityEditor.PrefabUtility.UnpackPrefabInstance(model,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);model.name=name;
 var animator=model.GetComponentInChildren<UnityEngine.Animator>();if(animator==null)animator=model.AddComponent<UnityEngine.Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
 var materialPath="Assets/CombatData/"+name+"Material.mat";
 var material=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
 if(material==null){material=new UnityEngine.Material(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/CombatData/OrcMaterial.mat"));material.name=name+"Material";UnityEditor.AssetDatabase.CreateAsset(material,materialPath);}
 foreach(var r in model.GetComponentsInChildren<UnityEngine.Renderer>()){
  var mats=r.sharedMaterials;for(int m=0;m<mats.Length;m++)mats[m]=material;r.sharedMaterials=mats;
 }
 string prefabPath="Assets/Prefabs/Combat/"+name+".prefab";
 var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(model,prefabPath);UnityEngine.Object.DestroyImmediate(model);
 for(int tier=0;tier<3;tier++){
  string path="Assets/CombatData/"+name+(tier==0?"":"Tier"+(tier+1))+".asset";
  var d=UnityEditor.AssetDatabase.LoadAssetAtPath<ArmySurvivor.Army.EnemyDefinition>(path);
  if(d==null){d=UnityEngine.ScriptableObject.CreateInstance<ArmySurvivor.Army.EnemyDefinition>();UnityEditor.AssetDatabase.CreateAsset(d,path);}
  d.prefab=prefab;d.health=UnityEngine.Mathf.Round(hp[i]*new[]{1f,1.5f,2.333333f}[tier]);d.damage=UnityEngine.Mathf.Round(damage[i]*new[]{1f,1.25f,1.5f}[tier]);
  d.speed=speed[i];d.attackInterval=interval[i];d.attackWindup=i==2?0.5f:0.35f;d.attackDuration=i==2?1.155556f:0.8666667f;d.attackState="Base Layer.Attack";d.stoppingDistance=i==2?1.3f:1.1f;d.turnSpeed=360;d.deathDuration=1.3f;d.movingParameter="Moving";d.deathTrigger="Death";
  UnityEditor.EditorUtility.SetDirty(d);definitions[i,tier]=d;report.AppendLine(d.name+" HP="+d.health+" damage="+d.damage);
 }
}
for(int tier=0;tier<stageData.stages.Length;tier++){
 var stage=stageData.stages[tier];int t=System.Math.Min(tier,2);
 stage.enemies=new[]{new ArmySurvivor.Army.DaySettings.EnemySpawn{enemy=stage.enemy,weight=50},new ArmySurvivor.Army.DaySettings.EnemySpawn{enemy=definitions[0,t],weight=25},new ArmySurvivor.Army.DaySettings.EnemySpawn{enemy=definitions[1,t],weight=15},new ArmySurvivor.Army.DaySettings.EnemySpawn{enemy=definitions[2,t],weight=10}};
}
UnityEditor.EditorUtility.SetDirty(stageData);UnityEditor.AssetDatabase.SaveAssets();return report.ToString();

