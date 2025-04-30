Shader "HDRP/SkillIndicator/ArrowHDRP"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Intensity("Intensity", Float) = 1

        [Header(Flow)]
        _FlowColor("Flow Color", Color) = (1,1,1,1)
        _Duration ("Duration", Range(0,1)) = 0

        [Space]
        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend Mode", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend Mode", Float) = 1
    }

    SubShader
    {
        Tags { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent"
            "RenderPipeline" = "HDRenderPipeline"
            "DisableBatching" = "True" 
        }

        Pass
        {
            Name "Unlit"
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv_mask : TEXCOORD1;
                float2 uv_flow : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Intensity;
                float4 _FlowColor;
                float _Duration;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv_mask = v.uv;
                o.uv_flow = float2(v.uv.x, v.uv.y + (1 - _Duration));
                return o;
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float mask = smoothstep(0, 0.3, i.uv_mask.y);

                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                float4 mainCol = mainTex.r * mask * _Color * _Intensity;

                float4 flowTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv_flow);
                float4 flow = flowTex.g * mainTex.b * mask * _FlowColor;

                return mainCol + flow;
            }
            ENDHLSL
        }
    }
}
