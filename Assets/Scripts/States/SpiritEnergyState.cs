using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpiritEnergyState : AbstractCharacterState
{
    private float _baseDuration;
    private float _duration;
    private int _stacks;
    private const int MaxStacks = 2;
    private const float ManaRestorePerStack = 0.09f;
    private const float BonusManaRestore = 0.05f;
    private const float HealthBonusPerStack = 1f; // Дополнительное здоровье за стак
    private List<StatusEffect> _effects = new ();
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.SpiritEnergy;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => _effects;

    private Health _healthComponent;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _baseDuration = durationToExit;
        _stacks = 1;
        
        _healthComponent = character.GetComponent<Health>();

        if (_healthComponent != null)
        {
            _healthComponent.HealTaked += OnHealTaked;
            _healthComponent.DamageTaken += OnDamageTaken;
        }

        ApplyManaRestore(ManaRestorePerStack * _stacks);
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;

        if (_duration <= 0 || _stacks == 0)
        {
            ExitState();
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
        if (_stacks >= MaxStacks)
        {
            return false;
        }

        _stacks++;
        _duration = Mathf.Max(_duration, time);

        ApplyManaRestore(ManaRestorePerStack * _stacks);

        return true;
    }

    private void ApplyManaRestore(float restoreValue)
    {
        _characterState.Character.Resources.FirstOrDefault(o => o.Type == ResourceType.Mana)?.Add(restoreValue);
    }
    
    private void OnHealTaked(float healAmount, Skill skill)
    {
        float bonusHeal = HealthBonusPerStack * _stacks;
        _healthComponent.Heal(bonusHeal);
        
        if (skill.Hero.CharacterState.CheckForState(States.SpiritEnergy))
        {
            ApplyManaRestore(BonusManaRestore * healAmount * _stacks);
        }
    }
    
    private void OnDamageTaken(float damageAmount, DamageType damageType, Skill skill)
    {
        ApplyManaRestore(ManaRestorePerStack * _stacks);
        
        if (skill.Hero.CharacterState.CheckForState(States.SpiritEnergy))
        {
            ApplyManaRestore(BonusManaRestore * damageAmount * _stacks);
        }
    }
}