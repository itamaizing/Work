using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperPoisonAura : NetworkBehaviour
{
    private Dictionary<Character, float> _evadeBonusFromTargets = new();
    private Health _health;
    private Character _owner;

    #region Talent

    private bool _isFeelingPoisoning = false;
    private bool _isEvadePoison = false;

    public bool IsFeelingPoisoning { get => _isFeelingPoisoning; set => _isFeelingPoisoning = value; }

    public void FeelingPoisoning(bool value) => _isFeelingPoisoning = value;
    public void EvadePoison(bool value) => _isEvadePoison = value;
    #endregion

    public override void OnStartServer()
    {
        base.OnStartServer();

        _owner = GetComponent<Character>();
        _health = GetComponent<Health>();

        if (_health != null)
        {
            _health.OnBeforeTakeDamage += OnBeforeTakeDamage;
        }
    }

    public override void OnStopServer()
    {
        if (_health != null)
        {
            _health.OnBeforeTakeDamage -= OnBeforeTakeDamage;
        }
    }

    private void OnBeforeTakeDamage(Damage damage, Skill skill)
    {
        if (!_isEvadePoison) return;
        if (skill == null || skill.Hero == null) return;

        Character attacker = skill.Hero;

        if (attacker == null || attacker == _owner) return;

        if (!HasPoison(attacker)) return;

        if (!_evadeBonusFromTargets.ContainsKey(attacker))
        {
            _evadeBonusFromTargets[attacker] = 5f;
        }
    }

    private bool HasPoison(Character character)
    {
        var states = character.CharacterState.CurrentStates;

        foreach (var state in states)
        {
            if (state.Effects.Contains(StatusEffect.Poison))  return true;
        }

        return false;
    }
}
