using System;
using System.Collections.Generic;
using UnityEngine;

public class LightShield : AbstractCharacterState, IDamageable
{
    private float _damageAbsorbed;
    private float _maxAbsorption = 20f;
    private float _duration;
    private bool _isTalentActive = false;

    public event Action<float, DamageType, Skill> DamageTaken;

    public override States State => States.LightShield;
    public override StateType Type => StateType.Magic;
    public override List<StatusEffect> Effects => new List<StatusEffect>();

    public override void EnterState(CharacterState character, float durationToExit, float isTalentActive, Character personWhoMadeBuff, string skillName)
    {
        _characterState = character;
        _duration = durationToExit;
        _isTalentActive = isTalentActive > 0;
        _damageAbsorbed = 0;
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
        
        var damageToTake = new Damage { Value = damageToAbsorb };
        var targets = GetCloserTargets(_characterState.transform.position, 10f);
            
        foreach (var target in targets)
        {
            target.Health.CmdTryTakeDamage(damageToTake, null);
            target.GetComponent<Character>().DamageTracker.AddDamage(damageToTake);
        }
        
        _characterState.GetComponent<Character>().DamageTracker.AddDamage(damage);
        DamageTaken?.Invoke(damageToAbsorb, damage.Type, skill);
        
        if (_damageAbsorbed >= _maxAbsorption)
        {
            return true;
        }

        return damage.Value == 0;
    }
    
    private List<Character> GetCloserTargets(Vector3 position, float radius)
    {
        List<Character> targets = new List<Character>();
        
        var enemyLayerMask = LayerMask.GetMask("Enemy");
        
        var colliders = Physics2D.OverlapCircleAll(position, radius);
        
        foreach (var item in colliders)
        {
            Debug.Log(item.name);
            if (item.transform.TryGetComponent(out Character enemy))
            {
                targets.Add(enemy);
            }
        }

        return targets;
    }

	public void ShowPhantomValue(Damage phantomValue)
	{
		throw new NotImplementedException();
	}
}
