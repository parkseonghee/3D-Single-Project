using System;
using System.Collections;
using System.Collections.Generic;
using ArmySurvivor.Building;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArmySurvivor.Equipment
{
    // Presentation only: the inventory commits the roll exactly once before Play.
    public sealed class SupplyDrawSequence : MonoBehaviour
    {
        public bool IsPlaying { get; private set; }
        readonly List<Action> restore = new List<Action>();
        GameObject stage;
        Action completed;

        public void Play(EquipmentCatalog data, Action onComplete)
        {
            if (IsPlaying) return;
            Transform gate = null;
            Camera source = null;
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>())
                    if (t.name == "Wall_A_gate" && t.Find("Bone_Base") != null) gate = t;
                foreach (var c in root.GetComponentsInChildren<Camera>())
                    if (c.enabled && c.targetTexture == null) source = c;
            }
            if (gate == null || source == null || data.supplyGateOpen == null || data.supplyFirework == null)
            {
                Debug.LogWarning("Supply cinematic unavailable: check gate, camera and EquipmentCatalog cinematic references.");
                onComplete?.Invoke();
                return;
            }
            completed = onComplete;
            IsPlaying = true;
            StartCoroutine(Animate(data, gate, source));
        }

        IEnumerator Animate(EquipmentCatalog data, Transform gate, Camera source)
        {
            try
            {
                stage = new GameObject("Supply Cinematic (temporary)");
                stage.transform.SetParent(transform, false);
                var camera = new GameObject("Supply Camera").AddComponent<Camera>();
                camera.transform.SetParent(stage.transform);
                camera.CopyFrom(source);
                camera.targetTexture = null;
                camera.orthographic = false;
                camera.fieldOfView = data.supplyCameraFov;
                camera.enabled = true;
                source.enabled = false;
                restore.Add(() => { if (source) source.enabled = true; });

                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                    {
                        var group = canvas.GetComponent<CanvasGroup>();
                        bool added = group == null;
                        if (added) group = canvas.gameObject.AddComponent<CanvasGroup>();
                        float alpha = group.alpha;
                        bool blocks = group.blocksRaycasts, interactable = group.interactable;
                        var captured = group;
                        restore.Add(() =>
                        {
                            if (!captured) return;
                            captured.alpha = alpha; captured.blocksRaycasts = blocks; captured.interactable = interactable;
                            if (added) Destroy(captured);
                        });
                        group.alpha = 0; group.blocksRaycasts = false; group.interactable = false;
                    }
                    foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>())
                        if (behaviour.enabled && (behaviour is BuildingPlacementController || behaviour is VillageCameraDrag || behaviour is CameraWheelZoom))
                        {
                            var captured = behaviour;
                            captured.enabled = false;
                            restore.Add(() => { if (captured) captured.enabled = true; });
                        }
                }
                foreach (var animator in gate.GetComponentsInChildren<Animator>())
                {
                    bool enabledBefore = animator.enabled;
                    animator.enabled = false;
                    restore.Add(() => { if (animator) animator.enabled = enabledBefore; });
                }
                foreach (var t in gate.GetComponentsInChildren<Transform>(true))
                {
                    var position = t.localPosition; var rotation = t.localRotation; var scale = t.localScale;
                    restore.Add(() => { if (t) { t.localPosition = position; t.localRotation = rotation; t.localScale = scale; } });
                }
                // The village's front is world -Z. Offsets stay relative to the gate position.
                Vector3 origin = gate.position;
                float duration = Mathf.Max(1, data.supplySequenceDuration);
                float elapsed = 0, nextBurst = 0;
                while (elapsed < duration)
                {
                    float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
                    camera.transform.position = origin + Vector3.Lerp(data.supplyCameraStart, data.supplyCameraEnd, progress);
                    camera.transform.LookAt(origin + data.supplyLookAt);
                    float opening = Mathf.Clamp01((elapsed - duration * data.supplyDoorStart) / Mathf.Max(.1f, data.supplyDoorDuration));
                    data.supplyGateOpen.SampleAnimation(gate.gameObject, opening * data.supplyGateOpen.length);
                    if (elapsed >= nextBurst)
                    {
                        nextBurst = elapsed + Mathf.Max(.05f, data.supplyFireworkInterval);
                        for (int side = -1; side <= 1; side += 2)
                        {
                            var point = origin + new Vector3(side * UnityEngine.Random.Range(5f, 14f), UnityEngine.Random.Range(1f, 5f), UnityEngine.Random.Range(0f, 10f));
                            var fx = Instantiate(data.supplyFirework, point, Quaternion.identity, stage.transform);
                            fx.transform.localScale *= data.supplyFireworkScale;
                            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>())
                            {
                                var main = ps.main; main.useUnscaledTime = true;
                            }
                            Destroy(fx, 6);
                        }
                    }
                    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) break;
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                Restore();
                var callback = completed; completed = null;
                if (isActiveAndEnabled) callback?.Invoke();
            }
        }

        void Restore()
        {
            for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
            restore.Clear();
            if (stage) { stage.SetActive(false); Destroy(stage); }
            stage = null;
            IsPlaying = false;
        }
        void OnDisable()
        {
            completed = null;
            StopAllCoroutines(); Restore();
        }
    }
}
