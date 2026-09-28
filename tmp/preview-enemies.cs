var origin=UnityEngine.Object.FindFirstObjectByType<ArmySurvivor.Army.RunController>().Commander.position+UnityEngine.Vector3.forward*8;
int i=0;foreach(var name in new[]{"Ninja","Orc_Skull","Yeti"}){
 var d=UnityEditor.AssetDatabase.LoadAssetAtPath<ArmySurvivor.Army.EnemyDefinition>("Assets/CombatData/"+name+".asset");
 var g=UnityEngine.Object.Instantiate(d.prefab,origin+UnityEngine.Vector3.right*(i++-1)*4,UnityEngine.Quaternion.Euler(0,180,0));
 var a=g.GetComponentInChildren<UnityEngine.Animator>();a.cullingMode=UnityEngine.AnimatorCullingMode.AlwaysAnimate;a.Play("Base Layer.Attack",0,0.4f);a.Update(0);
}
var cameraObject=new UnityEngine.GameObject("Monster Preview Camera");var cam=cameraObject.AddComponent<UnityEngine.Camera>();
cam.transform.position=origin+new UnityEngine.Vector3(0,5,-12);cam.transform.LookAt(origin+UnityEngine.Vector3.up);cam.orthographic=true;cam.orthographicSize=3.7f;cam.farClipPlane=100;
var rt=UnityEngine.RenderTexture.GetTemporary(1200,650,24);cam.targetTexture=rt;var old=UnityEngine.RenderTexture.active;cam.Render();UnityEngine.RenderTexture.active=rt;
var tex=new UnityEngine.Texture2D(1200,650,UnityEngine.TextureFormat.RGB24,false);tex.ReadPixels(new UnityEngine.Rect(0,0,1200,650),0,0);tex.Apply();System.IO.File.WriteAllBytes("C:/Users/user/GitHub/3D-Single-Project/tmp/new-enemies.png",tex.EncodeToPNG());
cam.targetTexture=null;UnityEngine.RenderTexture.active=old;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(cameraObject);return "Preview captured: Ninja, Skull Orc, Yeti";
