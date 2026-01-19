Shader "Custom/Desintegration"
{
 Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        [Space(10)]
        [Header(Lighting)]
        _AmbientColor("Ambient Color", Color) = (0.4, 0.4, 0.4, 1)
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpStr("Normal Strength", Float) = 1.0
        
        [Space(10)]
        [Header(Dissolution)]
        _DissolveTexture("Dissolve Texture", 2D) = "white" {}
        [HDR] _DissolveColor("Dissolve Border Color", Color) = (1, 1, 1, 1)
        _DissolveBorder("Dissolve Border", Float) = 0.05
        
        [Space(10)]
        [Header(Disintegration)]
        _FlowMap("Flow Map (RG)", 2D) = "black" {}
        _Expand("Expand", Float) = 1.0
        _Weight("Weight", Range(0, 1)) = 0
        _Direction("Direction", Vector) = (0, 0, 0, 0)
        [HDR] _DisintegrationColor("Disintegration Color", Color) = (1, 1, 1, 1)
        _Glow("Glow", Float) = 1.0
        
        [Space(10)]
        [Header(Particles)]
        _Shape("Shape Texture", 2D) = "white" {}
        _R("Particle Radius", Float) = 0.1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        LOD 100
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.6

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _AmbientColor;
                float4 _FlowMap_ST;
                float4 _DissolveColor;
                float4 _Direction;
                float4 _DisintegrationColor;
                
                float _BumpStr;
                float _DissolveBorder;
                float _Expand;
                float _Weight;
                float _Glow;
                float _R;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_FlowMap);
            SAMPLER(sampler_FlowMap);
            TEXTURE2D(_DissolveTexture);
            SAMPLER(sampler_DissolveTexture);
            TEXTURE2D(_Shape);
            SAMPLER(sampler_Shape);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionOS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD1;
            };

            struct GeomOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD1;
            };

            float Random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float RemapValue(float value, float from1, float to1, float from2, float to2)
            {
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }

            float4 RemapFlowTexture(float4 tex)
            {
                return float4(
                    RemapValue(tex.x, 0, 1, -1, 1),
                    RemapValue(tex.y, 0, 1, -1, 1),
                    0,
                    RemapValue(tex.w, 0, 1, -1, 1)
                );
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionOS = input.positionOS;
                output.uv = input.uv;
                output.normalWS = normalInput.normalWS;
                output.positionWS = vertexInput.positionWS;
                
                return output;
            }

            [maxvertexcount(7)]
            void geom(triangle Varyings input[3], inout TriangleStream<GeomOutput> triStream)
            {
                // Average values
                float2 avgUV = (input[0].uv + input[1].uv + input[2].uv) / 3.0;
                float3 avgPosOS = (input[0].positionOS.xyz + input[1].positionOS.xyz + input[2].positionOS.xyz) / 3.0;
                float3 avgNormalWS = normalize(input[0].normalWS + input[1].normalWS + input[2].normalWS);

                // Sample dissolve texture
                float dissolveValue = SAMPLE_TEXTURE2D_LOD(_DissolveTexture, sampler_DissolveTexture, avgUV, 0).r;
                float t = saturate(_Weight * 2.0 - dissolveValue);

                // Flow map
                float2 flowUV = avgPosOS.xz * _FlowMap_ST.xy + _FlowMap_ST.zw;
                float4 flowVector = RemapFlowTexture(SAMPLE_TEXTURE2D_LOD(_FlowMap, sampler_FlowMap, flowUV, 0));

                // Calculate pseudo-random position
                float3 pseudoRandomPos = avgPosOS + _Direction.xyz;
                pseudoRandomPos += flowVector.xyz * _Expand;

                float3 p = lerp(avgPosOS, pseudoRandomPos, t);
                float radius = lerp(_R, 0, t);

                // Generate particle billboard if dissolving
                if (t > 0)
                {
                    float3 posWS = TransformObjectToWorld(p);
                    float3 toCamera = normalize(GetCameraPositionWS() - posWS);
                    
                    // Billboard vectors
                    float3 up = float3(0, 1, 0);
                    float3 right = normalize(cross(up, toCamera));
                    up = normalize(cross(toCamera, right));

                    float halfS = 0.5 * radius;

                    float3 v[4];
                    v[0] = p + halfS * right - halfS * up;
                    v[1] = p + halfS * right + halfS * up;
                    v[2] = p - halfS * right - halfS * up;
                    v[3] = p - halfS * right + halfS * up;

                    GeomOutput o;
                    
                    // Vertex 0
                    o.positionCS = TransformObjectToHClip(v[0]);
                    o.positionWS = TransformObjectToWorld(v[0]);
                    o.uv = float2(1, 0);
                    o.color = float4(1, 1, 1, 1);
                    o.normalWS = avgNormalWS;
                    triStream.Append(o);

                    // Vertex 1
                    o.positionCS = TransformObjectToHClip(v[1]);
                    o.positionWS = TransformObjectToWorld(v[1]);
                    o.uv = float2(1, 1);
                    o.color = float4(1, 1, 1, 1);
                    o.normalWS = avgNormalWS;
                    triStream.Append(o);

                    // Vertex 2
                    o.positionCS = TransformObjectToHClip(v[2]);
                    o.positionWS = TransformObjectToWorld(v[2]);
                    o.uv = float2(0, 0);
                    o.color = float4(1, 1, 1, 1);
                    o.normalWS = avgNormalWS;
                    triStream.Append(o);

                    // Vertex 3
                    o.positionCS = TransformObjectToHClip(v[3]);
                    o.positionWS = TransformObjectToWorld(v[3]);
                    o.uv = float2(0, 1);
                    o.color = float4(1, 1, 1, 1);
                    o.normalWS = avgNormalWS;
                    triStream.Append(o);

                    triStream.RestartStrip();
                }

                // Original triangle
                for (int j = 0; j < 3; j++)
                {
                    GeomOutput o;
                    o.positionCS = TransformObjectToHClip(input[j].positionOS.xyz);
                    o.positionWS = input[j].positionWS;
                    o.uv = TRANSFORM_TEX(input[j].uv, _BaseMap);
                    o.color = float4(0, 0, 0, 0);
                    o.normalWS = input[j].normalWS;
                    triStream.Append(o);
                }

                triStream.RestartStrip();
            }

            half4 frag(GeomOutput input) : SV_Target
            {
                // Base color
                half4 albedo = _BaseColor;
                
                // Optional: sample base texture if assigned
                #if defined(_BASEMAP)
                    albedo *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                #endif

                // Normal mapping
                float3 normalWS = normalize(input.normalWS);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                normalTS.xy *= _BumpStr;
                
                // Simple lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = _AmbientColor.rgb + mainLight.color * NdotL;
                
                half4 color = albedo * half4(lighting, 1);

                // Particle glow effect
                float brightness = input.color.w * _Glow;
                color = lerp(color, _DisintegrationColor, input.color.x);

                if (brightness > 0)
                {
                    color *= brightness + _Weight;
                }

                // Dissolve effect
                float dissolve = SAMPLE_TEXTURE2D(_DissolveTexture, sampler_DissolveTexture, input.uv).r;

                if (input.color.w == 0) // Original mesh
                {
                    clip(dissolve - 2.0 * _Weight);
                    
                    if (_Weight > 0)
                    {
                        float border = step(dissolve - 2.0 * _Weight, _DissolveBorder);

                        if(border > 0)
                        {
                            color = _DissolveColor * _Glow * border;
                                    
                        } 


                    }
                }
                else // Particle
                {
                    float shape = SAMPLE_TEXTURE2D(_Shape, sampler_Shape, input.uv).r;
                    clip(shape - 0.5);
                }

                return color;
            }
            ENDHLSL
        }

