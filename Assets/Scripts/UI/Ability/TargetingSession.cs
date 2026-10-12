using System.Collections.Generic;
using UnityEngine;

public static class TargetingSession
{
    private static readonly List<Skill> _active = new();
    private static object _key;

    public static void Begin(Skill skill, object key)
    {
        key ??= skill;
        _active.RemoveAll(s => s == null);

        if (!Equals(_key, key))
        {
            foreach (var other in _active.ToArray())
                if (other != skill) other.CancelTargetingForNewSession();
            _active.Clear();
            _key = key;
        }

        if (!_active.Contains(skill)) _active.Add(skill);
    }

    public static void End(Skill skill)
    {
        _active.Remove(skill);
        if (_active.Count == 0) _key = null;
    }
}