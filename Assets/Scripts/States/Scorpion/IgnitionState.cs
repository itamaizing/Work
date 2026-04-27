using System.Collections.Generic;
using UnityEngine;

public class IgnitionState : RefreshingState
{
    private float _tickTimer = 0f;
    private int _currentTick = 0;
    private const int MaxTicks = 6;
    private const float TickInterval = 1f;
    private const float BaseScorchedChance = 5f;

    public override States State => States.Ignition;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Others };

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _currentTick = 0;
        _tickTimer = 0f;
        duration = MaxTicks;
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

    private void ApplyTick()
    {
        if(characterState.isClient) return;
        
        var damage = new Damage
        {
            Value = _currentTick,
            Type = DamageType.Magical,
        };
        health.TryTakeDamage(ref damage, skill);

        float chance = BaseScorchedChance * _currentTick;
        if (Random.Range(0f, 100f) <= chance)
        {
            characterState.AddState(States.ScorchedSoul, 6f, 0f,
                personWhoMadeBuff.gameObject, nameof(IgnitionState));
        }
    }

    public override void ExitState()
    {
        _currentTick = 0;
        _tickTimer = 0f;
        characterState.RemoveState(this);
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit,
        float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        _currentTick = 0;
        _tickTimer = 0f;
        duration = MaxTicks;
        return this;
    }
}
