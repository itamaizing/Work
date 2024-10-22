using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightShield : AbstractCharacterState, IDamageable
{
    private float _damageAbsorbed;
    private float _maxAbsorption;
    private float _duration;
    
    private bool _isBMTalentActive = false;

    public event Action<float, DamageType, Skill> DamageTaken;

    public override States State => States.LightShield;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float maxDamageAbsorbed, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _damageAbsorbed = 0;
        _maxAbsorption = maxDamageAbsorbed;

        var priestShield = _characterState.Character.Abilities.Abilities.FirstOrDefault(o => o.name == skillName);

        if (priestShield != null)
        {
            _isBMTalentActive = priestShield;
        }

        DamageTaken += DamageEnemiesInRadius;
    }

    public override void UpdateState()
    {
        _duration -= Time.deltaTime;
        if (_duration <= 0 || _damageAbsorbed >= _maxAbsorption)
        {
            ExitState();
        }
    }

    public override void ExitState()
    {
        Debug.Log("LightShield state exited.");
        DamageTaken -= DamageEnemiesInRadius;
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
        
        _characterState.GetComponent<Character>().DamageTracker.AddDamage(damage);
        DamageTaken?.Invoke(damageToAbsorb, damage.Type, skill);
        
        if (_damageAbsorbed >= _maxAbsorption)
        {
            return true;
        }

        return damage.Value == 0;
    }
    
    private void DamageEnemiesInRadius(float damage, DamageType type, Skill skill)
    {
        if(!_isBMTalentActive) return;
        
        var enemyLayerMask = LayerMask.GetMask("Enemy");
        
        var colliders = Physics2D.OverlapCircleAll(_characterState.transform.position, 10f, enemyLayerMask);
        
        foreach (var item in colliders)
        {
            if (item.transform.TryGetComponent(out Character enemy))
            {
                var damageToTake = new Damage { Value = damage };
                
                enemy.Health.CmdTryTakeDamage(damageToTake, null);
                enemy.GetComponent<Character>().DamageTracker.AddDamage(damageToTake);
            }
        }
    }

	public void ShowPhantomValue(Damage phantomValue)
	{
		throw new NotImplementedException();
	}
}
