Shader "UI/TutorialSpotlight"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _DimColor ("Dim Color", Color) = (0,0,0,0.72)
        _OutlineColor ("Outline Color", Color) = (1,0.82,0.36,1)
        _Hole ("Hole XYWH", Vector) = (0.4,0.4,0.2,0.2)
        _Shape ("Circle", Float) = 0
        _Radius ("Corner Radius", Float) = 0.02
        _Thickness ("Outline Thickness", Float) = 0.004
        _Feather ("Edge Feather", Float) = 0.002
        _Blink ("Blink", Float) = 1
        _Aspect ("Canvas Aspect", Float) = 1.77778
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _DimColor, _OutlineColor;
            float4 _Hole;
            float _Shape, _Radius, _Thickness, _Feather, _Blink, _Aspect;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = _Hole.xy + _Hole.zw * 0.5;
                float2 p = (i.uv - center) * float2(_Aspect, 1);
                float2 halfSize = max(_Hole.zw * float2(_Aspect, 1) * 0.5, 0.0001);
                float d;
                if (_Shape > 0.5)
                    d = (length(p / halfSize) - 1.0) * min(halfSize.x, halfSize.y);
                else
                {
                    float2 q = abs(p) - max(halfSize - _Radius, 0.0);
                    d = length(max(q,0)) + min(max(q.x,q.y),0) - _Radius;
                }
                float edge = max(_Feather, 0.00001);
                float outside = smoothstep(-edge, edge, d);
                float ring = 1.0 - smoothstep(_Thickness, _Thickness + edge, abs(d));
                fixed4 color = _DimColor;
                color.a *= outside;
                color = lerp(color, _OutlineColor * fixed4(1,1,1,_Blink), saturate(ring));
                return color;
            }
            ENDCG
        }
    }
}
