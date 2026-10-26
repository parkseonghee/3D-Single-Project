using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArmySurvivor.Equipment
{
    // Render only on selection/drag/zoom. Dedicated layer also excluded from gameplay cameras.
    public sealed class ItemInspectView : MonoBehaviour, IDragHandler, IScrollHandler
    {
        GameObject stage, pivot;
        Camera capture;
        RenderTexture texture;
        float yaw = 20, pitch = 0, zoom = 1;
        int layer;
        public void Show(EquipmentItem item)
        {
            Clear();
            if (item == null || item.model == null) return;
            layer = LayerMask.NameToLayer("ItemInspect");
            if (layer < 0) { Debug.LogError("ItemInspect layer is missing."); return; }
            stage = new GameObject("Item Inspect Stage");
            stage.transform.position = new Vector3(0, -15000, 0);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(stage, gameObject.scene);
            stage.SetActive(false);
            pivot = new GameObject("Rotation Pivot"); pivot.transform.SetParent(stage.transform, false);
            var model = Instantiate(item.model, pivot.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(item.previewRotation);
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            foreach (var b in model.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var b in model.GetComponentsInChildren<Rigidbody>(true)) b.isKinematic = true;
            foreach (var a in model.GetComponentsInChildren<Animator>(true)) a.enabled = false;
            foreach (var c in model.GetComponentsInChildren<Camera>(true)) c.enabled = false;
            foreach (var l in model.GetComponentsInChildren<Light>(true)) l.enabled = false;
            stage.SetActive(true);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { Clear(); return; }
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            model.transform.position += stage.transform.position - bounds.center;
            pivot.transform.localScale = Vector3.one * (2f / Mathf.Max(size, 0.001f));
            var go = new GameObject("Inspect Camera", typeof(Camera)); go.transform.SetParent(stage.transform, false);
            capture = go.GetComponent<Camera>(); capture.enabled = false;
            capture.clearFlags = CameraClearFlags.SolidColor; capture.backgroundColor = Color.clear;
            capture.cullingMask = 1 << layer; capture.orthographic = true;
            capture.nearClipPlane = 0.01f; capture.farClipPlane = 20;
            capture.allowHDR = false; capture.allowMSAA = false;
            go.transform.localPosition = new Vector3(0, 0, -5);
            var lampObject = new GameObject("Inspect Light", typeof(Light)); lampObject.transform.SetParent(stage.transform, false);
            var lamp = lampObject.GetComponent<Light>(); lamp.type = LightType.Point;
            lamp.intensity = 5; lamp.range = 10; lamp.cullingMask = 1 << layer;
            lampObject.transform.localPosition = new Vector3(-2, 2, -3);
            texture = new RenderTexture(768, 768, 24, RenderTextureFormat.ARGB32);
            capture.targetTexture = texture;
            GetComponent<RawImage>().texture = texture;
            ResetView();
        }
        public void ResetView() { yaw = 20; pitch = 0; zoom = 1; Render(); }
        public void OnDrag(PointerEventData e) { yaw -= e.delta.x * 0.45f; pitch = Mathf.Clamp(pitch + e.delta.y * 0.4f, -80, 80); Render(); }
        public void OnScroll(PointerEventData e) { zoom = Mathf.Clamp(zoom - e.scrollDelta.y * 0.12f, 0.65f, 1.8f); Render(); }
        void Render()
        {
            if (capture == null) return;
            foreach (var c in Camera.allCameras) if (c != capture) c.cullingMask &= ~(1 << layer);
            pivot.transform.localRotation = Quaternion.Euler(pitch, yaw, 0);
            capture.orthographicSize = 1.4f * zoom; capture.Render();
        }
        public void Clear()
        {
            if (capture != null) capture.targetTexture = null;
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (stage != null) { stage.SetActive(false); Destroy(stage); }
            var raw = GetComponent<RawImage>(); if (raw != null) raw.texture = null;
            capture = null; stage = null; pivot = null; texture = null;
        }
        void OnDisable() => Clear();
        void OnDestroy() => Clear();
    }
}
