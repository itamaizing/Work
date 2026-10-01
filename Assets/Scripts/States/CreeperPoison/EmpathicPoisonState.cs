using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmpathicPoisonsState : StateStackingRefreshing, IDamageable
{
    private PoisonCloudStateStacking _poisonCloud;
    private Character _player;
    private DamageType _damageType;
    private AttackRangeType _attackRangeType;

    private int _maxStacks = 8;
    private float _baseEvasionValue = 0.03f;

    private float _radiusCloud;

    private float _timeBeforeReductionDebuff;
    private float _startTimeBeforeReductionDebuff = 1.0f;

    private float _baseDuration;

    private Vector3 _playerPosition;
    private Vector3 _characterPosition;

    private bool _isInPoisonCloud;

    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Poison };

    public int CurrentStacks { get => CurrentStacksCount; set => CurrentStacksCount = value; }
    public float StacksDuration { get => RemainingDuration; }

    public event Action<Damage, Skill> DamageTaken;
    public override States State => States.EmpathicPoisons;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;

    public override List<StatusEffect> Effects => _effects;

    public Transform transform => throw new NotImplementedException();
    public GameObject gameObject => throw new NotImplementedException();

    public override void Apply(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        SetMaxStacks(_maxStacks);

        _timeBeforeReductionDebuff = _startTimeBeforeReductionDebuff;
        _player = character.Character;

        _player.Health.Shields.Add(this);
        _poisonCloud = (PoisonCloudStateStacking)_player.CharacterState.GetState(States.PoisonCloud);

        _baseDuration = durationToExit;

        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
        }

        ApplyEvasionBonus();
    }

    public void ShowPhantomValue(Damage value) { }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (CurrentStacksCount > 0)
        {
            if (damage.Type == DamageType.Physical)
            {
                var attrs = _player.AttributeSystem;
                float evade = damage.PhysicAttackType == AttackRangeType.MeleeAttack
                    ? attrs[CharacterAttributeName.EvasionPhysicalMelee].GetValue()
                    : attrs[CharacterAttributeName.EvasionPhysicalRange].GetValue();

                if (UnityEngine.Random.Range(0.0f, 100.0f) <= evade)
                {
                    damage.Value = 0;
                    return true;
                }
                return false;
            }
        }
        return true;
    }

    public override void UpdateState()
    {
        _playerPosition = _player.transform.position;
        _characterPosition = characterState.transform.position;

        if (CurrentStacksCount <= 0)
        {
            ExitState();
        }

        _timeBeforeReductionDebuff -= Time.deltaTime;
        if (_timeBeforeReductionDebuff <= 0)
        {
            CheckIfInPoisonCloud(_playerPosition, _characterPosition);
            if (_isInPoisonCloud)
            {
                ApplyEvasionBonus();
            }
            _timeBeforeReductionDebuff = _startTimeBeforeReductionDebuff;
        }
    }

    public override void ExitState()
    {
        ResetValues();
        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        if (CurrentStacksCount < MaxStacksCount)
        {
            CurrentStacksCount++;
        }
        RemainingDuration = _baseDuration;
        ApplyEvasionBonus();
        return true;
    }

    private void ApplyEvasionBonus()
    {
        if (_player == null) return;

        float bonus = _baseEvasionValue * CurrentStacksCount;

        var attrs = _player.AttributeSystem;
        attrs[CharacterAttributeName.EvasionPhysicalMelee].RemoveBySource(this);
        attrs[CharacterAttributeName.EvasionPhysicalRange].RemoveBySource(this);

        attrs[CharacterAttributeName.EvasionPhysicalMelee].AddModifier(new AttributeModifier(bonus, ModifierType.Flat, this));
        attrs[CharacterAttributeName.EvasionPhysicalRange].AddModifier(new AttributeModifier(bonus, ModifierType.Flat, this));
    }

    private void CheckIfInPoisonCloud(Vector3 playerPos, Vector3 characterPos)
    {
        float distance = Vector3.Distance(playerPos, characterPos);
        _isInPoisonCloud = distance <= _radiusCloud;
    }

    private void ResetValues()
    {
        CurrentStacksCount = 0;
        _baseDuration = 0;
        RemainingDuration = 0;

        _player?.AttributeSystem[CharacterAttributeName.EvasionPhysicalMelee].RemoveBySource(this);
        _player?.AttributeSystem[CharacterAttributeName.EvasionPhysicalRange].RemoveBySource(this);
    }
}