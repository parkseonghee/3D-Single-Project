using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArmySurvivor.Building
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Camera))]
    public class VillageCameraDrag : MonoBehaviour
    {
        [SerializeField] private Collider buildSurface;
        [SerializeField] private Vector2 viewLimit = new Vector2(75, 55);
        [SerializeField, Min(1)] private float dragThreshold = 8;
        private Camera view;
        private Vector2 pressPosition;
        private Vector3 anchor;
        private bool pressed;
        private readonly System.Collections.Generic.List<RaycastResult> uiHits = new System.Collections.Generic.List<RaycastResult>();
        public bool IsDragging { get; private set; }
        public bool ClickedThisFrame { get; private set; }
        public bool DragEndedThisFrame { get; private set; }

        private void Awake() => view = GetComponent<Camera>();

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            bool overUi = false;
            if (EventSystem.current != null)
            {
                uiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                    { position = mouse.position.ReadValue() }, uiHits);
                overUi = uiHits.Count > 0;
            }
            ProcessPointer(mouse.position.ReadValue(), mouse.leftButton.wasPressedThisFrame,
                mouse.leftButton.isPressed, mouse.leftButton.wasReleasedThisFrame, overUi);
        }

        public void ProcessPointer(Vector2 position, bool down, bool held, bool up, bool overUi)
        {
            ClickedThisFrame = DragEndedThisFrame = false;
            if (down)
            {
                pressed = !overUi && GroundPoint(position, out anchor);
                pressPosition = position;
                IsDragging = false;
            }
            if (!pressed) return;
            if (held || up)
            {
                if ((position - pressPosition).sqrMagnitude >= dragThreshold * dragThreshold) IsDragging = true;
                if (IsDragging && GroundPoint(position, out Vector3 point)) Pan(anchor - point);
            }
            if (up)
            {
                ClickedThisFrame = !IsDragging && !overUi;
                DragEndedThisFrame = IsDragging;
                pressed = IsDragging = false;
            }
        }

        private bool GroundPoint(Vector2 screen, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, new Vector3(0, buildSurface.bounds.max.y, 0));
            Ray ray = view.ScreenPointToRay(screen);
            bool hit = plane.Raycast(ray, out float distance);
            point = ray.GetPoint(distance);
            return hit;
        }

        private void Pan(Vector3 delta)
        {
            if (!GroundPoint(new Vector2(view.pixelWidth * 0.5f, view.pixelHeight * 0.5f), out Vector3 focus)) return;
            Vector3 center = buildSurface.bounds.center;
            Vector3 target = focus + delta;
            target.x = Mathf.Clamp(target.x, center.x - viewLimit.x, center.x + viewLimit.x);
            target.z = Mathf.Clamp(target.z, center.z - viewLimit.y, center.z + viewLimit.y);
            transform.position += new Vector3(target.x - focus.x, 0, target.z - focus.z);
        }

        private void OnDisable()
        {
            pressed = IsDragging = ClickedThisFrame = DragEndedThisFrame = false;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) OnDisable();
        }
    }
}
