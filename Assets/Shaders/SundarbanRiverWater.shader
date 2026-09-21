Shader "Custom/SundarbanRiverWater"
{
    Properties
    {
        _ShallowColor ("Shallow Water Color (Inland River)", Color) = (0.22, 0.40, 0.34, 0.65)
        _DeepColor ("Deep Water Color (Inland River)", Color) = (0.12, 0.28, 0.24, 0.94)
        _OceanBlueColor ("Ocean Blue Deep Color (Near Sea)", Color) = (0.08, 0.36, 0.58, 0.96)
        _OceanShallowBlueColor ("Ocean Blue Shallow Color (Near Sea)", Color) = (0.18, 0.54, 0.68, 0.72)
        _SeaTransitionZStart ("Sea Transition Start Z (Inland)", Float) = 110.0
        _SeaTransitionZEnd ("Sea Transition End Z (Sea)", Float) = -40.0
        _SkyReflectionColor ("Sky Reflection Color", Color) = (0.72, 0.84, 0.92, 1.0)
        _FoamColor ("Foam / Shore Ripples", Color) = (0.80, 0.86, 0.82, 0.90)
        _FoamDistance ("Shore Blend Distance", Range(0.01, 2.0)) = 0.5
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.92
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _WaveSpeed ("Wave Speed (XY, ZW)", Vector) = (0.015, 0.025, -0.010, 0.018)
        _NormalTiling ("Normal Tiling", Float) = 4.0
        _BumpScale ("Bump Scale", Range(0.0, 2.0)) = 0.85
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float4 tangentWS    : TEXCOORD2;
                float2 uv           : TEXCOORD3;
                float4 screenPos    : TEXCOORD4;
                float4 color        : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _OceanBlueColor;
                float4 _OceanShallowBlueColor;
                float _SeaTransitionZStart;
                float _SeaTransitionZEnd;
                float4 _SkyReflectionColor;
                float4 _FoamColor;
                float _FoamDistance;
                float _Smoothness;
                float4 _WaveSpeed;
                float _NormalTiling;
                float _BumpScale;
            CBUFFER_END

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Flowing dual normal maps
                float2 uv1 = input.uv * _NormalTiling + _Time.y * _WaveSpeed.xy;
                float2 uv2 = input.uv * (_NormalTiling * 1.6) + _Time.y * _WaveSpeed.zw;
                
                half3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv1), _BumpScale);
                half3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv2), _BumpScale * 0.70);
                half3 normalTS = normalize(n1 + n2);

                // World normal
                float3 bitangentWS = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                float3x3 tbn = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tbn));

                // Coastal ocean blue transition: as the river approaches the sea (southwards, decreasing Z)
                // Inland (Z >= 110): brackish estuarine river
                // Near sea (Z <= -20): deep vibrant Bay of Bengal ocean blue!
                float seaT = saturate((_SeaTransitionZStart - input.positionWS.z) / max(1.0, _SeaTransitionZStart - _SeaTransitionZEnd));
                seaT = smoothstep(0.0, 1.0, seaT);

                half4 activeDeep = lerp(_DeepColor, _OceanBlueColor, seaT);
                half4 activeShallow = lerp(_ShallowColor, _OceanShallowBlueColor, seaT);

                // Reliable shore distance from UV: 0 at shoreline, 1 in deep river channel
                float shoreFactor = saturate((1.0 - abs(input.uv.x - 0.5) * 2.0) * 3.5);
                half4 waterCol = lerp(activeShallow, activeDeep, shoreFactor);

                // Lighting & view direction
                Light mainLight = GetMainLight();
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                
                // Fresnel sky reflection (water reflects the sky at glancing angles)
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, 3.5);
                half3 skyReflect = _SkyReflectionColor.rgb * (mainLight.color * 0.4 + 0.6);

                // Specular sun highlight (sun glittering on river surface)
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specPower = exp2(9.0 * _Smoothness + 2.0);
                float specular = pow(NdotH, specPower) * _Smoothness;

                // Diffuse lighting
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = mainLight.color * (NdotL * 0.5 + 0.5);

                // Compose water color with sky reflection & sun glitter
                half3 baseRGB = waterCol.rgb * diffuse;
                half3 finalRGB = lerp(baseRGB, skyReflect, fresnel * 0.55) + (mainLight.color * specular * 1.3);

                // Subtle edge foam at the immediate shoreline
                float foamFactor = pow(1.0 - shoreFactor, 5.0);
                finalRGB = lerp(finalRGB, _FoamColor.rgb, foamFactor * 0.50);

                // Alpha: shallow water near mud edge is translucent, deep water is opaque
                float finalAlpha = lerp(0.70, waterCol.a, shoreFactor);

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}