Pass
{
    Name "ShadowCaster"
    Tags { "LightMode" = "ShadowCaster" }

    ZWrite On
    ZTest LEqual
    ColorMask 0
    Cull Off

    HLSLPROGRAM
    #pragma vertex vert
    #pragma geometry geom
    #pragma fragment frag
    #pragma target 4.6

    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float _Weight;
    CBUFFER_END

    TEXTURE2D(_DissolveTexture);
    SAMPLER(sampler_DissolveTexture);
    TEXTURE2D(_Shape);
    SAMPLER(sampler_Shape);

    struct Attributes
    {
        float4 positionOS : POSITION;
        float3 normalOS : NORMAL;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionOS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 normalWS : NORMAL;
    };

    struct GeomOutput
    {
        float4 positionCS : SV_POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
    };

    Varyings vert(Attributes input)
    {
        Varyings output;
        output.positionOS = input.positionOS;
        output.uv = input.uv;
        output.normalWS = TransformObjectToWorldNormal(input.normalOS);
        return output;
    }

    [maxvertexcount(3)]
    void geom(triangle Varyings input[3], inout TriangleStream<GeomOutput> triStream)
    {
        for (int j = 0; j < 3; j++)
        {
            GeomOutput o;
            
            float3 positionWS = TransformObjectToWorld(input[j].positionOS.xyz);
            float3 normalWS = input[j].normalWS;
            
            // Appliquer un bias manuel pour les ombres
            float4 positionCS = TransformWorldToHClip(positionWS);
            
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
            #endif
            
            o.positionCS = positionCS;
            o.uv = input[j].uv;
            o.color = float4(0, 0, 0, 0);
            triStream.Append(o);
        }
    }

    half4 frag(GeomOutput input) : SV_Target
    {
        float dissolve = SAMPLE_TEXTURE2D(_DissolveTexture, sampler_DissolveTexture, input.uv).r;

        if (input.color.w == 0)
        {
            clip(dissolve - 2.0 * _Weight);
        }
        else
        {
            float shape = SAMPLE_TEXTURE2D(_Shape, sampler_Shape, input.uv).r;
            clip(shape - 0.5);
        }

        return 0;
    }
    ENDHLSL
}
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
