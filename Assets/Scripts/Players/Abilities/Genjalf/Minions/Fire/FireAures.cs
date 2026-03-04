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
        var chatacter = GetComponent<Character>();
        chatacter.CharacterState.CmdAddState(States.Burn, 0, 0, chatacter.gameObject, name);
    }
}

public class Burn : AuraState
{
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    private float _damage = 1;

    public override float Distance => 2;
    public override float EffectRate => 1f;
    public override LayerMask LayerMask => LayerMask.GetMask("Enemy");
    public override States State => States.Burn;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    private Character _currentCharacter;

    public override void EffectOnEnter(Character character)
    {
        _currentCharacter = character;
        
        _currentCharacter.Health.DamageTaken += OnDamageTaken;
    }
    
    public override void EffectOnExit(Character character)
    {
    }

    private void OnDamageTaken(Damage damage, Skill target)
    {
        if (damage.Type == DamageType.Physical && damage.PhysicAttackType == AttackRangeType.MeleeAttack)
        {
            if (target.gameObject != null)
            {
                CmdAddState(target.Hero.gameObject);
            }
        }
    }
    
    [Command]
    private void CmdAddState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.AddState(States.Burning,7,0,target,nameof(Burning));
    }

    public override void UpdateState()
    {
        base.UpdateState();
        if (duration > 0)
        {
            duration -= Time.deltaTime;
        }
        else
        {
            ExitState();
            if (_currentCharacter)
            {
                _currentCharacter.Health.DamageTaken -= OnDamageTaken;
                _currentCharacter = null;
            }
        }
    }

    public override void EffectOnStay(List<Character> characters)
    {
        foreach (Character character in characters)
        {
            if (character == _self)
                continue;

            Damage damage = new Damage
            {
                Value = _damage,
            };
            character.CmdTryTakeDamage(damage, null);
        }
    }
}

public class Burning : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };
    protected float _damage = 1;
    protected float _timeAfterLastEffect = 0;
    protected float _effectRate = 1;

    public override States State => States.Burning;

    public override StateType Type => StateType.Magic;

    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => _effects;

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {

        Damage damage = new Damage
        {
            Value = _damage,
        };
        character.Character.CmdTryTakeDamage(damage, null);
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        duration = time;
        return false;
    }

    public override void UpdateState()
    {

        _timeAfterLastEffect += Time.deltaTime;

        if (_effectRate > _timeAfterLastEffect)
            return;


        Damage damage = new Damage
        {
            Value = _damage,
        };
        characterState.Character.CmdTryTakeDamage(damage,null);
        _timeAfterLastEffect = 0;
    }
}

public class BurningStacked : Burning
{
    public override States State => States.BurningStacked;

    private float _baseDuration;
    private float _stackTimer;

    public override float RemainingDuration
    {
        get => _stackTimer;
        set => _stackTimer = value;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        base.EnterState(character, durationToExit, damageToExit, personWhoMadeBuff, skillName);
        MaxStacksCount = 3;
        currentStacksCount = 1;
        _baseDuration = durationToExit;
        _stackTimer = durationToExit;
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount >= MaxStacksCount)
            return false;

        currentStacksCount++;
        _stackTimer = _baseDuration;
        return true;
    }

    public override void UpdateState()
    {
        _stackTimer -= Time.deltaTime;

        if (_stackTimer <= 0)
        {
            currentStacksCount--;

            if (CurrentStacksCount <= 0)
            {
                ExitState();
                return;
            }

            _stackTimer = _baseDuration;
        }
        _timeAfterLastEffect += Time.deltaTime;

        if (_timeAfterLastEffect < _effectRate) return;

        Damage damage = new Damage { Value = _damage };
        characterState.Character.CmdTryTakeDamage(damage, null);
        _timeAfterLastEffect = 0;
    }

    public override void ExitState()
    {
        characterState.RemoveState(this);
    }
}

