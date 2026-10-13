using UnityEngine;

public static class ChatGlobalStyle
{
    private const string PalettePath = "ColorPalette";

    private static ColorPalette _colors;

    public static ColorPalette Colors
    {
        get
        {
            if (_colors == null)
            {
                _colors = Resources.Load<ColorPalette>(PalettePath);

                if (_colors == null)
                {
                    Debug.LogWarning($"[ChatGlobalStyle]  Resources/{PalettePath}.asset не найден, используются цвета по умолчанию");
                    _colors = ScriptableObject.CreateInstance<ColorPalette>();
                }
            }
            return _colors;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _colors = null;
}