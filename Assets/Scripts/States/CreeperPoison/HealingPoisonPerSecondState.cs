using System.Collections.Generic;
using UnityEngine;

public class HealingPoisonPerSecondState : StateBasic
{
    private const float TickInterval = 1f;
    private float _currentHealingValue;
    private float _timeBetweenHeal;
    private readonly List<StatusEffect> _effects = new() { StatusEffect.Healing, StatusEffect.Poison };

    public float TotalHealValue => _currentHealingValue;
    public override bool IsUnique => false;
    protected override float DescriptionTickInterval => TickInterval;
    protected override float? DescriptionHealing => _currentHealingValue + 1f;

    public override States State => States.HealingPoisonPerSecond;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    public override void Apply(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _currentHealingValue = 0f;
        _timeBetweenHeal = TickInterval;
    }

    public override void UpdateState()
    {
        if (characterState == null || !characterState.isServer) return;

        _timeBetweenHeal -= Time.deltaTime;
        if (_timeBetweenHeal > 0f) return;
        _timeBetweenHeal += TickInterval;
        _currentHealingValue += 1f;

        Heal heal = new Heal { Value = _currentHealingValue, DamageableSkill = null };
        characterState.Character.Health.Heal(ref heal, null);
    }
}
