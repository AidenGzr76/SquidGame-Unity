Shader "UI/SpotlightUnlit"
{
    Properties
    {
        _Color ("Tint", Color) = (0,0,0,0.75)
        _Center ("Center (UV)", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Float) = 0.2
        _Feather ("Feather", Float) = 0.02
        _Aspect ("Aspect Ratio (W/H)", Float) = 1.0
        [HideInInspector]_MainTex ("Sprite", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Feather;
            float _Aspect;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
                fixed4 col : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // تصحیح نسبت: X همون بمونه، Y ضرب در aspect
                float2 diff = uv - _Center.xy;
                diff.y *= _Aspect;

                float d = length(diff);

                float edge0 = _Radius;
                float edge1 = _Radius + max(_Feather, 1e-5);
                float hole = smoothstep(edge1, edge0, d);

                fixed4 dim = _Color * (1.0 - hole);
                return dim;
            }
            ENDCG
        }
    }
}
