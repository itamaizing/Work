using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FireMagicActivationBooster : Skill, IPassiveSkill
{
    private const float SkillCooldown = 6f;

    private NewPunch_Scorpion _punch;
    private Kick_Scorpion _kick;
    private CleavingBlade_Scorpion _cleavingBlade;
    private ChainBlade _chainBlade;

    private readonly Dictionary<Skill, System.Action> _fireMagicHandlers = new();

    private float _chargeConsumedAt = float.NegativeInfinity;
    private bool  _isEnabled;

    public void Enable(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;

        _punch = _hero.Abilities.GetSkill<NewPunch_Scorpion>();
        _kick = _hero.Abilities.GetSkill<Kick_Scorpion>();
        _cleavingBlade = _hero.Abilities.GetSkill<CleavingBlade_Scorpion>();
        _chainBlade = _hero.Abilities.GetSkill<ChainBlade>();

        if (value)
        {
            foreach (var skill in _hero.Abilities.Abilities)
            {
                if (skill.Info.School != Schools.Fire) continue;
                if (skill is NewPunch_Scorpion or Kick_Scorpion or CleavingBlade_Scorpion) continue;

                var captured = skill;
                System.Action handler = () => OnFireMagicCastStarted(captured);
                _fireMagicHandlers[captured] = handler;
                captured.CastStarted += handler;
            }

            _punch.CastStarted += OnPunchCastStarted;
            _kick.CastStarted += OnKickCastStarted;
            _cleavingBlade.CastStarted += OnBladeCastStarted;
            _chainBlade.OnArrowHit += OnChainBladeHit;
        }
        else
        {
            foreach (var kvp in _fireMagicHandlers)
                kvp.Key.CastStarted -= kvp.Value;
            _fireMagicHandlers.Clear();

            _punch.CastStarted -= OnPunchCastStarted;
            _kick.CastStarted -= OnKickCastStarted;
            _cleavingBlade.CastStarted -= OnBladeCastStarted;
            _chainBlade.OnArrowHit -= OnChainBladeHit;

            if (_hero.isClient)
                _hero.CharacterState.CmdRemoveState(States.FireCharge);
        }
    }

    private void OnFireMagicCastStarted(Skill skill)
    {
        if (Time.time - _chargeConsumedAt < SkillCooldown) return;
        
        if (_hero.CharacterState.GetState(States.FireCharge) != null) return;

        if (_hero.isClient)
            _hero.CharacterState.CmdAddState(
                States.FireCharge, float.PositiveInfinity, 0f,
                Schools.Fire, _hero.gameObject, null);
    }

    private void OnPunchCastStarted()
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;

        state.ConsumeForPunchKick(punch: _punch);
        RecordConsumption();
    }

    private void OnKickCastStarted()
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;

        state.ConsumeForPunchKick(kick: _kick);
        RecordConsumption();
    }

    private void OnBladeCastStarted()
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;

        state.ConsumeForBlades(blade: _cleavingBlade);
        RecordConsumption();
    }

    private void OnChainBladeHit(Character target)
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;

        state.ConsumeForBlades(chainBlade: _chainBlade);
        RecordConsumption();
    }
    
    private void RecordConsumption() => _chargeConsumedAt = Time.time;

    protected override IEnumerator CastJob()
    {
        throw new System.NotImplementedException();
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
}