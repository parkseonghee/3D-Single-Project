using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Equipment.Editor
{
    public static class InfernoSealTicketSetup
    {
        const string Source = "Assets/Meshy_AI_Inferno_Seal_1026121053_texture_fbx";
        const string Folder = "Assets/Art/Meshy/Props/InfernoSeal";
        const string ModelName = "Meshy_AI_Inferno_Seal_1026121053_texture.fbx";
        const string TicketPath = "Assets/EquipmentData/Ticket.prefab";

        [InitializeOnLoadMethod]
        static void QueueSetup() => EditorApplication.delayCall += Apply;

        [MenuItem("Army Survivor/Equipment/Apply Inferno Seal Ticket")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            var catalog = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
            var ticket = AssetDatabase.LoadAssetAtPath<GameObject>(TicketPath);
            if (catalog == null || ticket == null || ticket.transform.Find("Inferno Seal Visual") != null) return;
            if (AssetDatabase.IsValidFolder(Source) && !AssetDatabase.IsValidFolder(Folder))
            {
                string error = AssetDatabase.MoveAsset(Source, Folder);
                if (!string.IsNullOrEmpty(error)) { Debug.LogError(error); return; }
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/" + ModelName);
            if (model == null) return;
            var root = PrefabUtility.LoadPrefabContents(TicketPath);
            try
            {
                // Keep the existing prefab/root IDs so catalog and any scene references survive.
                root.transform.localScale = Vector3.one;
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                visual.name = "Inferno Seal Visual";
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new System.InvalidOperationException("Inferno Seal has no renderer.");
                var bounds = BoundsOf(renderers);
                float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                visual.transform.localScale *= 0.9f / Mathf.Max(0.001f, size);
                bounds = BoundsOf(renderers);
                visual.transform.position += root.transform.position - bounds.center;
                foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (var body in visual.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                var oldRenderer = root.GetComponent<MeshRenderer>();
                if (oldRenderer != null) Object.DestroyImmediate(oldRenderer);
                var oldMesh = root.GetComponent<MeshFilter>();
                if (oldMesh != null) Object.DestroyImmediate(oldMesh);
                var saved = PrefabUtility.SaveAsPrefabAsset(root, TicketPath);
                if (saved == null) throw new System.InvalidOperationException("Could not save Inferno Seal ticket.");
                catalog.ticketPrefab = saved;
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                Debug.Log("Inferno Seal ticket installed: centered model, maximum dimension 0.9m; catalog linked.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
