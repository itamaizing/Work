using System.Collections.Generic;
using UnityEngine;

public class TrueSightState : AbstractCharacterState
{
    public override States State => States.TestAuraState;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => new();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _abilities = character.Character.GetComponent<SkillManager>();
        _health = character.Character.GetComponent<Health>();
        _personWhoMadeBuff = personWhoMadeBuff;

        duration = durationToExit;
        MaxStacksCount = 0;
        CanStack = false;

        CheckInvisibility();
    }

    public override void UpdateState()
    {
        duration -= Time.deltaTime;

        if (duration <= 0)
        {
            ExitState();
        }

        CheckInvisibility();
    }

    public override void ExitState()
    {
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = Mathf.Max(duration, time);
        return false;
    }

    private void CheckInvisibility()
    {
        var state = _characterState;

        if (state.CheckForState(States.Invisible) || state.CheckForState(States.CreeperInvisible))
        {
            Debug.Log($"[TrueSight] Обнаружен невидимый персонаж: {state.Character.name}");
        }
    }
}