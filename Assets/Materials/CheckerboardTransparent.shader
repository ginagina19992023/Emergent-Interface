Shader "Emergent/CheckerboardTransparent"
{
    Properties
    {
        _WhiteTile ("White Tile", Color) = (1, 1, 1, 0.4)
        _OtherTile ("Other Tile", Color) = (1, 1, 1, 0.05)
        _TilesU ("Tiles Along U", Float) = 16
        _TilesV ("Tiles Along V", Float) = 14
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WhiteTile;
                float4 _OtherTile;
                float _TilesU;
                float _TilesV;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 grid = floor(input.uv * float2(max(_TilesU, 0.001), max(_TilesV, 0.001)));
                half useWhite = step(0.25h, frac((grid.x + grid.y) * 0.5));
                half4 c = lerp(_OtherTile, _WhiteTile, useWhite);
                half3 rgb = MixFog(c.rgb, input.fogFactor);
                return half4(rgb, c.a);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
