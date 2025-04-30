Shader "HDRP/SkillIndicator/CircleHDRP"
{
    Properties
    {
        [Header(Base)]
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("MainTex", 2D) = "white" {}
        _Intensity("Intensity", float) = 1

        [Header(Sector)]
        [MaterialToggle] _Sector("Sector", Float) = 1
        _Angle ("Angle", Range(0, 360)) = 60
        _Outline ("Outline", Range(0, 5)) = 0.35
        _OutlineAlpha("Outline Alpha",Range(0,1))=0.5
        [MaterialToggle] _Indicator("Indicator", Float) = 1

        [Header(Flow)]
        _FlowColor("Flow Color",color) = (1,1,1,1)
        _FlowFade("Fade",range(0,1)) = 1
        _Duration("Duration",range(0,1)) = 0

        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend Mode", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend Mode", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderPipeline"="HDRenderPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Forward"
            Blend [_SrcBlend][_DstBlend]
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5 // HDRP는 DX11 이상을 기본으로 사용합니다.
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Intensity;
                float _Angle;
                float _Sector;
                float _Outline;
                float _OutlineAlpha;
                float4 _FlowColor;
                float _FlowFade;
                float _Duration;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS));
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 col = 0;
                float2 uv = input.uv;

                // Main texture sampling and intensity adjustment
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                mainTex *= _Intensity;

                // Indicator mode check
                #ifdef _INDICATOR_ON
                    return mainTex.b * 0.6 * _Color;
                #endif

                // UV transformation for circular mask
                float2 centerUV = uv * 2 - 1;
                float atan2UV = 1 - abs(atan2(centerUV.y, centerUV.x) / 3.14159265);

                // Sector calculation
                float sector = lerp(1.0, 1.0 - ceil(atan2UV - (_Angle * (1.0 / 360.0))), _Sector);
                float sectorBig = lerp(1.0, 1.0 - ceil(atan2UV - ((_Angle + _Outline) * (1.0 / 360.0))), _Sector);
                
                // Outline calculation
                float outline = (sectorBig - sector) * mainTex.g * _OutlineAlpha;

                // Outline visibility check for full circle angles
                float needOutline = 1 - step(359, _Angle);
                outline *= needOutline;

                col += mainTex.r * sector * _Color + outline * _Color;

                // Flow effect calculation
                float flowCircleInner = smoothstep(_Duration - _FlowFade, _Duration, length(centerUV));
                float flowCircleMask = step(length(centerUV), _Duration);
                
                float4 flowEffect = flowCircleInner * flowCircleMask * sector * mainTex.g * _FlowColor;

                col += flowEffect;

                return col;
            }
            ENDHLSL
        }
    }
}
