Shader "Custom/URP/MucusDecal"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Texture (RGB) Alpha (A)", 2D) = "white" {}
        [MainColor] _BaseColor("Mucus Color", Color) = (0.1, 0.8, 0.2, 0.8)
        _EdgeColor("Edge Glow Color", Color) = (0.4, 1.0, 0.1, 1.0)
        
        _Radius("Current Radius", Float) = 0.0
        _EdgeSoftness("Edge Softness", Range(0.01, 1.0)) = 0.2
        _NoiseScale("Organic Distortion Scale", Float) = 10.0
        _NoiseSpeed("Noise Pulse Speed", Float) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _EdgeColor;
                float _EdgeSoftness;
                float _NoiseScale;
                float _NoiseSpeed;
            CBUFFER_END

            float _Radius;

            float SimpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float SmoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = SimpleNoise(i);
                float b = SimpleNoise(i + float2(1.0, 0.0));
                float c = SimpleNoise(i + float2(0.0, 1.0));
                float d = SimpleNoise(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
    
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap); 
    
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 objectCenter = GetAbsolutePositionWS(TransformObjectToWorld(float3(0,0,0)));
                float distFromCenter = distance(input.worldPos.xz, objectCenter.xz);

                float time = _Time.y * _NoiseSpeed;
                float noise = SmoothNoise(input.uv * _NoiseScale + float2(time, time)) * 0.3;

                float distortedDist = distFromCenter + noise;

                float mask = 1.0 - smoothstep(_Radius - _EdgeSoftness, _Radius, distortedDist);

                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 finalColor = texColor * _BaseColor;

                float edgeMask = smoothstep(_Radius - _EdgeSoftness * 2.0, _Radius, distortedDist) * mask;
                finalColor.rgb = lerp(finalColor.rgb, _EdgeColor.rgb, edgeMask);

                finalColor.a *= mask;

                clip(finalColor.a - 0.01);

                return finalColor;
            }
            ENDHLSL
        }
    }
}