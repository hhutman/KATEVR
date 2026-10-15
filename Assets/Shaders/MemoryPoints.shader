// A memory made of points: each point is a tiny soft disc that always faces the viewer.
// _Progress 0→1 drives the whole life of the memory:
//   0.00–0.28  points drift in from a loose cloud and settle into the image
//   0.28–0.55  the image holds, readable, breathing slightly
//   0.55–1.00  the image breaks apart like skywriting: points peel away on the wind,
//              sweeping across the image from one side, thinning and fading into the smog.
// Per-point data (TEXCOORD1): x = random seed, y = sweep (0 = first to go), z = edge (0 centre → 1 border), w = size multiplier.
Shader "KateVR/MemoryPoints"
{
    Properties
    {
        _PointSize ("Point Size (m)", Float) = 0.1
        _Progress ("Progress", Range(0, 1)) = 0
        _Scatter ("Scatter Before Forming (m)", Float) = 5
        _Wind ("Wind Direction (m)", Vector) = (1.6, 0.7, 0.4, 0)
        _Opacity ("Opacity", Range(0, 1)) = 0.9
        _FogAmount ("How Much the Smog Swallows It", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PointSize;
                float _Progress;
                float _Scatter;
                float4 _Wind;
                float _Opacity;
                float _FogAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float4 data       : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float  fog        : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 Hash3(float n)
            {
                return frac(sin(float3(n, n + 1.7, n + 3.3)) * float3(43758.5453, 22578.1459, 19642.3490)) * 2.0 - 1.0;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                ZERO_INITIALIZE(Varyings, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float seed = v.data.x;
                float sweep = v.data.y;
                float edge = v.data.z;
                float sizeMul = v.data.w;
                float t = _Time.y;

                // Forming: from a loose cloud into the image (edges settle last and stay looser).
                float form = smoothstep(0.0, 0.28, _Progress - sweep * 0.05 - edge * 0.04);
                float3 scatter = Hash3(seed) * _Scatter * (1.0 + edge * 1.5);
                float3 p = v.positionOS.xyz + scatter * (1.0 - form);

                // Breaking apart: sweeps across the image, then accelerates downwind.
                float d = saturate((_Progress - 0.55) / 0.45 * 1.7 - sweep * 0.7);
                d = d * d;
                float3 n = Hash3(seed * 1.37 + 11.0);
                p += _Wind.xyz * d * 6.0 + n * d * 2.5;

                // A slow breath, so the image never sits perfectly still.
                p += float3(sin(t * 0.7 + seed * 6.0), sin(t * 0.9 + seed * 4.0), cos(t * 0.6 + seed * 5.0)) * 0.03;

                // Billboard each point in view space (works per eye in single-pass stereo).
                float3 posWS = TransformObjectToWorld(p);
                float3 posVS = TransformWorldToView(posWS);
                float size = _PointSize * sizeMul * (1.0 + d * 1.5);
                posVS.xy += (v.uv - 0.5) * size;
                o.positionCS = TransformWViewToHClip(posVS);

                // Points that drift near the viewer's head fade out, so nothing appears "inside" the visor.
                float nearFade = smoothstep(1.5, 3.5, length(posVS));
                float alpha = _Opacity * v.color.a * form * (1.0 - d) * (1.0 - edge * 0.75) * nearFade;
                o.color = float4(v.color.rgb, alpha);
                o.uv = v.uv;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 c = i.uv - 0.5;
                float soft = saturate(1.0 - dot(c, c) * 4.0);
                soft *= soft;
                half3 rgb = i.color.rgb;
                rgb = lerp(rgb, MixFog(rgb, i.fog), _FogAmount);
                return half4(rgb, i.color.a * soft);
            }
            ENDHLSL
        }
    }
}
