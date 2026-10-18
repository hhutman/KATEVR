// The bright golden bloom where the street dissolves into the haze.
// A soft additive glow on a large quad far ahead; buildings in front of it still occlude it,
// so they read as silhouettes against the light. No fog (it IS the fog's brightest point).
Shader "KateVR/HorizonGlow"
{
    Properties
    {
        _Color ("Glow", Color) = (1.0, 0.88, 0.62, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.55
        _Squash ("Wider Than Tall", Range(1, 4)) = 1.8
        _Center ("Glow Centre (UV)", Vector) = (0.5, 0.42, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-200" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Squash;
                float4 _Center;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes v)
            {
                Varyings o;
                ZERO_INITIALIZE(Varyings, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 d = (i.uv - _Center.xy) * float2(1.0, _Squash);
                half g = saturate(1.0 - length(d) * 2.0);
                g = g * g * (3.0 - 2.0 * g);
                return half4(_Color.rgb * g * _Intensity, 1);
            }
            ENDHLSL
        }
    }
}
