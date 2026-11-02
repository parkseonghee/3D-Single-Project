using UnityEngine;
using UnityEngine.UI;

namespace ArmySurvivor.Equipment
{
    public sealed class SupplyUIGradient : BaseMeshEffect
    {
        public bool horizontal;
        public Color[] colors;
        public float[] stops;
        public override void ModifyMesh(VertexHelper vh)
        {
            if(!IsActive()||colors==null||stops==null||colors.Length!=stops.Length)return;
            vh.Clear();var r=((RectTransform)transform).rect;
            for(int i=0;i<stops.Length-1;i++)
            {
                float a=stops[i],b=stops[i+1];int n=vh.currentVertCount;
                var p0=horizontal?new Vector2(Mathf.Lerp(r.xMin,r.xMax,a),r.yMin):new Vector2(r.xMin,Mathf.Lerp(r.yMax,r.yMin,a));
                var p1=horizontal?new Vector2(p0.x,r.yMax):new Vector2(r.xMax,p0.y);
                var p2=horizontal?new Vector2(Mathf.Lerp(r.xMin,r.xMax,b),r.yMax):new Vector2(r.xMax,Mathf.Lerp(r.yMax,r.yMin,b));
                var p3=horizontal?new Vector2(p2.x,r.yMin):new Vector2(r.xMin,p2.y);
                vh.AddVert(p0,colors[i],Vector2.zero);vh.AddVert(p1,colors[i],Vector2.zero);
                vh.AddVert(p2,colors[i+1],Vector2.zero);vh.AddVert(p3,colors[i+1],Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
