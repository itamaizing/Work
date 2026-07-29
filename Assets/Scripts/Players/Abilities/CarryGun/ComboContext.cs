using System;
using UnityEngine;

public static class ComboContext
{
    private const float DefaultWindow = 3f;

    #region Bleeding Combo
    public static class Bleeding
    {
        public static Type LastSkill { get; private set; }
        public static float LastTime { get; private set; }

        public static bool IsRecent => LastSkill != null && Time.time - LastTime <= DefaultWindow;

        public static void Set(Type skill)
        {
            LastSkill = skill;
            LastTime = Time.time;
        }

        public static void Reset()
        {
            LastSkill = null;
            LastTime = 0f;
        }
    }
    #endregion

    #region Jump Back Combo
    public static class JumpBack
    {
        public static Character LastTarget { get; set; }
        public static Type LastSkill { get; set; }
        public static float LastTime { get; set; }

        public static void Reset()
        {
            LastTarget = null;
            LastSkill = null;
            LastTime = 0f;
        }
    }
    #endregion

    #region Claw Strike Combo
    public static class ClawStrikeContext
    {
        public static Type LastSkill { get; private set; }
        public static float LastTime { get; private set; }
        public static bool LastWasBoosted { get; private set; }

        public static void Set(Type skill, bool wasBoosted = false)
        {
            LastSkill = skill;
            LastTime = Time.time;
            LastWasBoosted = wasBoosted;
        }

        public static void Reset()
        {
            LastSkill = null;
            LastTime = 0f;
            LastWasBoosted = false;
        }

        public static bool IsValidPreviousSkill()
        {
            if (Time.time - LastTime > DefaultWindow)
                return false;

            if (LastSkill == typeof(ClawStrike) && LastWasBoosted)
                return false;

            return LastSkill == typeof(CheliceraStrike)
                   || LastSkill == typeof(ClawStrike)
                   || LastSkill == typeof(JumpWithChelicera)
                   || LastSkill == typeof(DoubleCheliceraStrike);
        }
    }
    #endregion
}