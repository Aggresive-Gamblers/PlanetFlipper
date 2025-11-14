Shader "Custom/URP_VertexColorLit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _SpecColor("Specular Color", Color) = (0.2,0.2,0.2,1)
        _Gloss("Gloss", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "FORWARD"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            // Propriétés
            float4 _BaseColor;
            float4 _SpecColor;
            float _Gloss;

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR; // <- réception des couleurs par-vertex
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float4 color : COLOR;
                UNITY_FOG_COORDS(1)
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float4 worldPos = mul(unity_ObjectToWorld, IN.vertex);
                OUT.posWS = worldPos.xyz;
                OUT.positionCS = UnityObjectToClipPos(IN.vertex);
                OUT.normalWS = UnityObjectToWorldNormal(IN.normal);
                OUT.color = IN.color; // on transmet la couleur au fragment
                UNITY_TRANSFER_FOG(OUT, OUT.positionCS);
                return OUT;
            }

            // Fragment simple : albédo = vertexColor * baseColor ; éclairage Lambert + spec simple
            fixed4 frag(Varyings IN) : SV_Target
            {
                // Normal et direction de la lumière principale (directe)
                float3 N = normalize(IN.normalWS);
                float3 L = normalize(_WorldSpaceLightPos0.xyz); // compatibilité basique
                float3 V = normalize(_WorldSpaceCameraPos - IN.posWS);

                float NdotL = saturate(dot(N, L));
                float3 diffuseLighting = _LightColor0.rgb * NdotL;

                // simple speculaire Blinn-Phong
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), 16.0 * (1.0 - _Gloss) + 4.0);
                float3 specularLighting = _SpecColor.rgb * spec * _LightColor0.rgb;

                // couleur finale = couleur par-vertex * base color * (diffuse + ambient) + spec
                float3 vertexCol = IN.color.rgb;
                float3 baseCol = vertexCol * _BaseColor.rgb;

                // petit ambient approximatif
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * 0.25;

                float3 colorOut = baseCol * (ambient + diffuseLighting) + specularLighting;
                colorOut = saturate(colorOut);

                fixed4 outCol = fixed4(colorOut, 1.0);
                UNITY_APPLY_FOG(IN, outCol);
                return outCol;
            }
            ENDHLSL
        }
    }

    FallBack "Diffuse"
}