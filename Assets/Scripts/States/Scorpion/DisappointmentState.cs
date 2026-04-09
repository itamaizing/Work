using System.Collections.Generic;
using UnityEngine;

public class DisappointmentState : RefreshingState
{
    private float _baseDuration;
    private float _damageToExit;
    private float _damageOnStart = 0;
    private Animator _animator;
    private AnimatorStateInfo _currentState;
    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Move, StatusEffect.Ability };
    
    public override States State => States.DisappointmentState;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _baseDuration = durationToExit;
        _damageToExit = damageToExit == 0 ? 10000 : damageToExit;
        _damageOnStart = characterState.Character.Health.SumDamageTaken;

        characterState.Character.Move.SetCanMove(false);
        characterState.Character.Move.LookAtTransform(characterState.transform);

        if (characterState.Character.TryGetComponent(out SkillManager abilities))
        {
            base.abilities = abilities;
            foreach (var skill in base.abilities.Abilities)
            {
                skill.Disactive = true;
            }
        }

        MaxStacksCount = 1;
        currentStacksCount = 1;
    }

    public override void UpdateState()
    {
        //Debug.LogError("duration: "+duration);
        
        if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit || duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        if (!characterState.Check(StatusEffect.Move))
        {
            characterState.Character.Move.SetCanMove(true);
            characterState.Character.Move.StopLookAt();
        }

        if (!characterState.Check(StatusEffect.Ability) && abilities != null)
        {
            foreach (var skill in abilities.Abilities)
            {
                skill.Disactive = false;
            }
        }
        
        currentStacksCount = 0;
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = time;
        return true;
    }
}
