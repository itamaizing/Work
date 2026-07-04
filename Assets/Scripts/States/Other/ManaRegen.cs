using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaRegen : AbstractCharacterState
{
    private GameObject _manaRegen;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.ManaRegen;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _manaRegen = characterState.StateEffects.ManaRegen;

        if (_manaRegen) _manaRegen.SetActive(true);
    }

    public override void ExitState()
    {
        if (_manaRegen) _manaRegen.SetActive(false);

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        return false;
    }

    public override void UpdateState()
    {
    }
}
