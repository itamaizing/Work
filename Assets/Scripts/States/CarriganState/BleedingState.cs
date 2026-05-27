using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class BleedingState : RefreshingState
{
    private const float MaxDuration = 21f;

    private Character _target;

    private float _durationRemaining;

    private float _timeBetweenAttack;
    private float _startTimeBetweenAttack = 1.0f;

    private List<StatusEffect> _effects = new();

    public override States State => States.Bleeding;
    public override StateType Type => StateType.Physical;
    public override BaffDebaff BaffDebaff => BaffDebaff.Debaff;
    public override List<StatusEffect> Effects => _effects;

    public override float RemainingDuration
    {
        get => _durationRemaining;
        set => _durationRemaining = value;
    }

    public BleedingState()
    {
        MaxStacksCount = 1;
        currentStacksCount = 0;
    }

    public override void EnterState(CharacterState character, float durationToExit, float damageToExit, Character personWhoMadeBuff, string skillName)
    {
        characterState = character;
        _target = character.Character;
        _durationRemaining = durationToExit;
        duration = _durationRemaining;

        _timeBetweenAttack = _startTimeBetweenAttack;

        if (_target != null && _target.Health != null) _target.Health.IsDot = true;
    }

    public override void UpdateState()
    {
        _durationRemaining -= Time.deltaTime;
        duration = _durationRemaining;

        _timeBetweenAttack -= Time.deltaTime;

        if (_timeBetweenAttack <= 0)
        {
            if (NetworkServer.active) BleedingDamage();

            if (_target != null && _target.Health != null && _target.Health.barCharacter != null)
            {
                float previewDamage = _target.Health.MaxValue * 0.003f;
                _target.Health.barCharacter.PreviewDoTTick(previewDamage);
            }

            _timeBetweenAttack = _startTimeBetweenAttack;
        }

        if (_durationRemaining <= 0) ExitState();
    }

    public override void ExitState()
    {
        if (_target != null && _target.Health != null) _target.Health.IsDot = false;

        _durationRemaining = 0f;
        currentStacksCount = 0;
        _target = null;

        characterState.RemoveState(this);
    }

    public override bool Stack(float time)
    {
        _durationRemaining = Mathf.Min(_durationRemaining + 7f, MaxDuration);
        duration = _durationRemaining;

        if (characterState != null && characterState.StateIcons != null) characterState.StateIcons.ActivateIco(State, _durationRemaining, 1, false, 1);
        return true;
    }

    [Server]
    private void BleedingDamage()
    {
        if (_target == null || _target.IsDead) return;

        float bleedDamage = _target.Health.MaxValue * 0.003f;

        Damage damage = new Damage()
        {
            Value = bleedDamage,
            Type = DamageType.Physical,
        };

        _target.Health.TryTakeDamage(ref damage, null);
    }
}