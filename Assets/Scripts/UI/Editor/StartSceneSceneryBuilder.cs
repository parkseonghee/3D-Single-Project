#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArmySurvivor.UI.Editor
{
    public static class StartSceneSceneryBuilder
    {
        private const string Models = "Assets/kenney_hexagon-kit/Models/FBX format/";
        private static Transform root;
        private static System.Random random;

        [MenuItem("Army Survivor/Start Scene/Decorate Castle Surroundings")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != "Assets/Scenes/StartScene.unity")
                throw new System.InvalidOperationException("Open StartScene in Edit Mode first.");
            var backdrop = GameObject.Find("Living Castle Background");
            if (backdrop == null) throw new System.InvalidOperationException("Build the castle background first.");
            var old = backdrop.transform.Find("Surrounding Landscape");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            root = new GameObject("Surrounding Landscape").transform;
            root.SetParent(backdrop.transform);
            random = new System.Random(241024);

            // Overlapping silhouettes hide the flat horizon; the castle remains taller in frame.
            for (int i = 0; i < 13; i++)
            {
                float x = -85 + i * 14;
                Place("stone-mountain", "Distant Mountain", new Vector3(x, -4, Range(66,83)),
                    new Vector3(Range(23,32),Range(11,18),Range(25,34)),Range(0,360));
            }
            for (int i = 0; i < 8; i++)
                Place("grass-hill", "Woodland Hill", new Vector3(-65+i*19,-3.5f,Range(43,55)),
                    new Vector3(24,Range(7,10),22),Range(0,360));

            // Irregular clusters, rather than uniform rows, surround the settlement.
            for (int i=0;i<105;i++)
            {
                float x=Range(-68,68), z=Range(32,62);
                Tree(x,z,Range(12,23));
            }
            for (int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<34;i++)
                    Tree(side*Range(30,52),Range(-28,33),Range(11,21));
                for(int i=0;i<10;i++)
                    Place("stone-rocks","Weathered Rock",new Vector3(side*Range(28,42),-0.9f,Range(-24,30)),
                        Vector3.one*Range(3,6),Range(0,360));
            }
            // Small foreground groups frame the houses, leaving the roads and citizens clear.
            foreach(var p in new[]{new Vector3(-23,0,-25),new Vector3(23,0,-26),new Vector3(-27,0,-17),new Vector3(27,0,-20)})
            {
                Tree(p.x,p.z,15);
                Tree(p.x+2.6f,p.z-1.5f,10);
                Place("stone-rocks","Roadside Stones",p+new Vector3(-2,-0.55f,1),Vector3.one*2.4f,35);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static float Range(float min,float max) => min+(float)random.NextDouble()*(max-min);
        private static void Tree(float x,float z,float scale)
        {
            Place("unit-tree","Pine",new Vector3(x,-0.04f,z),new Vector3(scale,scale*Range(0.85f,1.2f),scale),Range(0,360));
        }
        private static void Place(string model,string name,Vector3 position,Vector3 scale,float yaw)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Models+model+".fbx");
            if(asset==null) throw new System.IO.FileNotFoundException(Models+model+".fbx");
            var g=(GameObject)PrefabUtility.InstantiatePrefab(asset,root);
            g.name=name;
            g.transform.position=position;
            g.transform.rotation=Quaternion.Euler(0,yaw,0)*asset.transform.localRotation;
            g.transform.localScale=Vector3.Scale(asset.transform.localScale,scale);
                        foreach(var renderer in g.GetComponentsInChildren<Renderer>())
            {
                string category=model=="unit-tree"?"Forest":model=="grass-hill"?"Hills":"Mountain Stone";
                string materialPath="Assets/Art/StartScene/"+category+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if(material==null)
                {
                    material=new Material(renderer.sharedMaterial);
                    material.name=category;
                    Color tint=category=="Forest"?new Color(0.38f,0.57f,0.26f):category=="Hills"?new Color(0.53f,0.62f,0.33f):new Color(0.69f,0.68f,0.59f);
                    material.color=tint;
                    if(material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",tint);
                    AssetDatabase.CreateAsset(material,materialPath);
                }
                renderer.sharedMaterial=material;
            }
            foreach(var c in g.GetComponentsInChildren<Collider>()) c.enabled=false;
        }
    }
}
#endif

