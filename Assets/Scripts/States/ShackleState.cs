using System.Collections.Generic;
using UnityEngine;

public class ShackleState : StackableState
{
    private float _duration;
    private Character _character;

    public override States State => States.ShackleState;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    protected override void OnEnterState(CharacterState characterState, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        this.characterState = characterState;
        _character     = characterState.Character;
        _duration      = durationToExit;
        MaxStacksCount = 1;

        _character.Move.SetCanMove(false);
    }

    public override void OnUpdateState()
    {
    }

    protected override void OnExitState()
    {
        _character.Move.SetCanMove(true);
    }

    public override bool Stack(float time) => false;
}
