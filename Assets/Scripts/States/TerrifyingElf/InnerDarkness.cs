using System.Collections.Generic;
using UnityEngine;

public class InnerDarkness : AbstractCharacterState
{
    private const float TimeDecreasePerStack = 2f;

    private float _duration;

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
        _duration = durationToExit;

        Debug.Log($"CurrentStacksCount: {CurrentStacksCount}");
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0) ExitState();
    }

    public override void ExitState()
    {
        CurrentStacksCount = 0;
        _characterState.StateIcons.RemoveItemByState(State);
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount <= 0)
        {
            InitializeFirstStack();
            return true;
        }

        if (CurrentStacksCount < MaxStacksCount)
        {
            AddNewStack();
            return true;
        }

        if (CurrentStacksCount == MaxStacksCount)
        {
            UpdateDurationForMaxStacks();
            return false;
        }

        return false;
    }

    private void InitializeFirstStack()
    {
        CurrentStacksCount++;
    }

    private void AddNewStack()
    {
        CurrentStacksCount++;
        _duration = _duration - CurrentStacksCount * TimeDecreasePerStack;
    }

    private void UpdateDurationForMaxStacks()
    {
        _duration = CurrentStacksCount * TimeDecreasePerStack;

        _characterState.CmdAddState(States.Fear, Random.Range(0.7f, 1.4f), 0, _personWhoMadeBuff.gameObject, null);
    }
}
