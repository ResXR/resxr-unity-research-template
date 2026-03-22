Shader "URP/Line/FillShadowSoft"
{
    Properties
    {
        _BaseColor   ("Line Color", Color) = (1,1,1,1)
        _Opacity     ("Opacity", Range(0,1)) = 1

        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.5
        _ShadowDepth ("Shadow Depth (0-1)", Range(0,1)) = 0.3
        _ShadowPower ("Shadow Curve", Range(0.5,4)) = 1.5
        _ShadowFromTop ("Shadow From Top (1) / Bottom (0)", Float) = 1

        _EdgeSoftness ("Edge Softness (0-0.5)", Range(0,0.5)) = 0.12
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            float4 _BaseColor; float _Opacity;
            float4 _ShadowColor; float _ShadowStrength, _ShadowDepth, _ShadowPower, _ShadowFromTop;
            float  _EdgeSoftness;

            v2f vert(appdata v){ v2f o; o.pos=TransformObjectToHClip(v.vertex.xyz); o.uv=v.uv; return o; }

            float edgeAlpha(float2 uv, float soft)
            {
                float dist = abs(uv.y - 0.5) * 2.0; // 0 center -> 1 edge
                float core = 1.0 - saturate(dist);
                float aa = fwidth(core);
                return smoothstep(0.0, aa + soft, core);
            }

            float shadowFactor(float2 uv)
            {
                float d = (_ShadowFromTop >= 0.5) ? (1.0 - uv.y) : uv.y; // 0 at chosen edge
                float t = saturate(1.0 - d / max(_ShadowDepth,1e-4));
                return pow(t, _ShadowPower) * _ShadowStrength;
            }

            half4 frag(v2f i):SV_Target
            {
                float3 col = _BaseColor.rgb;
                col = lerp(col, _ShadowColor.rgb, shadowFactor(i.uv));
                float a = edgeAlpha(i.uv, _EdgeSoftness) * _Opacity;
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
