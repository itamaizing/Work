using System;
using System.Collections.Generic;

public static class CampRegistry
{
    private static readonly List<MinionCamp> s_camps = new();

    public static IReadOnlyList<MinionCamp> Camps => s_camps;
    
    public static bool ManagerPresent { get; set; }

    public static event Action<MinionCamp> Registered;
    public static event Action<MinionCamp> Unregistered;

    public static void Register(MinionCamp camp)
    {
        if (camp == null || s_camps.Contains(camp)) return;
        s_camps.Add(camp);
        Registered?.Invoke(camp);
    }

    public static void Unregister(MinionCamp camp)
    {
        if (!s_camps.Remove(camp)) return;
        Unregistered?.Invoke(camp);
    }
}
