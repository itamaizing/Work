using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class InnerDarkness : AbstractCharacterState
{
    private const float TimeDecreasePerStack = 2f;

    private float _baseDuration;
    private float _changedDuration;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override States State => States.InnerDarkness;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _personWhoMadeBuff = personWhoMadeBuff;
        MaxStacksCount = 6;
        _baseDuration = durationToExit;
        duration = _baseDuration;
        CurrentStacksCount = 0;
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;
        if (duration <= 0) ExitState();
    }

    public override void ExitState()
    {
        _characterState.StateIcons.RemoveItemByState(State);
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            AddNewStack();
            return true;
        }

        else if (CurrentStacksCount == MaxStacksCount)
        {
            UpdateDurationForMaxStacks();
            Debug.Log($"duration: {duration}");
            return false;
        }

        return false;
    }

    private void AddNewStack()
    {
        //CurrentStacksCount++;
        duration = Mathf.Max(0f, _baseDuration - (CurrentStacksCount - 1) * TimeDecreasePerStack);
    }

    private void UpdateDurationForMaxStacks()
    {
        duration = Mathf.Max(0f, _baseDuration - (CurrentStacksCount - 1) * TimeDecreasePerStack);
        CmdStateFear();
    }

    [Command] private void CmdStateFear() => ClientRpcStateFear();
    [ClientRpc] private void ClientRpcStateFear() { _characterState.AddStateLogic(States.Fear, Random.Range(0.7f, 1.4f), 0f, Schools.None, _personWhoMadeBuff.gameObject, null); }
}
