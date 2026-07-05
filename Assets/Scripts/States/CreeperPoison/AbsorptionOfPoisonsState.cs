using System.Collections.Generic;
using UnityEngine;

public class AbsorptionOfPoisonsState : StackableState
{
    private Character _player;

    private float _maxHealth;
    private float _baseHealthIncrease = 0.1f;
    private float _increasedHealth;
    private float _allIncreasedHealth;

    private float _duration;
    private float _baseDuration;
    private AttributeModifier _attributeModifiers; 

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Absorptions };
    public override States State => States.AbsorptionOfPoison;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _attributeModifiers = new AttributeModifier(0, ModifierType.Flat);
        characterState = character;
        _player = personWhoMadeBuff;

        _duration = durationToExit;
        _baseDuration = durationToExit;

        _maxHealth = _player.Health.MaxValue;

        IncreaseHealth();
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float time)
    {
        currentStacksCount++;

        _duration = _baseDuration;

        IncreaseHealth();

        return true;
    }

    protected override void ExitState()
    {
        _player.Health.RemoveModifier(_attributeModifiers);
        //_player.Health.ChangedMaxValue(-_allIncreasedHealth);

        ResetValues();
    }

    private void IncreaseHealth()
    {
        _player.Health.RemoveModifier(_attributeModifiers);
        float increasingValue = currentStacksCount * _baseHealthIncrease;

        _increasedHealth = _maxHealth * increasingValue;

        _attributeModifiers.Value = _increasedHealth;

       
        //_player.Health.ChangedMaxValue(_increasedHealth);
        _player.Health.AddModifier(_attributeModifiers);

        _allIncreasedHealth += _increasedHealth;
    }

    private void ResetValues()
    {
        _player.Health.RemoveModifier(_attributeModifiers);
        _allIncreasedHealth = 0;

        currentStacksCount = 0;

        _duration = 0;

        _baseHealthIncrease = 0.1f;

        _increasedHealth = 0;
    }
}
