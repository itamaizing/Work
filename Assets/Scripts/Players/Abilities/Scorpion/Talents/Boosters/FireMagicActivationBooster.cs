using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FireMagicActivationBooster : Skill, IPassiveSkill
{
    [SerializeField] private float SkillCooldown = 6f;

    private readonly Dictionary<Skill, System.Action> _fireMagicHandlers = new();
    private readonly Dictionary<Skill, System.Action> _consumeHandlers = new();
    private readonly Dictionary<Skill, float> _cooldowns = new();

    private float _chargeConsumedAt = float.NegativeInfinity;
    private bool  _isEnabled;

    public void Enable(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;

        if (value)
        {
            foreach (var skill in _hero.Abilities.Abilities)
            {
                if (skill is NewPunch_Scorpion or Kick_Scorpion or CleavingBlade_Scorpion or ChainBlade)
                {
                    System.Action handler = () => OnConsumeSkillStarted(skill);
                    _consumeHandlers[skill] = handler;
                    skill.CastStarted += handler;
                }
                else if (skill.Info.School == Schools.Fire)
                {
                    System.Action handler = () => OnFireMagicCastStarted(skill);
                    _fireMagicHandlers[skill] = handler;
                    skill.CastSuccess += handler;
                }
            }
        }
        else
        {
            foreach (var kvp in _fireMagicHandlers)
                kvp.Key.CastSuccess -= kvp.Value;
            _fireMagicHandlers.Clear();

            foreach (var kvp in _consumeHandlers)
                kvp.Key.CastStarted -= kvp.Value;
            _consumeHandlers.Clear();

            if (_hero.isClient)
                _hero.CharacterState.CmdRemoveState(States.FireCharge);
        }
    }

    private void OnFireMagicCastStarted(Skill skill)
    {
        if (_hero.CharacterState.GetState(States.FireCharge) != null) return;
        if (_cooldowns.TryGetValue(skill, out var cooldown) && Time.time < cooldown) return;

        if (_hero.isClient)
            _hero.CharacterState.CmdAddState(
                States.FireCharge, float.PositiveInfinity, 0f,
                Schools.Fire, _hero.gameObject, null);
        _cooldowns[skill] = Time.time + SkillCooldown;
    }

    private void OnConsumeSkillStarted(Skill skill)
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;

        switch (skill)
        {
            case NewPunch_Scorpion punch:
                state.ConsumeForPunchKick(punch: punch);
                break;
            case Kick_Scorpion kick:
                state.ConsumeForPunchKick(kick: kick);
                break;
            case CleavingBlade_Scorpion _cleavingBlade:
                state.ConsumeForBlades(blade: _cleavingBlade);
                break;
            case ChainBlade _chainBlade:
                state.ConsumeForBlades(chainBlade: _chainBlade);
                break;
        }
    }
    
    protected override IEnumerator CastJob()
    {
        throw new System.NotImplementedException();
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
}