UnityEditor.EditorApplication.isPaused=true;
var rec=UnityEngine.Object.FindFirstObjectByType<ArmySurvivor.Army.RecruitmentController>();
foreach(var pair in rec.Units)UnityEngine.Object.DestroyImmediate(pair.Key.gameObject);rec.Units.Clear();
int i=0;foreach(var name in new[]{"Soldier","Archer","Shield","Mage","Cavalry"}){
 var d=UnityEditor.AssetDatabase.LoadAssetAtPath<ArmySurvivor.Army.UnitDefinition>("Assets/ArmyData/"+name+".asset");
 var root=new UnityEngine.GameObject("Test "+name).transform;root.SetParent(rec.SoldiersRoot);root.position=new UnityEngine.Vector3((i++-2)*4,0,3);
 var model=UnityEngine.Object.Instantiate(d.prefab,root);model.transform.localPosition=d.modelOffset;model.transform.localRotation=UnityEngine.Quaternion.Euler(d.modelRotation);model.transform.localScale=d.modelScale;
 rec.Units.Add(root,d);
}
var run=UnityEngine.Object.FindFirstObjectByType<ArmySurvivor.Army.RunController>();run.BeginRun();
var sb=new System.Text.StringBuilder();
var all=new System.Collections.Generic.List<UnityEngine.Transform>(rec.Units.Keys);all.Add(run.Commander);
foreach(var unit in all){
 var h=unit.GetComponent<ArmySurvivor.Army.UnitHealth>();h.TakeDamage(999999);
 foreach(var a in unit.GetComponentsInChildren<UnityEngine.Animator>()){
  a.Update(0.8f);var state=a.GetCurrentAnimatorStateInfo(0);
  if(!state.IsName("Death"))throw new System.Exception(unit.name+" did not enter Death");
  int matched=0,missing=0;foreach(var binding in UnityEditor.AnimationUtility.GetCurveBindings(a.GetCurrentAnimatorClipInfo(0)[0].clip)){if(binding.path==""||a.transform.Find(binding.path)!=null)matched++;else missing++;}
  sb.AppendLine(unit.name+" Death duration="+h.DeathDuration+" bindings="+matched+" missing="+missing);
 }
}
return sb.ToString();
