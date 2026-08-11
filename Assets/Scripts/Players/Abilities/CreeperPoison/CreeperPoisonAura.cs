using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreeperPoisonAura : NetworkBehaviour
{
    [Header("Poison Aura Settings")]
    [SerializeField] private float _radius = 6f;
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _attackSpeedPerStack = 0.1f;
    [SerializeField] private float _tickRate = 0.2f;

    private Coroutine _poisonAuraRoutine;
    private int _lastStacks = 0;
    
    private int _lastEnergyStacks = 0;

    private Health _health;
    private Character _owner;

    private float _tempEvadeBonus = 0f;
    
    private readonly AttributeModifier _castSpeedModifier = new AttributeModifier(1, ModifierType.Percent);

    #region Talent
    private bool _isFeelingPoisoning = false;
    private bool _isEvadePoison = false;
    private bool _isOwnElement = false;
    private bool _isPleasurePoisoning = false;
    private bool _isActiveWitheringPoison = false;
    private bool _isActiveWitheringPoisonMetabolism = false;
    private bool _isDecreaseCooldownDamage = false;

    public bool IsFeelingPoisoning { get => _isFeelingPoisoning; set => _isFeelingPoisoning = value; }
    public bool IsActiveWitheringPoison { get => _isActiveWitheringPoison; set => _isActiveWitheringPoison = value; }
    public bool IsActiveWitheringPoisonMetabolism { get => _isActiveWitheringPoisonMetabolism; set => _isActiveWitheringPoisonMetabolism = value; }

    public void DecreaseCooldownDamage(bool value) => _isDecreaseCooldownDamage = value;
    public void ActiveWitheringPoisonMetabolism(bool value) => _isActiveWitheringPoisonMetabolism = value;
    public void ActiveWitheringPoison(bool value) => _isActiveWitheringPoison = value;

    public void PleasurePoisoning(bool value)
    {
        _isPleasurePoisoning = value;
        EvaluateAuraState();
    }

    public void OwnElement(bool value)
    {
        if(_isOwnElement == value) return;

        _isOwnElement = value;
        if(isClient)
            CmdOwnElement(value);
    }

    [Command]
    private void CmdOwnElement(bool value)
    {
        _isOwnElement = value;
        EvaluateAuraState();
    }

    public void FeelingPoisoning(bool value) => _isFeelingPoisoning = value;
    public void EvadePoison(bool value) => _isEvadePoison = value;
    #endregion

    public override void OnStartServer()
    {
        base.OnStartServer();

        _owner = GetComponent<Character>();
        _health = GetComponent<Health>();

        if (_health != null)
        {
            _health.OnBeforeTakeDamage += OnBeforeTakeDamage;
            _health.DamageTaken += OnAfterDamage;
        }

        if (_owner != null && _owner.DamageTracker != null)
        {
            _owner.DamageTracker.OnDamageTracked += OnDamageDealt;
        }
        
        EvaluateAuraState();
    }

    public override void OnStopServer()
    {
        if (_health != null)
        {
            _health.OnBeforeTakeDamage -= OnBeforeTakeDamage;
            _health.DamageTaken -= OnAfterDamage;
        }

        if (_owner != null && _owner.DamageTracker != null)
        {
            _owner.DamageTracker.OnDamageTracked -= OnDamageDealt;
        }

        StopAuraRoutine();
    }
    
    private void EvaluateAuraState()
    {
        if (!isServer) return;

        bool needsAuraRoutine = _isOwnElement || _isPleasurePoisoning;

        if (needsAuraRoutine)
        {
            if (_poisonAuraRoutine == null)
            {
                _poisonAuraRoutine = StartCoroutine(PoisonAuraRoutine());
            }
        }
        else
        {
            StopAuraRoutine();
            ResetAuraEffects();
        }
    }

    private void StopAuraRoutine()
    {
        if (_poisonAuraRoutine != null)
        {
            StopCoroutine(_poisonAuraRoutine);
            _poisonAuraRoutine = null;
        }
    }

    private void ResetAuraEffects()
    {
        if (_lastStacks != 0)
        {
            ApplyAttackSpeed(0);
            _lastStacks = 0;
        }

        ResetEnergyRegen();
    }

    private void OnBeforeTakeDamage(Damage damage, Skill skill)
    {
        if (!_isEvadePoison) return;
        if (skill == null || skill.Hero == null) return;

        Character attacker = skill.Hero;

        if (attacker == null || attacker == _owner) return;

        if (!HasPoison(attacker)) return;

        _tempEvadeBonus = 5f;

        _health.EvadeMeleeDamage += _tempEvadeBonus;
        _health.EvadeRangeDamage += _tempEvadeBonus;
        _health.ResistMagDamage += _tempEvadeBonus;
    }

    private void OnDamageDealt(Damage damage, GameObject target)
    {
        if (!_isDecreaseCooldownDamage) return;

        if (_owner == null || _owner.Abilities == null) return;

        float reduction = 1f;

        foreach (var skill in _owner.Abilities.Skills)
        {
            if (skill == null) continue;
            if (skill.Cooldown.IsActive) skill.Cooldown.Modify(-reduction);
        }
    }

    private void OnAfterDamage(Damage damage, Skill skill)
    {
        if (_tempEvadeBonus <= 0) return;

        _health.EvadeMeleeDamage -= _tempEvadeBonus;
        _health.EvadeRangeDamage -= _tempEvadeBonus;
        _health.ResistMagDamage -= _tempEvadeBonus;

        _tempEvadeBonus = 0f;
    }

    private void ApplyEnergyRegen(int stacks)
    {
        if (_owner == null || _owner.CharacterState == null) return;

        if (stacks == _lastEnergyStacks) return;

        var state = _owner.CharacterState.GetState(States.FeelingPoisoning) as FeelingPoisoningState;

        if (state == null && stacks > 0)
        {
            _owner.CharacterState.CmdAddState(States.FeelingPoisoning, 999f, 0f, gameObject, "PleasurePoisoning");
            state = _owner.CharacterState.GetState(States.FeelingPoisoning) as FeelingPoisoningState;
        }

        if (state == null) return;

        int delta = stacks - _lastEnergyStacks;

        if (delta > 0)
        {
            for (int i = 0; i < delta; i++) state.Stack(999f);
        }
        else if (delta < 0)
        {
            for (int i = 0; i < -delta; i++) state.ReduceStack();
        }

        _lastEnergyStacks = stacks;
    }

    private bool HasPoison(Character character)
    {
        var states = character.CharacterState.CurrentStates;

        foreach (var state in states)
        {
            if (state.Effects.Contains(StatusEffect.Poison))
                return true;
        }

        return false;
    }

    private IEnumerator PoisonAuraRoutine()
    {
        while (true)
        {
            int totalStacks = CalculatePoisonStacks();

            if (_isOwnElement)
            {
                if (totalStacks != _lastStacks)
                {
                    ApplyAttackSpeed(totalStacks);
                    _lastStacks = totalStacks;
                }
            }
            else if (_lastStacks != 0)
            {
                ApplyAttackSpeed(0);
                _lastStacks = 0;
            }

            if (_isPleasurePoisoning)
            {
                ApplyEnergyRegen(totalStacks);
            }
            else
            {
                ResetEnergyRegen();
            }

            yield return new WaitForSeconds(_tickRate);
        }
    }

    private void ResetEnergyRegen()
    {
        if (_owner == null || _owner.CharacterState == null) return;

        var state = _owner.CharacterState.GetState(States.FeelingPoisoning);

        if (state != null) _owner.CharacterState.RemoveState(States.FeelingPoisoning);

        _lastEnergyStacks = 0;
    }

    private int CalculatePoisonStacks()
    {
        int totalStacks = 0;

        Collider[] enemies = Physics.OverlapSphere(transform.position, _radius, _enemyLayer);

        foreach (var enemy in enemies)
        {
            if (!enemy.TryGetComponent<CharacterState>(out var state)) continue;

            totalStacks += GetPoisonStacks(state);
        }

        return totalStacks;
    }

    private int GetPoisonStacks(CharacterState state)
    {
        int stacks = 0;

        if (state.GetState(States.BindingPoison) is BindingPoisonState bindingPoisonState) stacks += bindingPoisonState.CurrentStacks;
        if (state.GetState(States.PoisonBone) is PoisonBoneState poisonBoneState) stacks += poisonBoneState.CurrentStacks;
        if (state.GetState(States.EmpathicPoisons) is EmpathicPoisonsState empathicPoisonsState) stacks += empathicPoisonsState.CurrentStacks;
        if (state.GetState(States.WitheringPoison) is WitheringPoisonState witheringPoisonState) stacks += witheringPoisonState.CurrentStacksCount;

        return stacks;
    }

    private float _currentBonus = 0f;

    private void ApplyAttackSpeed(int stacks)
    {
        float newValue = stacks * _attackSpeedPerStack;

        if (_owner == null) return;

        if (!_owner.AttributeSystem[CharacterAttributeName.CastSpeed].Modifiers.Contains(_castSpeedModifier))
        {
            _castSpeedModifier.Source = this;
            _owner.AttributeSystem[CharacterAttributeName.CastSpeed].AddModifier(_castSpeedModifier);
        }

        if (Mathf.Approximately(newValue, _castSpeedModifier.Value)) return;

        _castSpeedModifier.Value = newValue;
    }
}
