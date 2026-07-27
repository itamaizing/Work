using UnityEngine;
using System;

public static class BleedingComboContext
{
    public static Type LastSkill;
    public static float LastTime;

    private const float Window = 3f;

    public static bool IsRecent => LastSkill != null && Time.time - LastTime <= Window;

    public static void Set(Type skill)
    {
        LastSkill = skill;
        LastTime = Time.time;
    }

    public static void Reset()
    {
        LastSkill = null;
    }
}