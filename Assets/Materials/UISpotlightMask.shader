Shader "UI/SpotlightMask"
{
    Properties
    {
        _Color ("Dim Color", Color) = (0,0,0,0.75)
        _Center ("Center (UV)", Vector) = (0.5,0.5,0,0)
        _Radius ("Radius", Float) = 0.2
        _Feather ("Feather", Float) = 0.02
        [HideInInspector]_MainTex ("Sprite", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
            sampler2D _MainTex;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // گرفتن نسبت صفحه برای جلوگیری از بیضی
                float aspect = (float) _ScreenParams.x / (float) _ScreenParams.y;
                float2 diff = uv - _Center.xy;
                diff.x *= aspect; // ✅ جبران کشیدگی

                float d = length(diff);

                float edge0 = _Radius;
                float edge1 = _Radius + max(_Feather, 1e-5);
                float hole  = smoothstep(edge1, edge0, d); // داخل دایره=1 ، بیرون=0

                // سیاهی با سوراخ
                return _Color * (1 - hole);
            }
            ENDCG
        }
    }
}
