using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CoolingAura : AuraStateHandler
{
    [Header("Magic Water Aura Settings")]
    [SerializeField] private float _buffDuration = -1f;
    
    [SerializeField] private float _minRadius = 1f;
    [SerializeField] private float _maxRadius = 5f;
    [SerializeField] private float _expandTime = 5f;

    private int _stackPerUnit = 6;
    private float _currentRadius;
    private float _elapsedTime;
    private Coroutine _expandCoroutine;

    protected override float GetCurrentRadius() => _currentRadius;

    protected override void OnAuraEnabled()
    {
        _currentRadius = _minRadius;
        _elapsedTime = 0f;

        if (_expandCoroutine != null)
            StopCoroutine(_expandCoroutine);
        
        _expandCoroutine = StartCoroutine(ExpandRadiusRoutine());
    }

    protected override void OnAuraDisabled()
    {
        if (_expandCoroutine != null)
        {
            StopCoroutine(_expandCoroutine);
            _expandCoroutine = null;
        }
        
        RemoveEffectsFromAllTargets();
    }

    private IEnumerator ExpandRadiusRoutine()
    {
        while (IsActive && _elapsedTime < _expandTime)
        {
            _elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsedTime / _expandTime);
            _currentRadius = Mathf.Lerp(_minRadius, _maxRadius, progress);

            yield return null;
        }

        if (IsActive)
            _currentRadius = _maxRadius;
    }

    protected override void OnTargetEnter(Character target)
    {
        for (int i = 0; i < _stackPerUnit; i++)
        {
            CmdApplyStateToTarget(target.gameObject, States.Cooling, _buffDuration, Schools.Water, _owner.gameObject, nameof(CoolingAura));
        }
        
    }

    protected override void OnTargetExit(Character target)
    {
        CmdRemoveStateFromTarget(target.gameObject, States.Cooling);
    }

    private void OnDestroy()
    {
        if (_expandCoroutine != null)
            StopCoroutine(_expandCoroutine);
    }
}

public class CoolingDamaged : AbstractCharacterState
{
    private List<StatusEffect> _effects = new List<StatusEffect>() { StatusEffect.Others };

    private float _physResistPercent = 0.1f;
    private float _savedPhysResist;

    public override States State => States.CoolingDamaged;
    public override StateType Type => StateType.Magic;
    public override BaffDebaff BaffDebaff => BaffDebaff.Baff;
    public override List<StatusEffect> Effects => _effects;

    protected override void OnEnterState(CharacterState character, float durationToExit, float damageToExit,
        Character personWhoMadeBuff, string skillName)
    {
        _savedPhysResist = character.Character.Health.DefPhysDamage;
        character.Character.Health.SetPhysicDef(
            _savedPhysResist + _savedPhysResist * _physResistPercent);

        character.Character.Health.DamageTaken += OnDamageTaken;
    }

    private void OnDamageTaken(Damage damage, Skill skill)
    {
        if (skill == null) return;
        if (damage.Type != DamageType.Physical) return;
        if (damage.PhysicAttackType != AttackRangeType.MeleeAttack) return;

        skill.Hero.CharacterState.AddState(States.Cooling, 6f, 0,
            characterState.Character.gameObject, nameof(Cooling));
    }

    public override void OnUpdateState()
    {
    }

    protected override void OnExitState()
    {
        if (characterState?.Character != null)
        {
            characterState.Character.Health.SetPhysicDef(_savedPhysResist);
            characterState.Character.Health.DamageTaken -= OnDamageTaken;
        }

        _savedPhysResist = 0f;
    }
}
