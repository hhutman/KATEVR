// Drifted ash and silt: velvety, matte, softly lit, with warm golden rims along the ridges
// (strongest where the low sun sits behind them) and low mist pooling in the hollows further away.
// Hand-lit and cheap for Quest; single-pass-instanced stereo safe.
Shader "KateVR/AshDune"
{
    Properties
    {
        _ShadowColor ("Shadow (away from the sun)", Color) = (0.55, 0.36, 0.17, 1)
        _LitColor ("Lit", Color) = (0.86, 0.66, 0.38, 1)
        _RimColor ("Ridge Rim (golden)", Color) = (1.0, 0.86, 0.56, 1)
        _RimPower ("Rim Tightness", Range(1, 8)) = 3.2
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.9
        _Sheen ("Velvet Sheen", Range(0, 1)) = 0.25
        _MistColor ("Mist", Color) = (0.96, 0.84, 0.60, 1)
        _MistHeight ("Mist Pools Below (m)", Float) = 0.9
        _MistStrength ("Mist Strength", Range(0, 1)) = 0.65
        _MistStart ("Mist Starts At (m)", Float) = 5
        _MistFull ("Mist Full At (m)", Float) = 22
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                half4 _LitColor;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half _Sheen;
                half4 _MistColor;
                float _MistHeight;
                half _MistStrength;
                float _MistStart;
                float _MistFull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fog        : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                ZERO_INITIALIZE(Varyings, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 N = normalize(i.normalWS);
                float3 toCam = GetCameraPositionWS() - i.positionWS;
                float dist = length(toCam);
                float3 V = toCam / max(dist, 0.0001);
                Light sun = GetMainLight();
                float3 L = sun.direction;

                // Soft, wrapped diffuse: no hard terminator, like powder.
                half wrap = saturate(dot(N, L) * 0.5 + 0.5);
                wrap = wrap * wrap;
                half3 col = lerp(_ShadowColor.rgb, _LitColor.rgb, wrap);

                // Velvet: surfaces turning away from you brighten slightly.
                half facing = 1.0 - saturate(dot(N, V));
                col += _LitColor.rgb * _Sheen * pow(facing, 1.5);

                // Golden rim along ridges, strongest when backlit by the hazy sun.
                half backlit = 0.35 + 0.65 * saturate(dot(L, -V));
                col += _RimColor.rgb * _RimStrength * pow(facing, _RimPower) * backlit;

                // Low mist pooling in the hollows, thickening with distance.
                half low = saturate(1.0 - i.positionWS.y / max(_MistHeight, 0.01));
                half far = saturate((dist - _MistStart) / max(_MistFull - _MistStart, 0.01));
                col = lerp(col, _MistColor.rgb, low * low * far * _MistStrength);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
