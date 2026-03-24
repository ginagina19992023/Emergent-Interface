Shader "Emergent/WaterFloor"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (0.06, 0.34, 0.55, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.75
        [HDR] _SpecularColor ("Specular", Color) = (0.65, 0.92, 1, 1)
        _WaveSpeed ("Wave Speed", Float) = 0.7
        _WaveAmplitude ("Wave Height", Float) = 0.32
        _WaveFrequency ("Wave Size", Float) = 0.011
        _DeepColor ("Deep Tint", Color) = (0.02, 0.12, 0.22, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                half _Smoothness;
                float4 _SpecularColor;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float4 _DeepColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            static const float kPi = 3.14159265;

            void WaveHeightAndDerivs(float3 worldXZ, float t, out float h, out float dhdx, out float dhdz)
            {
                float k = _WaveFrequency * kPi * 2.0;
                float k2 = k * 1.35;
                float s = _WaveSpeed;

                float w1 = (worldXZ.x + worldXZ.z) * k + t * s;
                float w2 = (worldXZ.x * 0.62 - worldXZ.z * 0.91) * k2 + t * s * 1.18;
                float w3 = (worldXZ.x * 1.1 + worldXZ.z * -0.4) * k * 0.55 + t * s * 0.72;

                float s1 = sin(w1);
                float s2 = sin(w2);
                float s3 = sin(w3);

                h = _WaveAmplitude * (s1 + 0.45 * s2 + 0.28 * s3);

                float c1 = cos(w1);
                float c2 = cos(w2);
                float c3 = cos(w3);

                dhdx = _WaveAmplitude * (
                    k * c1
                    + 0.45 * (0.62 * k2) * c2
                    + 0.28 * (1.1 * k * 0.55) * c3);

                dhdz = _WaveAmplitude * (
                    k * c1
                    + 0.45 * (-0.91 * k2) * c2
                    + 0.28 * (-0.4 * k * 0.55) * c3);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float t = _Time.y;

                float h, dhdx, dhdz;
                WaveHeightAndDerivs(positionWS, t, h, dhdx, dhdz);
                positionWS.y += h;

                float3 normalWS = normalize(float3(-dhdx, 1.0, -dhdz));

                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = normalWS;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 n = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

                half facing = saturate(n.y);
                half3 baseCol = lerp(_DeepColor.rgb, _BaseColor.rgb, facing * 0.85 + 0.15);

                half ndotl = saturate(dot(n, mainLight.direction));
                half3 diffuse = baseCol * mainLight.color * ndotl;

                half3 ambient = baseCol * half3(0.07, 0.1, 0.14);
                half3 lit = diffuse + ambient;

                half3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half3 hVec = normalize(mainLight.direction + v);
                half spec = pow(saturate(dot(n, hVec)), 48.0) * _Smoothness;
                lit += spec * _SpecularColor.rgb * mainLight.color;

                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
