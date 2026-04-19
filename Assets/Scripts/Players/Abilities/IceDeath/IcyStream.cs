using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcyStream : Skill
{
    [Header("Stream Settings")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private float _streamLength = 6f;
    [SerializeField] private float _streamWidth = 2f;
    [SerializeField] private float _streamHeight = 2f;
    [SerializeField] private Transform _streamStartPoint;
    [SerializeField] private LayerMask _targetsLayers;

    private Energy _energy;
    private Character _cachedTarget;

    private Coroutine _streamCoroutine;
    private bool _isStreaming;

    private const int MaxTicks = 7;

    protected override bool IsCanCast => !_isStreaming && IsCanCastCheck();
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    private bool IsCanCastCheck()
    {
        var target = Targeting.GetTarget()?.Character;
        if (target == null)
            return false;

        return Vector3.Distance(target.transform.position, Hero.transform.position) <= AreaInfo.Radius;
    }

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        if (_energy == null)
            _energy = (Energy)Hero.Resources[ResourceType.Energy];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        if (_energy == null)
            _energy = (Energy)Hero.Resources[ResourceType.Energy];

        while (Targeting.GetTarget()?.Character == null)
            yield return null;

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.AddTarget(Targeting.GetTarget().Targetable);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_isStreaming)
            yield break;

        _cachedTarget = Targeting.GetTarget()?.Character;
        if (_cachedTarget == null)
            yield break;

        StartStream();

        yield return null;
    }

    private void StartStream()
    {
        _isStreaming = true;
        _streamCoroutine = StartCoroutine(StreamRoutine());
    }

    private IEnumerator StreamRoutine()
    {
        for (int tick = 1; tick <= MaxTicks; tick++)
        {
            yield return new WaitForSeconds(_tickInterval);

            ApplyTick(tick);
        }

        _isStreaming = false;
    }

    private void ApplyTick(int tickNumber)
    {
        List<IDamageable> targets = GetTargetsInStream();

        foreach (var damageable in targets)
        {
            if (damageable == null)
                continue;

            Damage damage = new Damage
            {
                Value = tickNumber,
                Type = Info.DamageType,
                School = Schools.Water
            };

            CmdApplyDamage(damage, damageable.gameObject);
            ApplyFrozen(damageable.gameObject);
        }
    }

    private List<IDamageable> GetTargetsInStream()
    {
        List<IDamageable> result = new();

        Vector3 startPos = _streamStartPoint != null
            ? _streamStartPoint.position
            : Hero.transform.position + Vector3.up * 0.5f;

        Vector3 direction = (_cachedTarget.transform.position - startPos);
        direction.y = 0f;
        direction.Normalize();

        Vector3 center = startPos + direction * (_streamLength * 0.5f);

        Vector3 halfExtents = new Vector3(
            _streamWidth * 0.5f,
            _streamHeight * 0.5f,
            _streamLength * 0.5f
        );

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.LookRotation(direction),
            _targetsLayers
        );

        foreach (var hit in hits)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            Character character = hit.GetComponentInParent<Character>();
            if (character == null)
                continue;

            if (character == Hero)
                continue;

            if (!result.Contains(damageable))
                result.Add(damageable);
        }

        return result;
    }

    private void ApplyFrozen(GameObject target)
    {
        Character character = target.GetComponent<Character>();
        if (character == null)
            return;

        CharacterState state = character.GetComponent<CharacterState>();
        if (state == null)
            return;

        state.CmdAddState(States.Frozen, 0.3f, 0, Hero.gameObject, Name);
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();

        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

        _isStreaming = false;
    }
}