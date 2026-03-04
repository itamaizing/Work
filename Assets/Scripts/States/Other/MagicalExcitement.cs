using System.Collections.Generic;
using UnityEngine;

public class MagicalExcitement : AbstractCharacterState
{
    private float _duration;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.MagicalExcitement;
    public override StateType Type => StateType.Physical;
    public override List<StatusEffect> Effects => _effects;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        characterState = character;
        personWhoMadeBuff = personWhoMadeBuff;
    }

    public override void ExitState()
    {
        characterState.StateIcons.RemoveItemByState(State);
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        currentStacksCount++;

        _duration = time;

        return true;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0) ExitState();
    }
}
