using System.Collections.Generic;
using UnityEngine;

public class ShackleState : StateBasic
{
    private float _duration;
    private Character _character;

    public override States State => States.ShackleState;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void Apply(CharacterState characterState, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        this.characterState = characterState;
        _character     = characterState.Character;
        _duration      = durationToExit;

        _character.Move.SetCanMove(false);
    }

    protected override void OnExit()
    {
        _character.Move.SetCanMove(true);
    }
}
