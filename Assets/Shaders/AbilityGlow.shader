Shader "UI/Ability Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _Color ("Glow Color", Color) = (1, 0.72, 0.16, 1)
        _LineWidth ("Line Width", Range(0.005, 0.12)) = 0.035
        _GlowWidth ("Glow Width", Range(0.01, 0.24)) = 0.09
        _CornerRadius ("Corner Radius", Range(0, 0.35)) = 0.10
        _PulseFrequency ("Pulse Frequency (Hz)", Range(0, 6)) = 1.15
        _PulseDepth ("Pulse Depth", Range(0, 1)) = 0.55

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma target 2.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                #if defined(UNITY_UI_CLIP_RECT)
                float4 mask : TEXCOORD1;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _Color;
            float _LineWidth;
            float _GlowWidth;
            float _CornerRadius;
            float _PulseFrequency;
            half _PulseDepth;
            float4 _ClipRect;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;

                #if !defined(UNITY_COLORSPACE_GAMMA)
                if (_UIVertexColorAlwaysGammaSpace)
                    input.color.rgb = GammaToLinearSpace(input.color.rgb);
                #endif

                half pulse = 0.5h + 0.5h * cos(_Time.y * _PulseFrequency * 6.2831853);
                output.color = input.color * _Color;
                output.color.a *= lerp(1.0h - _PulseDepth, 1.0h, pulse);

                #if defined(UNITY_UI_CLIP_RECT)
                float2 pixelSize = output.position.w / abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
                float4 rect = clamp(_ClipRect, -2e10, 2e10);
                float2 softness = float2(_UIMaskSoftnessX, _UIMaskSoftnessY);
                output.mask = float4(input.vertex.xy * 2.0 - rect.xy - rect.zw,
                    0.25 / (0.25 * softness + abs(pixelSize)));
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float halfLine = _LineWidth * 0.5;
                float halfExtent = 0.5 - _GlowWidth - halfLine;
                float radius = min(_CornerRadius, halfExtent);
                float2 corner = abs(input.uv - 0.5) - (halfExtent - radius);
                float distanceToEdge = length(max(corner, 0.0)) + min(max(corner.x, corner.y), 0.0) - radius;

                float strokeDistance = abs(distanceToEdge) - halfLine;
                float aa = max(0.5 * fwidth(distanceToEdge), 0.0001);
                half stroke = 1.0h - smoothstep(-aa, aa, strokeDistance);
                half halo = saturate(1.0 - strokeDistance / _GlowWidth);
                half alpha = max(stroke, 0.65h * halo * halo) * input.color.a;

                #if defined(UNITY_UI_CLIP_RECT)
                float2 clipMask = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
                alpha *= clipMask.x * clipMask.y;
                #endif

                #if defined(UNITY_UI_ALPHACLIP)
                clip(alpha - 0.001h);
                #endif

                half3 color = lerp(input.color.rgb, 1.0h, 0.4h * stroke);
                return half4(color * alpha, alpha);
            }
            ENDCG
        }
    }
}
