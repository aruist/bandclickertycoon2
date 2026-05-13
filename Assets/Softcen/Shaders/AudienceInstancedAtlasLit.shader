Shader "Softcen/Audience Instanced Atlas Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1, 1, 1, 1)
        _AudienceAtlasST("Atlas ST", Vector) = (1, 1, 0, 0)
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AudienceAtlasST)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 atlasST = UNITY_ACCESS_INSTANCED_PROP(Props, _AudienceAtlasST);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normInputs.normalWS);
                output.uv = input.uv * atlasST.xy + atlasST.zw;
                output.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normalWS = normalize(input.normalWS);

                half3 lighting = SampleSH(normalWS);

                Light mainLight = GetMainLight(input.shadowCoord);
                half mainNdotL = saturate(dot(normalWS, mainLight.direction));
                lighting += mainLight.color * (mainNdotL * mainLight.distanceAttenuation * mainLight.shadowAttenuation);

                #ifdef _ADDITIONAL_LIGHTS
                uint additionalLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < additionalLightCount; lightIndex++)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWS);
                    half ndotl = saturate(dot(normalWS, light.direction));
                    lighting += light.color * (ndotl * light.distanceAttenuation * light.shadowAttenuation);
                }
                #endif

                half3 finalRGB = texColor.rgb * lighting;

                return half4(finalRGB, texColor.a);
            }
            ENDHLSL
        }
    }
}
