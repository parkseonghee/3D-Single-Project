#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using ArmySurvivor.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArmySurvivor.UI.Editor
{
    public static class VillageBackdropSetup
    {
        [MenuItem("Army Survivor/Village/Match Start Scene Village")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying || scene.path!="Assets/Scenes/VillageScene.unity") throw new System.InvalidOperationException("Open VillageScene in Edit Mode first.");
            var source=EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity",OpenSceneMode.Additive);
            GameObject copy;
            try
            {
                var original=source.GetRootGameObjects().First(g=>g.name=="Living Castle Background");
                copy=Object.Instantiate(original);
                SceneManager.MoveGameObjectToScene(copy,scene);
                copy.name="Village Castle Landscape";
            }
            finally {EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);}
            var prior=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Village Castle Landscape"&&g!=copy);
            if(prior!=null)Object.DestroyImmediate(prior);
            // Additive battle loading hides only the registered village roots.
            // Include the scenery, citizens and sun, including after rebuilding the village.
            var travel=new SerializedObject(Object.FindFirstObjectByType<ArmySurvivor.Army.SceneTravel>());
            var villageRoots=travel.FindProperty("villageRoots");
            for(int i=villageRoots.arraySize-1;i>=0;i--)
                if(villageRoots.GetArrayElementAtIndex(i).objectReferenceValue==null)
                    villageRoots.DeleteArrayElementAtIndex(i);
            villageRoots.InsertArrayElementAtIndex(villageRoots.arraySize);
            villageRoots.GetArrayElementAtIndex(villageRoots.arraySize-1).objectReferenceValue=copy;
            travel.ApplyModifiedPropertiesWithoutUndo();
            var map=GameObject.Find("Village Hex Map");
            foreach(Transform child in map.transform) child.gameObject.SetActive(false);
            var ground=map.GetComponent<BoxCollider>();
            var placement=Object.FindFirstObjectByType<BuildingPlacementController>();
            var castle=GameObject.Find("Starting Castle");
            var castleVisual=copy.transform.Find("Castle");
            // Retain the gameplay root and its starting-building reference.
            for(int i=castle.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(castle.transform.GetChild(i).gameObject);
            castle.transform.position=new Vector3(0,0,21);
            castleVisual.SetParent(castle.transform,true);
            var castleBounds=castleVisual.GetComponentInChildren<Renderer>().bounds;
            var castleCollider=castle.GetComponent<BoxCollider>();
            castleCollider.center=castle.transform.InverseTransformPoint(castleBounds.center);castleCollider.size=castleBounds.size;
            GameObject.Find("Starting Farm").transform.position=new Vector3(-7,0,-27);

            var exclusions=new List<Bounds>();
            foreach(Transform t in copy.transform)
            {
                if(t.GetComponent<MenuCitizen>()!=null || t.GetComponent<Light>()!=null)continue;
                if(t.name=="Landscape" || t.name=="Castle Terrace")continue;
                var renderers=t.GetComponentsInChildren<Renderer>();
                if(t.name=="Surrounding Landscape")
                {
                    foreach(Transform item in t)
                    {
                        var rs=item.GetComponentsInChildren<Renderer>();
                        if(rs.Length==0)continue;
                        var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
                        exclusions.Add(bounds);
                    }
                }
                else if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    exclusions.Add(bounds);
                }
            }
            var so=new SerializedObject(placement);
            var prop=so.FindProperty("sceneryExclusions");prop.arraySize=exclusions.Count;
            for(int i=0;i<exclusions.Count;i++)prop.GetArrayElementAtIndex(i).boundsValue=exclusions[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            // Existing construction and input components keep their serialized connections.
            var camera=GameObject.Find("Village Camera").GetComponent<Camera>();
            camera.orthographic=false;camera.fieldOfView=48;camera.farClipPlane=250;
            camera.transform.position=new Vector3(24,34,-55);camera.transform.LookAt(new Vector3(0,2,3));
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.66f,0.77f,0.83f);
            var drag=new SerializedObject(camera.GetComponent<VillageCameraDrag>());
            drag.FindProperty("viewLimit").vector2Value=new Vector2(26,22);drag.ApplyModifiedPropertiesWithoutUndo();
            var zoom=new SerializedObject(camera.GetComponent<ArmySurvivor.CameraWheelZoom>());
            zoom.FindProperty("fieldOfViewLimits").vector2Value=new Vector2(22,52);zoom.ApplyModifiedPropertiesWithoutUndo();
            GameObject.Find("Village Light").GetComponent<Light>().enabled=false;
            RenderSettings.sun=copy.GetComponentInChildren<Light>();
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(0.60f,0.69f,0.78f);RenderSettings.ambientEquatorColor=new Color(0.45f,0.48f,0.43f);RenderSettings.ambientGroundColor=new Color(0.25f,0.28f,0.22f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=camera.backgroundColor;RenderSettings.fogStartDistance=90;RenderSettings.fogEndDistance=180;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            VillageFrontPlotSetup.Apply();
        }
    }
}
#endif
