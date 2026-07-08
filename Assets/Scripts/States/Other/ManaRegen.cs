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

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _manaRegen = characterState.StateEffects.ManaRegen;

        if (_manaRegen) _manaRegen.SetActive(true);
    }

    protected override void OnExitState()
    {
        if (_manaRegen) _manaRegen.SetActive(false);

        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveStateFromList(this);
    }

    /*public override bool Stack(float time)
    {
        return false;
    }*/

    public override void OnUpdateState()
    {
    }
}
