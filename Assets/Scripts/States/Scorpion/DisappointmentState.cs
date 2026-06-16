using System.Collections.Generic;
using UnityEngine;

public class DisappointmentState : RefreshingState
{
    private float _baseDuration;
    private Animator _animator;
    private AnimatorStateInfo _currentState;
    private List<StatusEffect> _effects = new List<StatusEffect> { StatusEffect.Move, StatusEffect.Ability };
    public override DiminishingReturnGroup DrGroup => DiminishingReturnGroup.FearAndDisappointment;

    public override States State => States.DisappointmentState;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;
    
    private bool _isBleedingUpgrade = false;
    private bool _isAdditionalTime = false;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState.Character.Health.DamageTaken -= OnDamaged;
        duration = durationToExit;
        characterState.Character.Health.DamageTaken += OnDamaged;

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
        if (duration <= 0)
        {
            ExitState();
        }
    }

    private void OnDamaged(Damage dmg, Skill skill)
    {
        if (_isBleedingUpgrade && dmg.DamageKey == "bleeding")
        {
            return;
        }

        characterState.Character.Health.DamageTaken -= OnDamaged;
        ExitState();
    }

    public override void ExitState()
    {
        characterState.Character.Move.SetCanMove(true);
        characterState.Character.Move.StopLookAt();
        foreach (var skill in abilities.Abilities)
        {
            skill.Disactive = false;
        }

        if (characterState != null)
        {
            DiminishingReturnsTracker tracker;
            if (personWhoMadeBuff == null)
                tracker = characterState.Character.GetComponent<DiminishingReturnsTracker>();
            else
                tracker = personWhoMadeBuff.GetComponent<DiminishingReturnsTracker>();
            tracker?.OnEffectEnded(DrGroup);
        }
        
        currentStacksCount = 0;
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (_isAdditionalTime) duration = time;
        else
            duration = _baseDuration;
        return true;
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (!CanEnterState(character)) return null;

        _isBleedingUpgrade = skillName.Contains("bleedingUpgrade");
        _isAdditionalTime = skillName.Contains("bonus");

        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        
        EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        return this;
    }
}
