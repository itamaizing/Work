using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FireMagicActivationBooster : SkillTalentHandler
{
    private const float SkillCooldown = 6f;

    private readonly NewPunch_Scorpion _punch;
    private readonly Kick_Scorpion _kick;
    private readonly CleavingBlade_Scorpion _cleavingBlade;
    private readonly ChainBlade _chainBlade;
    private readonly SkillManager _skillManager;
    private readonly Character _character;

    private readonly Dictionary<Skill, float> _lastFireTime = new();
    private readonly Dictionary<Skill, System.Action> _fireMagicHandlers = new();

    private bool _isEnabled = false;
    public bool IsEnabled => _isEnabled;

    public FireMagicActivationBooster(
        NetworkBehaviour owner,
        NewPunch_Scorpion punch,
        Kick_Scorpion kick,
        CleavingBlade_Scorpion cleavingBlade,
        ChainBlade chainBlade,
        SkillManager skillManager,
        Character character) : base(owner)
    {
        _punch = punch;
        _kick = kick;
        _cleavingBlade = cleavingBlade;
        _chainBlade = chainBlade;
        _skillManager = skillManager;
        _character = character;
    }

    public override void Enable(bool value)
    {
        _isEnabled = value;
        
        if (value)
        {
            foreach (var skill in _skillManager.Abilities)
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

            if (_character.isClient)
                _character.CharacterState.CmdRemoveState(States.FireCharge);
        }
    }

    private void OnFireMagicCastStarted(Skill skill)
    {
        if (_lastFireTime.TryGetValue(skill, out float last))
            if (Time.time - last < SkillCooldown)
                return;

        _lastFireTime[skill] = Time.time;

        var charState = _character.CharacterState;
        var existing = charState.GetState(States.FireCharge) as FireChargeState;

        if (existing != null)
        {
            return;
        }

        if (_character.isClient)
        {
            charState.CmdAddState(States.FireCharge, -1f, 0f, _character.gameObject,
                nameof(FireMagicActivationBooster));
        }
    }

    private void OnPunchCastStarted()
    {
        var state = _character.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForPunchKick(punch: _punch);
    }

    private void OnKickCastStarted()
    {
        var state = _character.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForPunchKick(kick: _kick);
    }

    private void OnBladeCastStarted()
    {
        var state = _character.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForBlades(blade: _cleavingBlade);
    }
    private void OnChainBladeHit(Character target)
    {
        var state = _character.CharacterState.GetState(States.FireCharge) as FireChargeState;
        if (state == null) return;
        state.ConsumeForBlades(chainBlade: _chainBlade);
    }
}