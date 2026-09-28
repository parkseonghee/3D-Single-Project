var map=UnityEngine.GameObject.Find("Village Hex Map");
var area=map.GetComponent<UnityEngine.BoxCollider>().bounds;
var parent=map.transform.Find("Land Tiles");
var targets=new System.Collections.Generic.List<UnityEngine.Transform>();
foreach(UnityEngine.Transform t in parent){
 var r=t.GetComponentInChildren<UnityEngine.Renderer>(); if(r==null)continue;
 var b=r.bounds;
 if(b.max.x>area.min.x && b.min.x<area.max.x && b.max.z>area.min.z && b.min.z<area.max.z)targets.Add(t);
}
var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/kenney_hexagon-kit/Models/FBX format/sand.fbx");
UnityEditor.Undo.IncrementCurrentGroup(); int group=UnityEditor.Undo.GetCurrentGroup();
UnityEditor.Undo.SetCurrentGroupName("Change buildable village ground to sand");
foreach(var old in targets){
 var tile=(UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,parent);
 UnityEditor.Undo.RegisterCreatedObjectUndo(tile,"Sand tile");
 tile.transform.localPosition=old.localPosition; tile.transform.localRotation=old.localRotation; tile.transform.localScale=old.localScale;
 tile.name=old.name.Replace("grass","sand"); tile.transform.SetSiblingIndex(old.GetSiblingIndex());
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(tile.transform);
 UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(tile);
 UnityEditor.Undo.DestroyObjectImmediate(old.gameObject);
}
UnityEditor.Undo.CollapseUndoOperations(group);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(map.scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(map.scene);
var cam=UnityEngine.GameObject.Find("Village Camera").GetComponent<UnityEngine.Camera>();
var rt=UnityEngine.RenderTexture.GetTemporary(1280,720,24);
var previous=cam.targetTexture; var active=UnityEngine.RenderTexture.active;
cam.targetTexture=rt; cam.Render(); UnityEngine.RenderTexture.active=rt;
var tex=new UnityEngine.Texture2D(1280,720,UnityEngine.TextureFormat.RGB24,false);
tex.ReadPixels(new UnityEngine.Rect(0,0,1280,720),0,0); tex.Apply();
System.IO.File.WriteAllBytes("C:/Users/user/GitHub/3D-Single-Project/tmp/village-sand.png",tex.EncodeToPNG());
cam.targetTexture=previous;UnityEngine.RenderTexture.active=active;UnityEngine.RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);
return "Changed and saved "+targets.Count+" sand tiles; build area="+area;
