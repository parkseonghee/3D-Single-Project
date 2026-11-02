using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Army.Editor
{
    public static class ExperienceBottleOptimization
    {
        [MenuItem("Army Survivor/Optimize Experience Bottle")]
        public static void Optimize()
        {
            const string path = "Assets/Prefabs/Combat/SnakeExperienceBottle.prefab";
            const string meshPath = "Assets/EquipmentData/ExperienceBottle-LowPoly.asset";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var filter = prefab.GetComponentInChildren<MeshFilter>();
                if (AssetDatabase.GetAssetPath(filter.sharedMesh) == meshPath) { Debug.Log("Experience bottle already optimized."); return; }
                var source = filter.sharedMesh;
                Mesh reduced = null;
                foreach (int resolution in new[]{32,24,16,12,8})
                {
                    if (reduced != null) Object.DestroyImmediate(reduced);
                    reduced = Cluster(source, resolution);
                    if (reduced.vertexCount <= 16000) break;
                }
                reduced.name = "ExperienceBottle-LowPoly";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing != null) { EditorUtility.CopySerialized(reduced, existing); Object.DestroyImmediate(reduced); reduced = existing; }
                else AssetDatabase.CreateAsset(reduced, meshPath);
                filter.sharedMesh = reduced;
                foreach(var renderer in prefab.GetComponentsInChildren<Renderer>())
                { renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false; }
                PrefabUtility.SaveAsPrefabAsset(prefab,path);
                AssetDatabase.SaveAssets();
                Debug.Log($"BOTTLE_OPTIMIZED: {source.vertexCount} -> {reduced.vertexCount} vertices; {reduced.triangles.Length/3} triangles. Source FBX unchanged.");
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        // Offline vertex clustering with UV-cell separation. Retains source material/UV islands.
        static Mesh Cluster(Mesh source, int resolution)
        {
            var vertices = source.vertices; var uvs = source.uv; var normals = source.normals;
            var bounds = source.bounds;
            float step = Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))/resolution;
            var index = new Dictionary<(int,int,int,int,int),int>();
            var positions = new List<Vector3>(); var tex = new List<Vector2>(); var norms = new List<Vector3>(); var counts = new List<int>();
            var remap = new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=(vertices[i]-bounds.min)/Mathf.Max(step,.000001f); var uv=uvs.Length>i?uvs[i]:Vector2.zero;
                var key=(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y),Mathf.RoundToInt(p.z),Mathf.FloorToInt(uv.x*resolution/2),Mathf.FloorToInt(uv.y*resolution/2));
                if(!index.TryGetValue(key,out int n))
                { n=positions.Count;index[key]=n;positions.Add(Vector3.zero);tex.Add(Vector2.zero);norms.Add(Vector3.zero);counts.Add(0); }
                positions[n]+=vertices[i];tex[n]+=uv;norms[n]+=normals.Length>i?normals[i]:Vector3.up;counts[n]++;remap[i]=n;
            }
            for(int i=0;i<positions.Count;i++){positions[i]/=counts[i];tex[i]/=counts[i];norms[i]=norms[i].normalized;}
            var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);mesh.SetUVs(0,tex);mesh.SetNormals(norms);mesh.subMeshCount=source.subMeshCount;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var triangles=source.GetTriangles(sub);var output=new List<int>();var unique=new HashSet<(int,int,int)>();
                for(int i=0;i<triangles.Length;i+=3)
                {
                    int a=remap[triangles[i]],b=remap[triangles[i+1]],c=remap[triangles[i+2]];
                    if(a==b||b==c||a==c)continue;
                    var key=a<b&&a<c?(a,b,c):b<c?(b,c,a):(c,a,b);
                    if(!unique.Add(key))continue;
                    output.Add(a);output.Add(b);output.Add(c);
                }
                mesh.SetTriangles(output,sub);
            }
            mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.Optimize();return mesh;
        }
    }
}
