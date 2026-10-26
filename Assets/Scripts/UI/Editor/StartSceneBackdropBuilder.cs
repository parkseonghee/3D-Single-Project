#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.UI.Editor
{
    public static class StartSceneBackdropBuilder
    {
        private const string Source = "Assets/ToonyTinyPeople/TT_RTS/TT_RTS_Standard/";
        private const string Output = "Assets/Art/StartScene";
        private static Transform root;

        [MenuItem("Army Survivor/Start Scene/Build Living Castle Background")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/StartScene.unity")
                throw new System.InvalidOperationException("Open StartScene in Edit Mode first.");
            EnsureFolder(Output);
            var old = GameObject.Find("Living Castle Background");
            if (old != null) Object.DestroyImmediate(old);
            root = new GameObject("Living Castle Background").transform;
            var canvas = Object.FindFirstObjectByType<Canvas>();
            foreach (var image in canvas.GetComponentsInChildren<Image>(true))
                if (image.transform.parent == canvas.transform && image.name == "Image")
                    Object.DestroyImmediate(image.gameObject);

            var grass = Material("Meadow", new Color(0.30f, 0.40f, 0.20f));
            var stone = Material("Warm Limestone", new Color(0.58f, 0.54f, 0.43f));
            var road = Material("Sandstone Road", new Color(0.66f, 0.57f, 0.40f));
            Block("Landscape", new Vector3(0,-0.4f,0), new Vector3(500,0.7f,500), grass);
            Block("Castle Terrace",new Vector3(0,-0.08f,17),new Vector3(35,0.2f,22),stone);
            Block("Village Square",new Vector3(0,0.015f,-3),new Vector3(41,0.08f,9),road);
            Block("Castle Approach",new Vector3(0,0.02f,3),new Vector3(5,0.08f,25),road);
            foreach (float x in new[]{-14f,14f})
                Block("Village Lane",new Vector3(x,0.02f,-8),new Vector3(4,0.08f,27),road);

            Building("Castle",new Vector3(0,0,21),1.75f,180);
            Building("Wall_A_gate",new Vector3(0,0,9),1f,90);
            foreach(float x in new[]{-6f,6f,-12f,12f})
                Building("Wall_A_wall",new Vector3(x,0,9),1f,90);
            foreach(float x in new[]{-16f,16f})
            {
                Building("Tower_A",new Vector3(x,0,9),1.25f,180);
                Building("Tower_A",new Vector3(x,0,25),1.25f,180);
                foreach(float z in new[]{15f,21f}) Building("Wall_A_wall",new Vector3(x,0,z),1f,0);
            }
            Building("House",new Vector3(-22,0,-5),1.25f,90);
            Building("House",new Vector3(-21,0,-16),1.15f,80);
            Building("House",new Vector3(22,0,-6),1.25f,270);
            Building("House",new Vector3(22,0,-17),1.15f,280);
            Building("Market",new Vector3(-8,0,-13),1f,0);
            Building("Market",new Vector3(8,0,-13),1f,180);
            Building("House",new Vector3(-25,0,7),1.1f,140);
            Building("House",new Vector3(25,0,7),1.1f,220);

            // A few grass islands break up the plaza without hiding the castle.
            foreach(float x in new[]{-9f,9f})
            {
                Block("Garden Border", new Vector3(x,0.12f,3),new Vector3(7,0.3f,4),stone);
                Block("Garden", new Vector3(x,0.29f,3),new Vector3(6.6f,0.08f,3.6f),grass);
            }
            var controller = CitizenController();
            var routes = new[]
            {
                Route("West promenade",new[]{new Vector3(-18,0.1f,-4),new Vector3(-3,0.1f,-4),new Vector3(-3,0.1f,-1),new Vector3(-18,0.1f,-1)}),
                Route("East promenade",new[]{new Vector3(3,0.1f,-4),new Vector3(18,0.1f,-4),new Vector3(18,0.1f,-1),new Vector3(3,0.1f,-1)}),
                Route("West market lane",new[]{new Vector3(-14,0.1f,-18),new Vector3(-14,0.1f,-7),new Vector3(-12.5f,0.1f,-7),new Vector3(-12.5f,0.1f,-18)}),
                Route("East market lane",new[]{new Vector3(14,0.1f,-18),new Vector3(14,0.1f,-7),new Vector3(12.5f,0.1f,-7),new Vector3(12.5f,0.1f,-18)}),
                Route("Castle visitors",new[]{new Vector3(-1.2f,0.1f,-2),new Vector3(-1.2f,0.1f,6),new Vector3(1.2f,0.1f,6),new Vector3(1.2f,0.1f,-2)})
            };
            for(int i=0;i<15;i++)
            {
                var points=routes[i%routes.Length];
                int start=(i/routes.Length)%points.Length;
                var citizen=Prefab(Source+"prefabs/TT_Peasant.prefab", "Citizen "+(i+1), points[start].position,1.25f,0);
                var animator=citizen.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController=controller;
                animator.applyRootMotion=false;
                citizen.AddComponent<MenuCitizen>().Configure(points,animator,1.05f+(i%4)*0.14f,start);
                foreach(var collider in citizen.GetComponentsInChildren<Collider>()) collider.enabled=false;
            }
            var camera=Camera.main;
            camera.orthographic=false;
            camera.fieldOfView=44;
            camera.nearClipPlane=0.3f;
            camera.farClipPlane=250;
            camera.transform.position=new Vector3(24,30,-48);
            camera.transform.LookAt(new Vector3(0,3,5));
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(0.66f,0.77f,0.83f);
            var sun=new GameObject("Afternoon Sun").AddComponent<Light>();
            sun.transform.SetParent(root);
            sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(46,-32,0);
            sun.color=new Color(1,0.88f,0.70f);
            sun.intensity=1.5f;
            sun.shadows=LightShadows.Soft;
            RenderSettings.sun=sun;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(0.60f,0.69f,0.78f);
            RenderSettings.ambientEquatorColor=new Color(0.45f,0.48f,0.43f);
            RenderSettings.ambientGroundColor=new Color(0.25f,0.28f,0.22f);
            RenderSettings.fog=true;
            RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=camera.backgroundColor;
            RenderSettings.fogStartDistance=90;
            RenderSettings.fogEndDistance=180;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            StartSceneSceneryBuilder.Build();
            StartSceneMenuLayout.Apply();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path)) return;
            var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));
        }
        private static Material Material(string name,Color color)
        {
            string path=Output+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) {material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.color=color;
            material.SetFloat("_Smoothness",0.1f);
            EditorUtility.SetDirty(material);
            return material;
        }
        private static void Block(string name,Vector3 position,Vector3 size,Material material)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name=name;g.transform.SetParent(root);g.transform.position=position;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        private static GameObject Prefab(string path,string name,Vector3 position,float scale,float yaw)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null) throw new System.IO.FileNotFoundException(path);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);
            g.name=name;g.transform.position=position;g.transform.rotation=Quaternion.Euler(0,yaw,0)*prefab.transform.localRotation;g.transform.localScale=prefab.transform.localScale*scale;
            return g;
        }
        private static void Building(string name,Vector3 position,float scale,float yaw)
        {
            var g=Prefab(Source+"models/buildings/"+name+".FBX",name,position,scale,yaw);
            // Imported building origins differ. Ground each model using its visible base.
            var renderers=g.GetComponentsInChildren<Renderer>();
            float bottom=renderers.Min(r=>r.bounds.min.y);
            g.transform.position+=Vector3.up*(0.08f-bottom);
        }
        private static Transform[] Route(string name,Vector3[] positions)
        {
            var holder=new GameObject(name).transform;holder.SetParent(root);
            return positions.Select((p,i)=>{var t=new GameObject("Waypoint "+i).transform;t.SetParent(holder);t.position=p;return t;}).ToArray();
        }
        private static AnimatorController CitizenController()
        {
            string path=Output+"/Citizen.controller";
            var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(existing!=null) return existing;
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Walking",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;
            var idle=machine.AddState("Rest"); var walk=machine.AddState("Walk");
            string folder=Source+"animation/animation_infantry/Infantry/";
            idle.motion=AssetDatabase.LoadAllAssetsAtPath(folder+"infantry_01_idle.FBX").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            walk.motion=AssetDatabase.LoadAllAssetsAtPath(folder+"infantry_02_walk.FBX").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            machine.defaultState=idle;
            var a=idle.AddTransition(walk);a.hasExitTime=false;a.duration=0.18f;a.AddCondition(AnimatorConditionMode.If,0,"Walking");
            var b=walk.AddTransition(idle);b.hasExitTime=false;b.duration=0.18f;b.AddCondition(AnimatorConditionMode.IfNot,0,"Walking");
            return controller;
        }
    }
}
#endif


