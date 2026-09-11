using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillingSpreeStateStacking : RefreshingStateStacking
{
    public override DiminishingReturnGroup DrGroup => DiminishingReturnGroup.None;
    public override States State => States.KillingSpree;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects  => new List<StatusEffect>();

    private float _bonusPerTargetSwitch = 0.20f;
    private float _currentBonus = 1;
    
    private Character _lastTarget;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        parameters[StateParameter.DamageToExit] = damageToExit == 0 ? 1000 : damageToExit;
        if (characterState.isServer)
            SubscribeToPhysicalSkills();
    }

    public override void UpdateState()
    {
        if (RemainingDuration <= 0)
        {
            ExitState();
        }
    }

    private void SubscribeToPhysicalSkills()
    {
        foreach (var physSkill in characterState.Character.Abilities.Abilities)
        {
            if (physSkill.Info.AbilityForm == AbilityForm.Physical || physSkill.Info.AbilityForm == AbilityForm.Both)
            {
                if(physSkill is IComboParticipatingSkill comboParticipating)
                {
                    comboParticipating.OnBeforeApplyParticipatingDamage += OnPhysicalDamageApplied;
                    continue;
                }
                physSkill.OnBeforeApplyDamage += OnPhysicalDamageApplied;
            }
        }
    }

    private void UnsubscribeFromPhysicalSkills()
    {
        foreach (var physSkill in characterState.Character.Abilities.Abilities)
        {
            if (physSkill.Info.AbilityForm == AbilityForm.Physical || physSkill.Info.AbilityForm == AbilityForm.Both)
            {
                if(physSkill is IComboParticipatingSkill comboParticipating)
                {
                    comboParticipating.OnBeforeApplyParticipatingDamage -= OnPhysicalDamageApplied;
                    continue;
                }
                physSkill.OnBeforeApplyDamage -= OnPhysicalDamageApplied;
            }
        }
    }
    
    private void OnPhysicalDamageApplied(ref Damage damage, Skill skill, GameObject targetObj)
    {
        var target = targetObj.GetComponent<Character>();
        if (target == null) return;

        if (target == _lastTarget)
        {
            ExitState();
            return;
        }
        
        _lastTarget = target;
        _currentBonus += _bonusPerTargetSwitch;

        damage.Value *= _currentBonus;
    }

    public override void ExitState()
    {
        if(characterState.isServer)
            UnsubscribeFromPhysicalSkills();
        _lastTarget = null;
        _currentBonus = 1f;
        currentStacksCount = 0;

        base.ExitState();
    }

    public override bool Stack(float time)
    {
        
        return true;
    }

    
}
