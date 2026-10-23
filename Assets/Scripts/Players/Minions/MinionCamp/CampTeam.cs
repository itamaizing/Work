using System;
using System.Collections;
using Mirror;
using UnityEngine;

//константы команд (1, 2, нейтральная 3) + поиск локального героя и его команды.
public static class CampTeam
{
    public const int None = -1;
    public const int DarkTeamIndex = 1;
    public const int LightTeamIndex = 2;
    public const int Neutral = 3;

    public static bool TryGetLocalTeam(out int team)
    {
        team = 0;
        var local = Character.Local;
        if (local == null) return false;

        team = local.NetworkSettings.TeamIndex;
        return team > 0;
    }

    public static int LayerFor(int ownerTeam, int localTeam)
    {
        bool ally = ownerTeam != Neutral && ownerTeam == localTeam;
        return LayerMask.NameToLayer(ally ? "Allies" : "Enemy");
    }
    
    public static void WhenLocalPlayerReady(MonoBehaviour host, Action onReady)
    {
        if (TryGetLocalTeam(out _)) { onReady(); return; }
        host.StartCoroutine(Wait(onReady));
    }

    private static IEnumerator Wait(Action onReady)
    {
        while (!TryGetLocalTeam(out _)) yield return null;
        onReady();
    }
}
