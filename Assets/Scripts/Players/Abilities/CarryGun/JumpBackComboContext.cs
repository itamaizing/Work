using System;

public static class JumpBackComboContext
{
    public static Character LastTarget;
    public static Type LastSkill;
    public static float LastTime;

    public static void Reset()
    {
        LastTarget = null;
        LastSkill = null;
        LastTime = 0f;
    }
}