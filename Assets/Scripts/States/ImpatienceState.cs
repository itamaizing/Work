using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class ImpatienceState : AbstractCharacterState
{
    private const float TimeDecreasePerStack = 2f;
    private float _durationRemaining;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Ability };

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.InnerDarkness;
    public override StateType Type => StateType.Aura;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _durationRemaining = durationToExit;
    }

    public override void UpdateState()
    {
        _durationRemaining -= Time.deltaTime;
        if (_durationRemaining <= 0) ExitState();
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
        currentStacksCount = 1;
    }

    public override bool Stack(float time)
    {
        Debug.Log($"CurrentStacksCount: {currentStacksCount}");

        if (currentStacksCount < MaxStacksCount)
        {
            AddNewStack(time);
            return true;
        }

        else if (currentStacksCount == MaxStacksCount)
        {
            UpdateDurationForMaxStacks(time);
            return false;
        }

        return false;
    }

    private void AddNewStack(float time)
    {
        currentStacksCount++;

        _durationRemaining = time - (currentStacksCount - 1) * TimeDecreasePerStack;
    }

    private void UpdateDurationForMaxStacks(float time)
    {
        _durationRemaining = time - (currentStacksCount - 1) * TimeDecreasePerStack;
        Debug.Log("обновление при максимальном стаке");
    }
}
