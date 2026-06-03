using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class DiminishingReturnsTracker : NetworkBehaviour
{
    private const float WindowDuration = 9f;

    private static readonly float[] Multipliers = { 1f, 0.5f, 0.25f, 0f };

    private class GroupState
    {
        public int ApplicationCount;
        public float Timer;
        public bool TimerRunning;
        public bool IsImmune => ApplicationCount >= Multipliers.Length - 1;
    }

    private readonly Dictionary<DiminishingReturnGroup, GroupState> _groups = new();

    private GroupState GetOrCreate(DiminishingReturnGroup group)
    {
        if (!_groups.TryGetValue(group, out var g))
        {
            g = new GroupState();
            _groups[group] = g;
        }
        return g;
    }

    public float GetModifiedDuration(DiminishingReturnGroup group, float baseDuration)
    {
        if (group == DiminishingReturnGroup.None) return baseDuration;
        var g = GetOrCreate(group);
        if (g.IsImmune) return 0f;
        return baseDuration * Multipliers[g.ApplicationCount];
    }

    public void ConsumeApplication(DiminishingReturnGroup group)
    {
        if (group == DiminishingReturnGroup.None) return;
        var g = GetOrCreate(group);
        if (!g.IsImmune)
            g.ApplicationCount = Mathf.Min(g.ApplicationCount + 1, Multipliers.Length - 1);
    }

    public void OnEffectEnded(DiminishingReturnGroup group)
    {
        if (group == DiminishingReturnGroup.None) return;
        var g = GetOrCreate(group);
        g.TimerRunning = true;
        g.Timer = WindowDuration;
    }

    private void Update()
    {
        foreach (var kv in _groups)
        {
            var g = kv.Value;
            if (!g.TimerRunning) continue;

            g.Timer -= Time.deltaTime;
            if (g.Timer <= 0f)
            {
                g.ApplicationCount = 0;
                g.Timer = 0f;
                g.TimerRunning = false;
            }
        }
    }
}
