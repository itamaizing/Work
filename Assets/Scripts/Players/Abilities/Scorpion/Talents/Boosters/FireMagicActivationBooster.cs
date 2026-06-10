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

    private readonly Dictionary<Skill, float> _lastFireTime = new();
    private readonly Dictionary<Skill, System.Action> _fireMagicHandlers = new();

    private bool _isEnabled = false;

    public void Enable(bool value)
    {
        if(_isEnabled == value) return;
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
                if (skill is NewPunch_Scorpion || skill is Kick_Scorpion || skill is CleavingBlade_Scorpion) continue;

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
        if (_lastFireTime.TryGetValue(skill, out float last))
            if (Time.time - last < SkillCooldown)
                return;

        _lastFireTime[skill] = Time.time;

        var charState = _hero.CharacterState;
        var existing = charState.GetState(States.FireCharge) as FireChargeState;

        if (existing != null)
        {
            return;
        }

        if (_hero.isClient)
        {
            _hero.CharacterState.CmdAddState(States.FireCharge, float.PositiveInfinity, 0f, Schools.Fire, _hero.gameObject,
                null);
        }
    }

    private void OnPunchCastStarted()
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForPunchKick(punch: _punch);
    }

    private void OnKickCastStarted()
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForPunchKick(kick: _kick);
    }

    private void OnBladeCastStarted()
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForBlades(blade: _cleavingBlade);
    }
    private void OnChainBladeHit(Character target)
    {
        var state = _hero.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForBlades(chainBlade: _chainBlade);
    }

    protected override IEnumerator CastJob()
    {
        throw new System.NotImplementedException();
    }

    protected override int AnimTriggerCastDelay { get; }
    protected override int AnimTriggerCast { get; }
}