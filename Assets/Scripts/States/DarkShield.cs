using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DarkShield : AbstractCharacterState
{
    private float _damageDebuffDelay = 0.2f;
    private float _maxDamagePerTick;
    private float _duration;
    private Health _healthComponent;

    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.DarkShield;
    public override StateType Type => StateType.Immaterial;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _maxDamagePerTick = damageToExit;
        
        _healthComponent = character.GetComponent<Health>();
        if (_healthComponent != null)
        {
            _healthComponent.DamageTaken += HandleDamageTaken;
        }
    }

    public override void ExitState()
    {
        if (_healthComponent != null)
        {
            _healthComponent.DamageTaken -= HandleDamageTaken;
        }
        
        _characterState.RemoveState(this);
    }

    private void HandleDamageTaken(float damage, DamageType type, Skill skill)
    {
        if (_healthComponent == null) return;
        
        _healthComponent.StartCoroutine(ApplyDelayedDamage(damage));
    }

    private IEnumerator ApplyDelayedDamage(float damage)
    {
        yield return new WaitForSeconds(_damageDebuffDelay);

        var damageToApply = Mathf.Min(damage, _maxDamagePerTick);
        var damageToTake = new Damage { Value = damageToApply };
        
        _healthComponent.TryTakeDamage(ref damageToTake, null);
    }

    public override bool Stack(float time)
    {
        _duration = time;
        return true;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }
}
