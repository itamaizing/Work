using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class SpikeTentacle : MonoBehaviour
{
    [SerializeField] private Animator _animatorSpike;

    private Skill _skill;
    private Character _target;
    private Character _player;

    private const string SpawnSpikeTrigger = "SpawnSpike";

    public void Init(Character target, Character player, Skill skill)
    {
        _skill = skill;
        _target = target;
        _player = player;
        
        Invoke("SpawnSpike", 1f);
    }

    private void SpawnSpike()
    {
        _animatorSpike.SetTrigger(SpawnSpikeTrigger);

        DamageTarget();
        Destroy(gameObject, 0.5f);
    }

    private void DamageTarget()
    {
        var damage = new Damage
        {
            Value = 30f,
            Type = DamageType.Physical,
            PhysicAttackType = AttackRangeType.MeleeAttack
        };

        _skill.ApplyDamage(damage, _target.gameObject);
        _target.CharacterState.AddState(States.Stun, 2f, 0f, _player.gameObject, "TentacleSpike");
    }
}
