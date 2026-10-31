using UnityEngine;

public enum CursorState
{
    Default,
    Targeting,
    TargetingHighlight,
    ResizeHorizontal,
    ResizeVertical,
    ResizeCorner_LB_RT, // Диагональный "/"
    ResizeCorner_RB_LT, // Диагональный "\"
}

public static class CursorService
{
    public static CursorTheme_SO CurrentTheme;
    public static CursorTheme_SO DefaultTheme;

    private static CursorTheme_SO ActiveTheme => CurrentTheme != null ? CurrentTheme : DefaultTheme;

    public static void SetState(CursorState state)
    {
        SetCursor(ActiveTheme.Cursors[state]);
    }

    private static void SetCursor(CursorParameters parameters)
    {
        if (parameters == null || parameters.Image == null)
            return;

        Cursor.SetCursor(parameters.Image, parameters.HotSpot, CursorMode.Auto);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void FindDefaultTheme()
    {
        var defaultTheme = Resources.Load<CursorTheme_SO>("DefaultCursors");
        if (defaultTheme != null)
            DefaultTheme = defaultTheme;
        Debug.Log($"Loaded Cursors: {DefaultTheme}");
    }

    private static void SetTheme(CursorTheme_SO theme)
    {
        CurrentTheme = theme;
    }
}
