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

    private Health _health;
    private Character _owner;

    private float _tempEvadeBonus = 0f;

    #region Talent
    private bool _isFeelingPoisoning = false;
    private bool _isEvadePoison = false;
    private bool _isOwnElement = false;

    public bool IsFeelingPoisoning { get => _isFeelingPoisoning; set => _isFeelingPoisoning = value; }

    public void OwnElement(bool value) => _isOwnElement = value;
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

        _poisonAuraRoutine = StartCoroutine(PoisonAuraRoutine());
    }

    public override void OnStopServer()
    {
        if (_health != null)
        {
            _health.OnBeforeTakeDamage -= OnBeforeTakeDamage;
            _health.DamageTaken -= OnAfterDamage;
        }

        if (_poisonAuraRoutine != null) StopCoroutine(_poisonAuraRoutine);
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

    private void OnAfterDamage(Damage damage, Skill skill)
    {
        if (_tempEvadeBonus <= 0) return;

        _health.EvadeMeleeDamage -= _tempEvadeBonus;
        _health.EvadeRangeDamage -= _tempEvadeBonus;
        _health.ResistMagDamage -= _tempEvadeBonus;

        _tempEvadeBonus = 0f;
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

            if (totalStacks != _lastStacks)
            {
                ApplyAttackSpeed(totalStacks);
                _lastStacks = totalStacks;
            }

            yield return new WaitForSeconds(_tickRate);
        }
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

        if (state.GetState(States.BindingPoison) is BindingPoisonState b)
            stacks += b.CurrentStacks;

        if (state.GetState(States.PoisonBone) is PoisonBoneState pb)
            stacks += pb.CurrentStacks;

        if (state.GetState(States.EmpathicPoisons) is EmpathicPoisonsState ep)
            stacks += ep.CurrentStacks;

        if (state.GetState(States.WitheringPoison) is WitheringPoisonState wp)
            stacks += wp.CurrentStacks;

        return stacks;
    }

    private float _currentBonus = 0f;

    private void ApplyAttackSpeed(int stacks)
    {
        float newBonus = stacks * _attackSpeedPerStack;

        if (Mathf.Approximately(newBonus, _currentBonus)) return;

        float delta = newBonus - _currentBonus;

        if (_owner == null || _owner.Abilities == null) return;

        var skills = _owner.Abilities.Skills;

        foreach (var skill in skills)
        {
            if (skill == null) continue;

            if (skill.Info.DamageType != DamageType.Physical) continue;

            if (delta > 0) skill.Buff.CastSpeed.IncreasePercentage(delta);
            else skill.Buff.CastSpeed.ReductionPercentage(-delta);
        }

        _currentBonus = newBonus;
    }
}
