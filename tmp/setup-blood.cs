var source=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/FX/FX_prefabs/FX_Blood.prefab");
var obj=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source);
UnityEditor.PrefabUtility.UnpackPrefabInstance(obj,UnityEditor.PrefabUnpackMode.Completely,UnityEditor.InteractionMode.AutomatedAction);
var renderer=obj.GetComponentInChildren<UnityEngine.ParticleSystemRenderer>();var original=renderer.sharedMaterial;
var mat=new UnityEngine.Material(UnityEngine.Shader.Find("Universal Render Pipeline/Particles/Unlit"));mat.name="Blood";
mat.SetTexture("_BaseMap",original.mainTexture);mat.SetColor("_BaseColor",original.HasProperty("_TintColor")?original.GetColor("_TintColor"):UnityEngine.Color.white);
mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;mat.SetOverrideTag("RenderType","Transparent");
UnityEditor.AssetDatabase.CreateAsset(mat,"Assets/CombatData/Blood.mat");renderer.sharedMaterial=mat;
foreach(var ps in obj.GetComponentsInChildren<UnityEngine.ParticleSystem>()){var main=ps.main;main.loop=false;main.playOnAwake=true;}
obj.name="FX_Blood";var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(obj,"Assets/Prefabs/Combat/FX_Blood.prefab");UnityEngine.Object.DestroyImmediate(obj);
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/PlayScene.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
ArmySurvivor.Army.CombatController combat=null;foreach(var root in scene.GetRootGameObjects()){combat=root.GetComponentInChildren<ArmySurvivor.Army.CombatController>(true);if(combat!=null)break;}
var so=new UnityEditor.SerializedObject(combat);so.FindProperty("bloodEffect").objectReferenceValue=prefab;so.FindProperty("bloodHeight").floatValue=1;so.FindProperty("bloodScale").floatValue=1;so.FindProperty("bloodLifetime").floatValue=1.5f;so.ApplyModifiedPropertiesWithoutUndo();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);UnityEditor.AssetDatabase.SaveAssets();return "Blood prefab, URP material and scene reference saved";
