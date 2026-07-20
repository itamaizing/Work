using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ImmaterialityState : AbstractCharacterState
{
    private int _defualtPlayerLayer;
    private int _newPlayerLayer;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Immateriality };
    public override States State => States.Immateriality;
    public override StateType Type => StateType.Immaterial;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _defualtPlayerLayer = characterState.gameObject.layer;

        _newPlayerLayer = LayerMask.NameToLayer("LightningMovement");

        DisabledCollider();
    }

    public override void OnUpdateState()
    {

    }

    protected override void OnExitState()
    {
        TargetRpcResetPlayerComponents();
        duration = 0;
        characterState.RemoveStateFromList(this);
    }

    /*public override bool Stack(float time)
    {
        return false;
    }*/

    private void DisabledCollider()
    {
        if (characterState.Character != null)
        {
            TargetRpcDisbledCollider();
        }
    }


    [ClientRpc]
    private void TargetRpcDisbledCollider()
    {
        if (characterState.Character != null)
        {
            characterState.Character.gameObject.layer = _newPlayerLayer;
        }
    }

    [ClientRpc]
    private void TargetRpcResetPlayerComponents()
    {
        characterState.Character.gameObject.layer = _defualtPlayerLayer;
    }
}