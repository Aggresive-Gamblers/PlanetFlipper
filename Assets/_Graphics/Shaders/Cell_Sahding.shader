Shader "Custom/URP_CellShader_Outline"
{
    Properties
    {
        [Header(Cell Shading)]
        _ToonSteps ("Toon Steps", Range(2, 10)) = 3

        [Header(Outline)]
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Vertex Color)]
        _VertexColorStrength ("Vertex Color Strength", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        // =====================
        // Outline Pass
        // =====================
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" "Queue"="Geometry+1" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Blend Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _OutlineWidth;
                float4 _OutlineColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                posWS += normalWS * _OutlineWidth;

                output.positionCS = TransformWorldToHClip(posWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // =====================
        // Cell Shader Pass
        // =====================
        Pass
        {
            Name "CellShading"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float4 vertexColor : COLOR;
                float fogFactor : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _ToonSteps;
                float _VertexColorStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.vertexColor = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 baseColor = input.vertexColor.rgb * _VertexColorStrength;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionCS));

                half3 normalWS = normalize(input.normalWS);
                half3 lightDir = normalize(mainLight.direction);
                half NdotL = dot(normalWS, lightDir);
                NdotL = saturate(NdotL);

                NdotL = floor(NdotL * _ToonSteps) / _ToonSteps;

                half3 color = baseColor * mainLight.color * NdotL;

                 color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);      
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
