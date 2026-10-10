using System.Collections.Generic;
using UnityEngine;

namespace ArmySurvivor.Army
{
    // One render per definition per HUD lifetime. No live RenderTexture cameras.
    public sealed class UnitPortraitCache : System.IDisposable
    {
        public sealed class Portrait
        {
            public Texture2D normal;
            public Texture2D dead;
        }

        private readonly Dictionary<UnitDefinition, Portrait> cache = new Dictionary<UnitDefinition, Portrait>();
        private readonly int layer;
        public int CaptureCount { get; private set; }

        public UnitPortraitCache(int layer) { this.layer = layer; }

        public Portrait Get(UnitDefinition definition)
        {
            if (cache.TryGetValue(definition, out var result)) return result;
            if (definition.prefab == null) return null;
            var stage = new GameObject("Portrait Capture Stage");
            stage.SetActive(false);
            stage.transform.position = new Vector3(0, -10000, 0);
            RenderTexture target = null;
            var previous = RenderTexture.active;
            try
            {
                var model = Object.Instantiate(definition.prefab, stage.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(definition.modelRotation);
                model.transform.localScale = definition.modelScale;
                foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
                foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
                foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) body.isKinematic = true;
                foreach (var light in model.GetComponentsInChildren<Light>(true)) light.enabled = false;
                foreach (var camera in model.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                stage.SetActive(true);
                foreach (var particles in model.GetComponentsInChildren<ParticleSystem>(true))
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                foreach (var animator in model.GetComponentsInChildren<Animator>())
                {
                    animator.Rebind();
                    animator.Update(0);
                    animator.enabled = false;
                }
                var renderers = model.GetComponentsInChildren<Renderer>();
                var bounds = new Bounds(model.transform.position, Vector3.zero);
                bool hasBounds = false;
                foreach (var renderer in renderers)
                {
                    if (!renderer.enabled || renderer is ParticleSystemRenderer) continue;
                    if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                // Prefer the rig head, so a mounted unit frames its rider instead of its horse.
                Transform head = null;
                foreach (var t in model.GetComponentsInChildren<Transform>())
                    if (t.name.EndsWith(" Head", System.StringComparison.OrdinalIgnoreCase) || t.name == "Head")
                        if (head == null || t.position.y > head.position.y) head = t;
                float height = Mathf.Max(0.3f, bounds.size.y);
                Vector3 focus = head != null ? head.position : bounds.center + Vector3.up * height * 0.25f;
                float halfHeight = head != null
                    ? Mathf.Max(0.3f, (bounds.max.y - head.position.y) * 1.5f, height * 0.19f)
                    : height * 0.33f;
                focus.y += halfHeight * 0.02f;
                var cameraObject = new GameObject("Portrait Camera", typeof(Camera));
                cameraObject.transform.SetParent(stage.transform, false);
                var capture = cameraObject.GetComponent<Camera>();
                capture.enabled = false;
                capture.clearFlags = CameraClearFlags.SolidColor;
                capture.backgroundColor = Color.clear;
                capture.cullingMask = 1 << layer;
                capture.orthographic = true;
                capture.orthographicSize = halfHeight;
                capture.nearClipPlane = 0.01f;
                capture.farClipPlane = height * 10 + 20;
                capture.allowHDR = false;
                capture.allowMSAA = false;
                capture.transform.position = focus + new Vector3(0, 0.02f, 1) * (height * 3 + 3);
                capture.transform.LookAt(focus);
                var lampObject = new GameObject("Portrait Light", typeof(Light));
                lampObject.transform.SetParent(stage.transform, false);
                lampObject.transform.rotation = Quaternion.Euler(35, 160, 0);
                var lamp = lampObject.GetComponent<Light>();
                lamp.type = LightType.Directional;
                lamp.intensity = 1.8f;
                lamp.cullingMask = 1 << layer;
                lamp.shadows = LightShadows.None;
                target = RenderTexture.GetTemporary(256, 256, 24, RenderTextureFormat.ARGB32);
                capture.targetTexture = target;
                capture.Render();
                CaptureCount++;
                RenderTexture.active = target;
                var normal = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                normal.name = definition.name + " Portrait";
                normal.ReadPixels(new Rect(0, 0, 256, 256), 0, 0);
                normal.Apply();
                var pixels = normal.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    byte grey = (byte)(p.r * 0.299f + p.g * 0.587f + p.b * 0.114f);
                    pixels[i] = new Color32(grey, grey, grey, p.a);
                }
                var dead = new Texture2D(256, 256, TextureFormat.RGBA32, false);
                dead.name = definition.name + " Portrait Dead";
                dead.SetPixels32(pixels);
                dead.Apply();
                result = new Portrait { normal = normal, dead = dead };
                cache.Add(definition, result);
                capture.targetTexture = null;
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                stage.SetActive(false);
                Object.Destroy(stage);
            }
        }

        public void Dispose()
        {
            foreach (var portrait in cache.Values)
            {
                Object.Destroy(portrait.normal);
                Object.Destroy(portrait.dead);
            }
            cache.Clear();
        }
    }
}
