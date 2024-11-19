using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpiritEnergyState : AbstractCharacterState
{
    private Skill _skill;
    
    private float _baseDuration;
    private float _duration;
    private bool _isTalentActive = false;
    
    private const float ManaRestorePerStack = 0.09f;
    private const float BuffedManaRestorePerStack = 0.18f;
    private const float BonusManaRestore = 0.05f;
    private const float BuffedBonusManaRestore = 0.1f;
    private const float HealthBonusPerStack = 1f;
    
    private List<StatusEffect> _effects = new ();
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override States State => States.SpiritEnergy;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    public override float TEST_ChangeableValue { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    private Health _healthComponent;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _skill = personWhoMadeBuff.Abilities.Abilities.FirstOrDefault(o => o.Name == skillName);
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        CurrentStacksCount++;
        MaxStacksCount = 2;
        _isTalentActive = damageToExit > 0;
        
        _healthComponent = character.GetComponent<Health>();

        if (_healthComponent != null)
        {
            _healthComponent.HealTaked += OnHealTaked;
            _healthComponent.DamageTaken += OnDamageTaken;
        }

        var manaRestoreValue = _isTalentActive ? BuffedManaRestorePerStack : ManaRestorePerStack;
        ApplyManaRestore(manaRestoreValue * CurrentStacksCount);
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        
        if (_duration <= _baseDuration * (CurrentStacksCount - 1) && CurrentStacksCount > 0)
        {
            CurrentStacksCount--;
            _duration = _baseDuration * CurrentStacksCount;

            if (CurrentStacksCount == 0)
            {
                ExitState();
            }
        }
    }

    public override void ExitState()
    {
        if (_healthComponent != null)
        {
            _healthComponent.HealTaked -= OnHealTaked;
            _healthComponent.DamageTaken -= OnDamageTaken;
        }
        
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
            _duration += time;
            _duration = Mathf.Min(_duration, _baseDuration * CurrentStacksCount);
            var manaRestoreValue = _isTalentActive ? BuffedManaRestorePerStack : ManaRestorePerStack;
            ApplyManaRestore(manaRestoreValue * CurrentStacksCount);
        }
        
        return true;
    }

    private void ApplyManaRestore(float restoreValue)
    {
        _characterState.Character.Resources.FirstOrDefault(o => o.Type == ResourceType.Mana)?.Add(restoreValue);
    }
    
    private void OnHealTaked(float healAmount, Skill skill, string sourceName)
    {
        float bonusHeal = HealthBonusPerStack * CurrentStacksCount;
        var currentSkill = skill;

        if (currentSkill == null)
        {
            currentSkill = _skill;
        }
        var heal = new Heal { Value = bonusHeal };
        
        if (sourceName != nameof(States.SpiritEnergy))
        {
            currentSkill.CmdApplyHeal(heal, _healthComponent.gameObject, null, nameof(States.SpiritEnergy));   
        }
        
        if (currentSkill.Hero.CharacterState.CheckForState(States.SpiritEnergy))
        {
            var manaRestoreBonusValue = _isTalentActive ? BuffedBonusManaRestore : BonusManaRestore;
            ApplyManaRestore(manaRestoreBonusValue * healAmount * CurrentStacksCount);
        }
    }

    private void OnDamageTaken(float value, Damage damage, Skill skill)
    {
        var manaRestoreValue = _isTalentActive ? BuffedManaRestorePerStack : ManaRestorePerStack;
        ApplyManaRestore(manaRestoreValue * CurrentStacksCount);

        if (skill.Hero.CharacterState.CheckForState(States.SpiritEnergy))
        {
            var manaRestoreBonusValue = _isTalentActive ? BuffedBonusManaRestore : BonusManaRestore;
            ApplyManaRestore(manaRestoreBonusValue * damage.Value * CurrentStacksCount);
        }
    }
}