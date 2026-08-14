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
        CmdApplyStateToTarget(target.gameObject, States.HotBloodBuff, _buffDuration, Schools.Fire, _owner.gameObject, nameof(HotBloodAura),0);
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.HotBloodBuff);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}

public class HotAuraBuff : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    
    private const float CastSpeedBonusPercent = 0.10f;
    
    private readonly AttributeModifier _castSpeedModifier = new AttributeModifier(CastSpeedBonusPercent, ModifierType.Percent);

    public override States State => States.HotBloodBuff;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _castSpeedModifier.Source = this;

        ApplyCastSpeedBuff();
    }

    private void ApplyCastSpeedBuff()
    {
        if (characterState == null || characterState.Character == null) return;

        var castSpeedAttr = characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeed];

        if (castSpeedAttr != null && !castSpeedAttr.Modifiers.Contains(_castSpeedModifier))
        {
            castSpeedAttr.AddModifier(_castSpeedModifier);
        }
    }

    private void RemoveCastSpeedBuff()
    {
        if (characterState == null || characterState.Character == null) return;

        var castSpeedAttr = characterState.Character.AttributeSystem[CharacterAttributeName.CastSpeed];

        if (castSpeedAttr != null)
        {
            castSpeedAttr.RemoveModifier(_castSpeedModifier);
        }
    }

    public override void ExitState()
    {
        RemoveCastSpeedBuff();
        base.ExitState();
    }

    public override bool Stack(float time) => false;

    public override void UpdateState() { }
}
