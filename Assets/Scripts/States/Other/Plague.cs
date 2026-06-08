using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class Plague : RefreshingState
{
    private float _tickTimer = 3f;

    private const int MaxStacks = 3;
    private const float TickInterval = 3f;
    private const float DurationTime = 12f;

    private float _damageSum;

    public override States State => States.Plague;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    private readonly List<StatusEffect> _effects = new();
    public override List<StatusEffect> Effects => _effects;
    
    private Resource _healthResource;

    public override void EnterState(CharacterState character,
        float durationToExit,
        float damageToExit,
        Character personWhoMadeBuff,
        string skillName)
    {
        _damageSum = 0;
        characterState = character;
        duration = DurationTime;
        _tickTimer = TickInterval;
        
        _healthResource = character.Character.TryGetResource(ResourceType.Health);

        currentStacksCount = 1;
        
        if (_healthResource == null)
            ExitState();
        
        ApplyDamage();
    }

    public override void UpdateState()
    {
        _tickTimer -= Time.deltaTime;

        if (_tickTimer > 0) return;

        _tickTimer = TickInterval;
        
        ApplyDamage();
    }

    private void ApplyDamage()
    {
        float maxHp = _healthResource.MaxValue;

        float damageValue = maxHp * 0.01f * currentStacksCount;
        
        var dmg = new Damage { Value = damageValue, School = Schools.Dark };

        if (characterState.isServer)
            characterState.Character.TryTakeDamage(ref dmg,null);

        _damageSum += dmg.Value;
    }

    public override bool Stack(float time)
    {
        if (currentStacksCount < MaxStacks) currentStacksCount++;


        return true;
    }

    public override void ReduceStack()
    {
        currentStacksCount--;

        if (currentStacksCount <= 0)
        {
            characterState.StateIcons.RemoveItemByState(State);
            ExitState();
        }
        else
        {
            characterState.StateIcons.ActivateIco(State, duration, -1, true, MaxStacksCount);
        }
    }
    
    public float GetSumDamage()
    {
        return _damageSum;
    }

    public override void ExitState()
    {
        base.ExitState();
        currentStacksCount = 0;
        _damageSum = 0;
    }

    public override AbstractCharacterState TryApply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        if (currentStacksCount == 0)
        {
            BaseInit(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);

            EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        }
        else
        {
            Stack(durationToExit);
        }

        return this;
    }
}