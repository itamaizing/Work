using System;
using System.Collections.Generic;
using UnityEngine;

//Cписок участников, участвующих в захвате лагеря (кто бил, кто саппортил).
public sealed class CampAttackers
{
    private sealed class Entry
    {
        public float LastSeen;
        public Action<float, Skill, string> OnHeal;
        public Action<StateBasic> OnBuff;
    }

    private readonly Dictionary<Character, Entry> _entries = new();
    private readonly List<Character> _expired = new();
    private float _timeout = 5f;
    private bool _frozen;

    public int Count => _entries.Count;

    public void Configure(float timeoutSeconds) => _timeout = timeoutSeconds;

    public void Freeze(bool frozen)
    {
        _frozen = frozen;
        if (!frozen)
        {
            float now = Time.time;
            foreach (var entry in _entries.Values) entry.LastSeen = now;
        }
    }

    public void Add(Character hero)
    {
        if (hero is null || hero == null) return;

        if (_entries.TryGetValue(hero, out var existing))
        {
            existing.LastSeen = Time.time;
            return;
        }

        var entry = new Entry { LastSeen = Time.time };
        entry.OnHeal = (amount, skill, source) =>
        {
            if (skill != null && skill.Hero != null) Add(skill.Hero);
        };
        entry.OnBuff = state =>
        {
            var caster = state?.SourceCaster;
            if (caster != null && caster.TryGetComponent(out Character source)) Add(source);
        };

        _entries[hero] = entry;
        hero.Health.HealTakedServer += entry.OnHeal;
        hero.CharacterState.OnStateAdded += entry.OnBuff;
    }

    public void Remove(Character hero)
    {
        if (hero is null || !_entries.TryGetValue(hero, out var entry)) return;

        if (hero != null)
        {
            hero.Health.HealTakedServer -= entry.OnHeal;
            hero.CharacterState.OnStateAdded -= entry.OnBuff;
        }
        _entries.Remove(hero);
    }

    public void Clear()
    {
        _expired.Clear();
        _expired.AddRange(_entries.Keys);
        foreach (var hero in _expired) Remove(hero);
        _expired.Clear();
        _frozen = false;
    }

    public void Tick(float now)
    {
        if (_frozen || _entries.Count == 0) return;

        _expired.Clear();
        foreach (var pair in _entries)
        {
            if (pair.Key == null || now - pair.Value.LastSeen > _timeout)
                _expired.Add(pair.Key);
        }
        foreach (var hero in _expired) Remove(hero);
        _expired.Clear();
    }

    public void CopyTo(List<Character> buffer)
    {
        buffer.Clear();
        foreach (var hero in _entries.Keys)
            if (hero != null) buffer.Add(hero);
    }

    public Character FindByConnection(Mirror.NetworkConnectionToClient conn)
    {
        if (conn == null) return null;
        foreach (var hero in _entries.Keys)
        {
            if (hero != null && hero.netIdentity != null && hero.netIdentity.connectionToClient == conn)
                return hero;
        }
        return null;
    }
}
