using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class HotBloodAura : AuraStateHandler
{
    [SerializeField] private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        target.CharacterState.CmdAddState(States.HotBloodBuff, _buffDuration, 0,Schools.Fire, _owner.gameObject, nameof(HotBloodAura));
    }

    protected override void OnTargetExit(Character target)
    {
        target.CharacterState.CmdRemoveState(States.HotBloodBuff);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}

public class HotAuraBuff : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    private float _percentage = 0.1f;
    private Character _character;

    public override States State => States.HotBloodBuff;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _character = character.Character;
        foreach (var skill in character.Character.Abilities.Abilities)
        {
            skill.Buff.CastSpeed.IncreasePercentage(1 - _percentage);
            skill.Buff.AttackSpeed.IncreasePercentage(1 - _percentage);
        }
    }

    public override void ExitState()
    {
        base.ExitState();
        if (_character != null)
        {
            foreach (var skill in _character.Abilities.Abilities)
            {
                skill.Buff.CastSpeed.Reset();
                skill.Buff.AttackSpeed.Reset();
            }
        }
    }

    public override bool Stack(float time)
    {
        return false;
    }

    public override void UpdateState()
    {
    }
}
