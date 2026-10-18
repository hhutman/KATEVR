// Kate's suit up close: weathered heavy fabric (sleeve) and worn leather / rubber (glove).
// Matte and dark, with dusty ash settled on the upward surfaces, procedural grain bumps
// (in object space, so the grain sticks to the cloth as the arm moves), darkened creases,
// a deep shadowed underside, and a warm golden-amber rim along the top ridges of the folds,
// knuckles and fingers, as if the hazy sun is catching them.
//
// Vertex colour carries the sculpt: R = fold ridge / knuckle (catches the rim), G = crease (darkens).
// Quest-safe: one pass, no textures, no shadows, single-pass-instanced stereo.
Shader "KateVR/SuitFabric"
{
    Properties
    {
        _BaseColor ("Colour (lit)", Color) = (0.20, 0.19, 0.14, 1)
        _ShadowColor ("Colour (in shadow)", Color) = (0.035, 0.03, 0.022, 1)
        _DustColor ("Ash Dust", Color) = (0.52, 0.40, 0.25, 1)
        _DustAmount ("Dust On Upper Surfaces", Range(0, 1)) = 0.3
        _Bounce ("Warm Light From The Haze Above", Range(0, 1)) = 0.35
        _Underside ("Underside Darkness", Range(0, 1)) = 0.6
        _RimColor ("Rim (golden amber)", Color) = (1.0, 0.72, 0.36, 1)
        _RimPower ("Rim Tightness", Range(0.5, 8)) = 2.6
        _RimStrength ("Rim Strength", Range(0, 4)) = 1.5
        _RidgeGlow ("Glow On Upward Fold Ridges", Range(0, 2)) = 0.7
        _CreaseDark ("Crease Darkening", Range(0, 1)) = 0.65
        _BumpFreq ("Grain Size, Coarse (per metre)", Float) = 140
        _BumpFreq2 ("Grain Size, Fine (per metre)", Float) = 520
        _BumpStrength ("Grain Strength", Range(0, 1.5)) = 0.35
        _Spec ("Sheen (leather)", Range(0, 1)) = 0.04
        _SpecPower ("Sheen Tightness", Range(2, 64)) = 12
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
                half4 _BaseColor;
                half4 _ShadowColor;
                half4 _DustColor;
                half _DustAmount;
                half _Bounce;
                half _Underside;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half _RidgeGlow;
                half _CreaseDark;
                float _BumpFreq;
                float _BumpFreq2;
                half _BumpStrength;
                half _Spec;
                half _SpecPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half4  sculpt     : TEXCOORD3;
                float  fog        : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            // Value noise with its slope, in one go (x = value, yzw = gradient).
            float4 NoiseD(float3 x)
            {
                float3 i = floor(x);
                float3 w = frac(x);
                float3 u = w * w * (3.0 - 2.0 * w);
                float3 du = 6.0 * w * (1.0 - w);
                float a = Hash(i);
                float b = Hash(i + float3(1, 0, 0));
                float c = Hash(i + float3(0, 1, 0));
                float d = Hash(i + float3(1, 1, 0));
                float e = Hash(i + float3(0, 0, 1));
                float f = Hash(i + float3(1, 0, 1));
                float g = Hash(i + float3(0, 1, 1));
                float h = Hash(i + float3(1, 1, 1));
                float k1 = b - a, k2 = c - a, k3 = e - a;
                float k4 = a - b - c + d, k5 = a - c - e + g, k6 = a - b - e + f;
                float k7 = -a + b + c - d + e - f - g + h;
                float v = a + k1 * u.x + k2 * u.y + k3 * u.z + k4 * u.x * u.y + k5 * u.y * u.z + k6 * u.z * u.x + k7 * u.x * u.y * u.z;
                float3 grad = du * float3(k1 + k4 * u.y + k6 * u.z + k7 * u.y * u.z,
                                          k2 + k5 * u.z + k4 * u.x + k7 * u.z * u.x,
                                          k3 + k6 * u.x + k5 * u.y + k7 * u.x * u.y);
                return float4(v, grad);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                ZERO_INITIALIZE(Varyings, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.positionOS = v.positionOS.xyz;
                o.sculpt = v.color;
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half ridge = i.sculpt.r;
                half crease = i.sculpt.g;

                // Grain: two octaves of bumps, tilting the surface normal.
                float4 n1 = NoiseD(i.positionOS * _BumpFreq);
                float4 n2 = NoiseD(i.positionOS * _BumpFreq2 + 17.3);
                float3 slope = TransformObjectToWorldDir(n1.yzw + n2.yzw * 0.5, false) * _BumpStrength;
                float3 N = normalize(i.normalWS);
                N = normalize(N - (slope - dot(slope, N) * N));

                float3 toCam = GetCameraPositionWS() - i.positionWS;
                float3 V = normalize(toCam);
                Light sun = GetMainLight();
                float3 L = sun.direction;
                half ndl = dot(N, L);

                // Ash dust settles on the upward-facing surfaces, less so in the creases.
                half dust = _DustAmount * saturate(N.y * 1.4 - 0.1) * (0.55 + 0.45 * n1.x) * (1.0 - crease * 0.7);
                half3 albedo = lerp(_BaseColor.rgb, _DustColor.rgb, dust);

                // Soft wrapped light, warm bounce from the bright haze overhead, deep shadow beneath.
                half wrap = saturate(ndl * 0.5 + 0.5);
                wrap *= wrap;
                half3 col = lerp(_ShadowColor.rgb, albedo, wrap);
                col += albedo * _Bounce * saturate(N.y * 0.6 + 0.4) * half3(1.0, 0.82, 0.58);
                col *= lerp(1.0, 1.0 - _Underside, saturate(-N.y * 1.3));
                col *= 1.0 - crease * _CreaseDark;

                // Golden rim: grazing edges, and the top ridges of folds / knuckles turned up to the haze.
                half facing = 1.0 - saturate(dot(N, V));
                half top = saturate(N.y * 0.75 + 0.4);
                half lightSide = 0.55 + 0.45 * saturate(ndl * 0.5 + 0.5);
                half rim = pow(facing, _RimPower) * (1.0 + ridge * 1.2);
                rim += ridge * _RidgeGlow * saturate(N.y * 1.3) * (0.3 + 0.7 * facing);
                rim *= top * lightSide * (1.0 - crease * 0.85);
                col += _RimColor.rgb * rim * _RimStrength;

                // Worn leather / rubber sheen.
                float3 H = normalize(L + V);
                col += _RimColor.rgb * _Spec * pow(saturate(dot(N, H)), _SpecPower) * (1.0 - crease) * (1.0 - dust);

                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
