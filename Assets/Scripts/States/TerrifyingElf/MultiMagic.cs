using System.Collections.Generic;
using UnityEngine;

public class MultiMagic : AbstractCharacterState
{
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Ability };

    public override States State => States.MultiMagic;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override float RemainingDuration => duration;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character caster, string skillName)
    {
        _characterState = character;
        duration = durationToExit;
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;
        if (duration <= 0f) ExitState();
    }

    public override void ExitState()
    {
        Debug.Log("выход из мульти");
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time) => false;
}
