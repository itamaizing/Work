using System.Collections.Generic;
using UnityEngine;

public class TiredSoul : AbstractCharacterState
{
    private float _duration;
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.TiredSoul;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        _duration = time;
        return true;
    }
}