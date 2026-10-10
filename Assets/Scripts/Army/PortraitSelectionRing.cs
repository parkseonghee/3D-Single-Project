using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Army
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PortraitSelectionRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            const int segments = 64;
            var rect = rectTransform.rect;
            float outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            float inner = outer - 2;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(rect.center + direction * outer, color, Vector2.zero);
                vh.AddVert(rect.center + direction * inner, color, Vector2.zero);
                if (i == segments) continue;
                int v = i * 2;
                vh.AddTriangle(v, v + 2, v + 1);
                vh.AddTriangle(v + 1, v + 2, v + 3);
            }
        }
    }
}
