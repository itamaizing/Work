using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeakeningSilence : StackableState
{
    private float _damagePerTick;
    private float _currentDamage;
    private float _tickInterval = 1f;

    private bool damageTick;

    public override States State => States.WeakeningSilence;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => new List<StatusEffect> { StatusEffect.Poison };

    public WeakeningSilence() => MaxStacksCount = 6;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        health = character.Character.Health;
        _damagePerTick = damageToExit;
        damageTick = true;
        _currentDamage = _damagePerTick;

        if (health == null)
        {
            Debug.LogWarning($"Health component is missing on {character.name}. WeakeningSilence will not deal damage.");
            return;
        }

        characterState.StartCoroutine(PeriodicDamageRoutine());
    }
    
    protected override void ExitState()
    {
        characterState.RemoveStateFromList(this);
        damageTick = false;
        characterState.StopCoroutine(PeriodicDamageRoutine());
    }

    public override void UpdateState()
    {
    }

    public override bool Stack(float addDuration)
    {
        if (currentStacksCount >= MaxStacksCount) return false;

        currentStacksCount++;
        _currentDamage += _damagePerTick;
        base.duration = Mathf.Max(base.duration, addDuration);

        return true;
    }

    private IEnumerator PeriodicDamageRoutine()
    {
        while (damageTick)
        {
            yield return new WaitForSeconds(_tickInterval);
            ApplyDamage();
        }
    }

    [Server]
    private void ApplyDamage()
    {
        if (health != null)
        {
            Damage damage = new Damage
            {
                Value = _currentDamage,
                Type = DamageType.Magical
            };
            if (health != null)
            {
                health.TryTakeDamage(ref damage, null);
            }
        }
        else
        {
            Debug.LogError("Health is null in CmdApplyDamage.");
        }
    }
}
