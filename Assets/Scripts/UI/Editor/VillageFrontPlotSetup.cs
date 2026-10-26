#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ArmySurvivor.Building;

namespace ArmySurvivor.UI.Editor
{
    public static class VillageFrontPlotSetup
    {
        [MenuItem("Army Survivor/Village/Configure Front Construction Plot")]
        public static void Apply()
        {
            var scene=EditorSceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/Scenes/VillageScene.unity")
                throw new System.InvalidOperationException("Open VillageScene in Edit Mode first.");
            var landscape=GameObject.Find("Village Castle Landscape").transform;
            var plot=new Bounds(new Vector3(0,0,-42),new Vector3(48,1,36));
            var ground=GameObject.Find("Village Hex Map").GetComponent<BoxCollider>();
            ground.center=new Vector3(0,-0.25f,-42);ground.size=new Vector3(48,0.5f,36);
            foreach(Transform item in landscape.Find("Surrounding Landscape"))
            {
                var p=item.position;
                if(p.z < -22 && Mathf.Abs(p.x)<28)
                {p.x=p.x<0?-31:31;item.position=p;}
            }
            var existing=landscape.Find("Front Construction Plot");
            if(existing==null)
            {
                var tile=GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name="Front Construction Plot";tile.transform.SetParent(landscape,false);
                Object.DestroyImmediate(tile.GetComponent<Collider>());
                existing=tile.transform;
                tile.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/StartScene/Sandstone Road.mat");
            }
            existing.position=new Vector3(0,-0.055f,-42);existing.localScale=new Vector3(48,0.1f,36);
            var so=new SerializedObject(Object.FindFirstObjectByType<BuildingPlacementController>());
            var list=so.FindProperty("sceneryExclusions");
            for(int i=list.arraySize-1;i>=0;i--)
            {
                var b=list.GetArrayElementAtIndex(i).boundsValue;
                if(b.min.x<plot.max.x&&b.max.x>plot.min.x&&b.min.z<plot.max.z&&b.max.z>plot.min.z)
                    list.DeleteArrayElementAtIndex(i);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var camera=GameObject.Find("Village Camera").GetComponent<Camera>();
            camera.transform.position=new Vector3(24,44,-72);camera.transform.LookAt(new Vector3(0,0,-12));
            var drag=new SerializedObject(camera.GetComponent<VillageCameraDrag>());
            drag.FindProperty("viewLimit").vector2Value=new Vector2(26,60);drag.ApplyModifiedPropertiesWithoutUndo();
            VillageStartingBuildingsSetup.Apply();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
