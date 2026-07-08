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

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _duration = durationToExit;
        characterState = character;
        personWhoMadeBuff = personWhoMadeBuff;
    }


    /*public override bool Stack(float time)
    {
        currentStacksCount++;

        _duration = time;

        return true;
    }*/

    public override void OnUpdateState()
    {
    }
}
