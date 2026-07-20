using System.Collections.Generic;
using UnityEngine;

public class BaffState : StackableState
{
    private float _durationRemaining;
    private string _skillName;

    public override States State => States.BaffState;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;

    public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

    public BaffState()
    {
        MaxStacksCount = 20;
        currentStacksCount = 1;
    }

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _durationRemaining = durationToExit;
        _skillName = skillName;
    }

    public override void OnUpdateState()
    {
        if (_durationRemaining <= 0f)
        {
            OnExitState();
            return;
        }

        _durationRemaining -= Time.deltaTime;
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacksCount)
        {
            currentStacksCount++;
            _durationRemaining = time;

            return true;
        }

        return false;
    }
}
