using UnityEngine;

internal static class PsionicBarScale
{
    public const float FineWidth = 0.3f;

    public static float GetFill(float value, float maximum, float threshold, float fineWidth)
    {
        if (maximum <= 0f) return 0f;

        value = Mathf.Clamp(value, 0f, maximum);
        if (threshold <= 0f || threshold >= maximum)
            return value / maximum;

        if (value <= threshold)
            return value / threshold * fineWidth;

        return fineWidth + (value - threshold) / (maximum - threshold) * (1f - fineWidth);
    }
}
