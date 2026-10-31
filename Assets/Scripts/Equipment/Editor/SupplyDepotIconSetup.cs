using System.IO;
using ArmySurvivor.Building;
using UnityEditor;
using UnityEngine;

namespace ArmySurvivor.Equipment.Editor
{
    public static class SupplyDepotIconSetup
    {
        private const string BuildingPath = "Assets/EquipmentData/SupplyDepot.asset";
        private const string IconPath = "Assets/EquipmentData/SupplyDepotIcon.png";

        [MenuItem("Army Survivor/Equipment/Bake Supply Depot Icon")]
        public static void Bake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            BuildingDefinition building = AssetDatabase.LoadAssetAtPath<BuildingDefinition>(BuildingPath);
            if (building == null || building.prefab == null) return;

            int layer = LayerMask.NameToLayer("ItemInspect");
            if (layer < 0) { Debug.LogError("ItemInspect layer is required for icon capture."); return; }
            GameObject stage = new GameObject("Supply Depot Icon Capture") { hideFlags = HideFlags.HideAndDontSave };
            stage.transform.position = new Vector3(0, -20000, 0);
            RenderTexture target = null;
            Texture2D pixels = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                GameObject model = Object.Instantiate(building.prefab, stage.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(building.modelRotation);
                model.transform.localScale = building.modelScale;
                foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
                foreach (MonoBehaviour behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                GameObject cameraObject = new GameObject("Icon Camera", typeof(Camera));
                cameraObject.transform.SetParent(stage.transform, false);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << layer;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.82f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.transform.position = bounds.center + new Vector3(1, 0.85f, -1).normalized * 20f;
                camera.transform.LookAt(bounds.center);

                GameObject lightObject = new GameObject("Icon Light", typeof(Light));
                lightObject.transform.SetParent(stage.transform, false);
                lightObject.transform.position = bounds.center + new Vector3(-5, 8, -6);
                Light light = lightObject.GetComponent<Light>();
                light.type = LightType.Point;
                light.range = 40;
                light.intensity = 8;
                light.cullingMask = 1 << layer;

                target = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                pixels.Apply();
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
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            building.icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            EditorUtility.SetDirty(building);
            AssetDatabase.SaveAssets();
            Debug.Log("Supply Depot icon baked and linked.");
        }
    }
}
