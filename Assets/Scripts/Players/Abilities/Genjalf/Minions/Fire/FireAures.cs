using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Collections;
using UnityEngine;

public class FireAures : MonoBehaviour
{
    private void Start()
    {
        //var chatacter = GetComponent<Character>();
        //chatacter.CharacterState.CmdAddState(States.Burn, 0, 0, chatacter.gameObject, name);
    }
}

public class Burn : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Others };
    
    private float _damagePerSecond = 1f;
    private float _damageRadius = 1f;
    private float _timer = 0f;
    private LayerMask _enemyLayer;

    public override States State => States.Burn;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _enemyLayer = LayerMask.GetMask("Enemy");
        
        character.Character.Health.DamageTaken += OnDamageTaken;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (skill == null) return;
        if (damage.Type != DamageType.Physical) return;
        if (damage.PhysicAttackType != AttackRangeType.MeleeAttack) return;

        skill.Hero.CharacterState.AddState(States.Burning, 7f, 0,
            characterState.Character.gameObject, nameof(Burning));
    }

    public override void UpdateState()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;

        var colliders = Physics.OverlapSphere(
            characterState.Character.transform.position, _damageRadius, _enemyLayer);

        foreach (var col in colliders)
        {
            if (col.TryGetComponent<Character>(out var enemy))
            {
                Damage damage = new Damage
                {
                    Value = _damagePerSecond,
                    Type = DamageType.Magical,
                    School = Schools.Fire,
                };
                enemy.CmdTryTakeDamage(damage, null);
            }
        }
    }

    protected override void ExitState()
    {
        if (characterState?.Character != null)
            characterState.Character.Health.DamageTaken -= OnDamageTaken;
        
    }
}

public class Burning : RefreshingState
{
    private List<StatusEffect> _effects = new List<StatusEffect>();
    protected float _damage = 1;
    protected float _timeAfterLastEffect = 0;
    protected float _effectRate = 1;

    private float _baseDuration;
    private float _stackTimer;

    public override States State => States.Burning;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => _effects;

    public override float RemainingDuration => _baseDuration;

    protected override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        Damage damage = new Damage
        {
            Value = _damage,
        };
        if(character.isClient)
            character.Character.CmdTryTakeDamage(damage, null);

        MaxStacksCount = 5;
        _baseDuration = durationToExit;
        _stackTimer = durationToExit;
    }

    public override bool Stack(float time)
    {
        _stackTimer = _baseDuration;
        return true;
    }

    public override void GloabalUpdate()
    {
        UpdateState();
    }

    public override void UpdateState()
    {
        _stackTimer -= Time.deltaTime;

        if (_stackTimer <= 0)
        {
            currentStacksCount--;
            if (CurrentStacksCount <= 0)
            {
                GlobalExit();
                return;
            }

            _stackTimer = _baseDuration;
        }
        _timeAfterLastEffect += Time.deltaTime;

        if (_timeAfterLastEffect < _effectRate) return;

        Damage damage = new Damage { Value = _damage };
        if(characterState.isClient)
            characterState.Character.CmdTryTakeDamage(damage, null);
        _timeAfterLastEffect = 0;
    }

}

