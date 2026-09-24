using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArmySurvivor
{
    [RequireComponent(typeof(Camera))]
    public class CameraWheelZoom : MonoBehaviour
    {
        [SerializeField] private Vector2 orthographicLimits = new Vector2(6, 55);
        [SerializeField] private Vector2 fieldOfViewLimits = new Vector2(25, 65);
        [SerializeField, Range(0.01f, 5f)] private float zoomPerStep = 0.12f;
        private Camera view;
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        private void Awake() => view = GetComponent<Camera>();

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0)) return;
            if (EventSystem.current != null)
            {
                uiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                    { position = mouse.position.ReadValue() }, uiHits);
                if (uiHits.Count > 0) return;
            }
            // Unity Input System의 일반 휠 한 칸은 120이다.
            Zoom(scroll / 120f);
        }

        public void Zoom(float steps)
        {
            if (view == null) view = GetComponent<Camera>();
            float multiplier = Mathf.Exp(-steps * zoomPerStep);
            if (view.orthographic)
                view.orthographicSize = Mathf.Clamp(view.orthographicSize * multiplier,
                    orthographicLimits.x, orthographicLimits.y);
            else
                view.fieldOfView = Mathf.Clamp(view.fieldOfView * multiplier,
                    fieldOfViewLimits.x, fieldOfViewLimits.y);
        }
    }
}
