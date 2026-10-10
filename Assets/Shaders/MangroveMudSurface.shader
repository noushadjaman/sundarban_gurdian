Shader "Custom/MangroveMudSurface"
{
    Properties
    {
        _BaseMap ("Mud Base Texture", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0.0, 3.0)) = 1.5
        _BaseColor ("Color Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        _Smoothness ("Mud Smoothness (Wetness)", Range(0.0, 1.0)) = 0.55
        _Metallic ("Metallic", Range(0.0, 1.0)) = 0.0
        _WetMudDarken ("Wet Mud Darkening Power", Range(0.5, 3.0)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float4 color      : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                float4 _BaseColor;
                float _BumpScale;
                float _Smoothness;
                float _Metallic;
                float _WetMudDarken;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Sample Base Map
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);

                // 2. Multiply by Vertex Colors! (Author's baked wet-mud, dry-silt, and leaf-litter)
                // Vertex colors in the GLB are 0.10 - 0.25 (dark wet mud). We calibrate it for URP lighting.
                half3 vCol = pow(input.color.rgb, _WetMudDarken);
                half3 albedo = texColor.rgb * vCol * _BaseColor.rgb * 4.2;

                // 3. Normal mapping
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                
                half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv * 2.6);
                half3 normalTS = UnpackNormalScale(normalSample, _BumpScale);
                float3 normalWS = normalize(mul(normalTS, tbn));

                // 4. Lighting & Specular
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * (NdotL * mainLight.shadowAttenuation);

                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                
                // Wet mud glistens along the water edge
                float wetness = saturate(1.0 - input.positionWS.y * 1.5);
                float effectiveSmoothness = lerp(_Smoothness * 0.5, _Smoothness, wetness);
                float specular = pow(NdotH, 32.0 * effectiveSmoothness) * effectiveSmoothness * 0.45;

                // Ambient light
                half3 ambient = SampleSH(normalWS) * 0.85;

                half3 finalRGB = albedo * (diffuse + ambient) + (mainLight.color * specular * mainLight.shadowAttenuation);
                return half4(finalRGB, 1.0);
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

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct VaryingsShadow
            {
                float4 positionCS : SV_POSITION;
            };

            VaryingsShadow vertShadow(AttributesShadow input)
            {
                VaryingsShadow output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                return output;
            }

            half4 fragShadow(VaryingsShadow input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
