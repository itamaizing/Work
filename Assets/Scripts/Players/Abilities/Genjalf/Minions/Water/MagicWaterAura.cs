using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class MagicWaterAura : AuraStateHandler
{
    [SerializeField] private float _buffDuration = -1f;

    protected override void OnTargetEnter(Character target)
    {
        CmdApplyStateToTarget(target.gameObject, States.MagicWater, _buffDuration, Schools.Water, _owner.gameObject,
            nameof(MagicWater),0);
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.MagicWater);
    }

    protected override void OnAuraDisabled()
    {
        RemoveEffectsFromAllTargets();
    }
}

public class MagicWater : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();

    private const float ManaMaxPercent = 0.10f;      // +10% к максимальному запасу маны
    private const float ManaRegenPercent = 0.003f;    // 0.3% от максимальной маны идет в регенерацию

    private readonly AttributeModifier _maxManaModifier =
        new AttributeModifier(ManaMaxPercent, ModifierType.Percent);

    private readonly AttributeModifier _manaRegenModifier =
        new AttributeModifier(0f, ModifierType.Flat);

    public override States State => States.MagicWater;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState characterState, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        duration = durationToExit;
        this.characterState = characterState;

        _maxManaModifier.Source = this;
        _manaRegenModifier.Source = this;

        ApplyBuffs();
    }

    private void ApplyBuffs()
    {
        if (characterState == null || characterState.Character == null) return;

        if (characterState.Character.Resources.TryGetValue(ResourceType.Mana, out var mana) && mana != null)
        {
            mana.AddModifier(ResourceAttributeName.MaxValue, _maxManaModifier);

            _manaRegenModifier.Value = ManaRegenPercent * mana.MaxValue;

            mana.AddModifier(ResourceAttributeName.Regen, _manaRegenModifier);
        }
    }

    private void RemoveBuffs()
    {
        if (characterState == null || characterState.Character == null) return;

        if (characterState.Character.Resources.TryGetValue(ResourceType.Mana, out var mana) && mana != null)
        {
            mana.RemoveModifierBySource(ResourceAttributeName.MaxValue, this);
            mana.RemoveModifierBySource(ResourceAttributeName.Regen, this);
        }
    }

    public override void ExitState()
    {
        currentStacksCount = 0;
        RemoveBuffs();
        base.ExitState();
    }

    public override bool Stack(float time) => false;

    public override void UpdateState()
    {
    }
}
