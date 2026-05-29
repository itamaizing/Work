using System.Collections;
using Mirror;
using UnityEngine;

public class StingingHitsPassive : Skill, IPassiveSkill
{
    [SerializeField] private ScorpionPassive scorpionPassive;
    private float _critChance = 0.1f;
    private float _critMultiplierLow = 1.7f;
    private float _critMultiplierHigh = 2.7f;
    protected override bool IsCanCast => false;
    
    private bool _isOnCooldown = false;
    private int _remainingHits = 0;
    private Coroutine _cooldownCoroutine;
    private bool _isEnabled;
    private Skill _lastCriticalSkill;

    protected override IEnumerator CastJob()
    {
        yield return null;
    }

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    public void EnableStingingHits(bool value)
    {
        if (_isEnabled == value) return;
        _isEnabled = value;

        CmdEnableStingingHits(_isEnabled);
    }

    [Command]
    private void CmdEnableStingingHits(bool value)
    {
        if (value)
        {
            SubscribeToPhysicalSkills();
        }
        else
        {
            UnsubscribeFromPhysicalSkills();
        }
    }
    
    private void SubscribeToPhysicalSkills()
    {
        var abilities = _hero.Abilities.Abilities;

        foreach (var skill in abilities)
        {
            if (skill.Info.AbilityForm == AbilityForm.Physical || skill.Info.AbilityForm == AbilityForm.Both)
            {
                skill.OnBeforeApplyDamage += OnBeforePhysicalDamage;

                if(skill is IComboParticipatingSkill comboParticipating)
                {
                    comboParticipating.OnBeforeApplyParticipatingDamage += OnBeforePhysicalDamage;
                }
            }
        }
    }

    private void UnsubscribeFromPhysicalSkills()
    {
        var abilities = _hero.Abilities.Abilities;

        foreach (var skill in abilities)
        {
            if (skill.Info.AbilityForm == AbilityForm.Physical || skill.Info.AbilityForm == AbilityForm.Both)
            {
                skill.OnBeforeApplyDamage -= OnBeforePhysicalDamage;

                if(skill is IComboParticipatingSkill comboParticipating)
                {
                    comboParticipating.OnBeforeApplyParticipatingDamage -= OnBeforePhysicalDamage;
                }
            }
        }
    }
    
    private void OnBeforePhysicalDamage(ref Damage damage, Skill skill, GameObject target)
    {
        if (target == null) return;

        bool isCritical = false;
        if (_lastCriticalSkill is ISwordSkill || _lastCriticalSkill is AbsorbationSwordSkill)
        {
            if (skill is AbsorbationSwordSkill) isCritical = true;
        }

        var characterTarget = target.GetComponent<Character>();
        
        if (WasDebuffAppliedByHandOrFoot(skill, characterTarget) && !isCritical) isCritical = true;
        else if (target.GetComponent<CharacterState>().CheckForState(States.Stun) && scorpionPassive.IsAddStateUpdateChance)
        {
            if(UnityEngine.Random.value <= _critChance + scorpionPassive.AdditionalAddStateChance&& !isCritical) 
                isCritical = true;
        }
        else if (UnityEngine.Random.value <= _critChance && !isCritical) isCritical = true;

        if (isCritical)
        {
            _lastCriticalSkill = skill;
            
            float multiplier = Random.Range(_critMultiplierLow, _critMultiplierHigh);

            damage.Value *= multiplier;
        }
    }
    
    private bool WasDebuffAppliedByHandOrFoot(Skill skill, Character target)
    {
        if (skill is NewPunch_Scorpion || skill is Kick_Scorpion)
        {
            return false;
        }

        return false;
    }
}
