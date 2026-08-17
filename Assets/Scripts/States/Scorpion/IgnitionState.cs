using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class IgnitionState : RefreshingState
{
    private float _tickTimer = 0f;
    private int _currentTick = 0;
    private int MaxTicks = 6;
    private const float TickInterval = 1f;
    private const float BaseScorchedChance = 5f;
    private float _damageBonus = 0;

    private float _fireBreathBonus = 0f;
    public override States State => States.Ignition;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Others };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _currentTick = 0;
        _tickTimer = 0f;
        MaxTicks = Mathf.Max(1, Mathf.CeilToInt(durationToExit / TickInterval));
        duration = durationToExit;
        _schoolState = Schools.Fire;
    }

    public override void UpdateState()
    {
        _tickTimer += Time.deltaTime;

        if (_tickTimer >= TickInterval)
        {
            _tickTimer -= TickInterval;
            _currentTick++;
            ApplyTick();

            if (_currentTick >= MaxTicks)
                ExitState();
        }
    }
    
    public void UpdateFireBreathBonus(float bonus)
    {
        MaxTicks += Mathf.CeilToInt(bonus / TickInterval);
        _fireBreathBonus = bonus;
    }

    private void ApplyTick()
    {
        if (characterState.isClient) return;

        float finalDamage = _currentTick + _damageBonus + _fireBreathBonus;

        var damage = new Damage { Value = finalDamage, Type = DamageType.Magical };
        health.TryTakeDamage(ref damage, skill);

        float chance = BaseScorchedChance * _currentTick;
        if (Random.Range(0f, 100f) <= chance)
            characterState.AddState(States.ScorchedSoul, 6f, 0f, personWhoMadeBuff.gameObject, nameof(IgnitionState));
    }

    public override void ExitState()
    {
        _currentTick = 0;
        _tickTimer = 0f;
        _damageBonus = 0f;
        _fireBreathBonus = 0;
        MaxTicks = 6;
        characterState.RemoveState(this);
    }
    
    private float ExtractNumber(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0f;

        Match match = Regex.Match(text, @"-?\d+(\.\d+)?");

        if (match.Success &&
            float.TryParse(match.Value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out float value))
        {
            return value;
        }

        return 0f;
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit,
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _currentTick = 0;
        _tickTimer = 0f;
        MaxTicks = Mathf.Max(1, Mathf.CeilToInt(durationToExit / TickInterval));
        duration = durationToExit;
        _damageBonus = ExtractNumber(skillName);
        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        return this;
    }
}
