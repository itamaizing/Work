using System;
using System.Collections.Generic;
using UnityEngine;

public class LightShield : AbstractCharacterState, IDamageable
{
    private float _damageAbsorbed;
    private float _maxAbsorption;
    private float _duration;

    public event Action<float, DamageType, Skill> DamageTaken;
    public override float TEST_ChangeableValue { get; set; }
    public override States State => States.LightShield;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _maxAbsorption = damageToExit;
        _damageAbsorbed = 0;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("LightShield state exited.");
        _characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        _duration = time;
        _damageAbsorbed = 0;
        return true;
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        float damageToAbsorb = Mathf.Min(_maxAbsorption - _damageAbsorbed, damage.Value);
        _damageAbsorbed += damageToAbsorb;
        damage.Value -= damageToAbsorb;

        DamageTaken?.Invoke(damageToAbsorb, damage.Type, skill);

        if (_damageAbsorbed >= _maxAbsorption)
        {
            ExitState();
            return true;
        }

        return damage.Value == 0;
    }

	public void ShowPhantomValue(Damage phantomValue)
	{
		throw new NotImplementedException();
	}
}
