using System.IO;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Equipment.Editor
{
    public static class TicketIconSetup
    {
        const string IconPath = "Assets/EquipmentData/InfernoSealIcon.png";
        [InitializeOnLoadMethod]
        static void Queue() => EditorApplication.delayCall += EnsureIcon;
        static void EnsureIcon()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
            if (!EditorApplication.isPlayingOrWillChangePlaymode && catalog != null && catalog.ticketIcon == null)
                Bake();
        }
        [MenuItem("Army Survivor/Equipment/Bake Ticket Icon")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var catalog = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>("Assets/Resources/EquipmentCatalog.asset");
            if (catalog == null || catalog.ticketPrefab == null) return;
            int layer = LayerMask.NameToLayer("ItemInspect");
            if (layer < 0) return;
            var stage = new GameObject("Ticket Icon Capture") { hideFlags = HideFlags.HideAndDontSave };
            stage.transform.position = new Vector3(0, -20000, 0);
            RenderTexture target = null;
            Texture2D pixels = null;
            var previous = RenderTexture.active;
            try
            {
                var model = Object.Instantiate(catalog.ticketPrefab, stage.transform);
                model.transform.localPosition = Vector3.zero;
                foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                foreach (var b in model.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var cameraObject = new GameObject("Icon Camera", typeof(Camera));
                cameraObject.transform.SetParent(stage.transform, false);
                var camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << layer;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.57f;
                camera.nearClipPlane = 0.01f; camera.farClipPlane = 20;
                camera.allowHDR = false; camera.allowMSAA = false;
                camera.transform.position = bounds.center + Vector3.back * 3;
                camera.transform.LookAt(bounds.center);
                AddLight(stage.transform, layer, new Vector3(-1, 1, -2), 8);
                AddLight(stage.transform, layer, new Vector3(1, 0, -1), 4);
                target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target; camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); pixels.Apply();
                File.WriteAllBytes(IconPath, pixels.EncodeToPNG());
                camera.targetTexture = null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                if (pixels != null) Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(stage);
            }
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            catalog.ticketIcon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Debug.Log("Inferno Seal UI icon baked and linked: 256px transparent Sprite.");
        }
        static void AddLight(Transform parent, int layer, Vector3 position, float intensity)
        {
            var go = new GameObject("Icon Light", typeof(Light)); go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var light = go.GetComponent<Light>(); light.type = LightType.Point;
            light.range = 10; light.intensity = intensity; light.cullingMask = 1 << layer;
        }
    }
}
