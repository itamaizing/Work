Shader "Custom/AlwaysOnTopLine"
{
    SubShader
    {
        Tags { "Queue"="Overlay" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Lighting Off
        }
    }
}
