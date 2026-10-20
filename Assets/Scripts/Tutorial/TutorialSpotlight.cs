using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Tutorial
{
    [RequireComponent(typeof(Image))]
    public sealed class TutorialSpotlight : MonoBehaviour, ICanvasRaycastFilter
    {
        private static readonly int Hole = Shader.PropertyToID("_Hole");
        private static readonly int Shape = Shader.PropertyToID("_Shape");
        private static readonly int Radius = Shader.PropertyToID("_Radius");
        private static readonly int Thickness = Shader.PropertyToID("_Thickness");
        private static readonly int Feather = Shader.PropertyToID("_Feather");
        private static readonly int Dim = Shader.PropertyToID("_DimColor");
        private static readonly int Outline = Shader.PropertyToID("_OutlineColor");
        private static readonly int Blink = Shader.PropertyToID("_Blink");
        private static readonly int Aspect = Shader.PropertyToID("_Aspect");
        private RectTransform rect;
        private Image image;
        private Material instance;
        private Rect hole;
        private TutorialHoleShape holeShape;
        private float radius;
        public bool BlockOutside { get; set; }
        public bool BlockHole { get; set; }
        public bool HasHole { get; private set; }

        private void Awake()
        {
            rect = (RectTransform)transform;
            image = GetComponent<Image>();
            // A missing shader must never leave the full-screen default white Image visible.
            image.enabled = false;
            image.raycastTarget = true;
        }

        public void Configure(TutorialStyle style)
        {
            Shader shader = style != null ? style.spotlightShader : null;
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("Tutorial spotlight shader is missing or unsupported. Assign it in TutorialStyle.", this);
                image.enabled = false;
                return;
            }

            instance = new Material(shader) { name = "Tutorial Spotlight Instance" };
            image.material = instance;
            image.color = Color.white;
            image.enabled = true;
        }

        public void Draw(Rect localHole, TutorialHoleShape shape, float cornerRadius, TutorialStyle style)
        {
            HasHole = localHole.width > 0 && localHole.height > 0;
            hole = localHole;
            holeShape = shape;
            radius = cornerRadius;
            if (instance == null) return;
            Rect r = rect.rect;
            instance.SetVector(Hole, HasHole ? new Vector4(
                (hole.xMin - r.xMin) / r.width, (hole.yMin - r.yMin) / r.height,
                hole.width / r.width, hole.height / r.height) : new Vector4(-2, -2, 0, 0));
            instance.SetFloat(Shape, shape == TutorialHoleShape.Circle ? 1 : 0);
            instance.SetFloat(Aspect, r.width / r.height);
            instance.SetFloat(Radius, cornerRadius / r.height);
            instance.SetFloat(Thickness, style.outlineThickness / r.height);
            instance.SetFloat(Feather, style.edgeFeather / r.height);
            instance.SetColor(Dim, style.dimColor);
            instance.SetColor(Outline, style.outlineColor);
            instance.SetFloat(Blink, 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * style.blinkSpeed * Mathf.PI * 2));
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (BlockHole) return true;
            if (!BlockOutside) return false;
            if (!HasHole) return true;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out Vector2 local)) return true;
            if (holeShape == TutorialHoleShape.Circle)
            {
                Vector2 p = local - hole.center;
                Vector2 half = hole.size * 0.5f;
                return (p.x * p.x / (half.x * half.x) + p.y * p.y / (half.y * half.y)) > 1;
            }
            Vector2 q = new Vector2(Mathf.Abs(local.x - hole.center.x), Mathf.Abs(local.y - hole.center.y));
            Vector2 inner = hole.size * 0.5f - Vector2.one * radius;
            Vector2 corner = Vector2.Max(q - inner, Vector2.zero);
            bool inside = corner.magnitude <= radius || (q.x <= inner.x && q.y <= hole.height * 0.5f) ||
                (q.y <= inner.y && q.x <= hole.width * 0.5f);
            return !inside;
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance);
        }
    }
}
