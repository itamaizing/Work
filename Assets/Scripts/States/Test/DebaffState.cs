using System.Collections.Generic;
using UnityEngine;


public class DebaffState : AbstractCharacterState
{
    private float _durationRemaining;
    private string _skillName;

    public override States State => States.DebaffState;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => new List<StatusEffect>() { StatusEffect.Strengthening };

    public DebaffState()
    {
        MaxStacksCount = 20;
        currentStacksCount = 1;
    }

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        base.personWhoMadeBuff = personWhoMadeBuff;
        _durationRemaining = durationToExit;
        _skillName = skillName;
    }

    public override void UpdateState()
    {
        if (_durationRemaining <= 0f)
        {
            ExitState();
            return;
        }

        _durationRemaining -= Time.deltaTime;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
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