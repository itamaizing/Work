using System.Collections.Generic;
using UnityEngine;

public class DisappointmentState : AbstractCharacterState
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

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
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
    }

    public override void UpdateState()
    {
        if (characterState.Character.Health.SumDamageTaken - _damageOnStart >= _damageToExit)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);

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
    }

    public override bool Stack(float time)
    {
        duration = _baseDuration;
        return true;
    }
}
