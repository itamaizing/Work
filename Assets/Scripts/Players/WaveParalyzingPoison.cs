using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveParalyzingPoison : Skill
{
    [SerializeField] private Character _player;

    [Header("Wave Settings")]
    [SerializeField] private float _waveDuration = 1.5f;
    [SerializeField] private float _stepTime = 0.2f;
    [SerializeField] private float _radiusStep = 1f;

    [Header("Effect")]
    [SerializeField] private float _paralyzingPoisonDuration = 2f;

    private float _currentRadius;
    private HashSet<Character> _affectedTargets = new();

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {

    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callback)
    {
        while (!GetMouseButton) yield return null;

        TargetInfo info = new TargetInfo();
        info.AddTarget(Hero);

        callback(info);
        yield break;
    }

    protected override IEnumerator CastJob()
    {
        Vector3 origin = _player.transform.position;

        _currentRadius = 0;
        _affectedTargets.Clear();

        float elapsed = 0f;

        while (elapsed < _waveDuration)
        {
            ExpandWave(origin);

            yield return new WaitForSeconds(_stepTime);

            elapsed += _stepTime;
            _currentRadius += _radiusStep;
        }
    }

    protected override void ClearData()
    {
        _affectedTargets.Clear();
        _currentRadius = 0;
    }

    private void ExpandWave(Vector3 origin)
    {
        Collider[] hits = Physics.OverlapSphere(
            origin,
            _currentRadius,
            _targetsLayers
        );

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target))
                continue;

            if (_affectedTargets.Contains(target))
                continue;

            _affectedTargets.Add(target);

            ApplyStun(target);
        }
    }

    private void ApplyStun(Character target)
    {
        target.CharacterState.AddState(States.ParalyzingPoison, _paralyzingPoisonDuration, 0, _player.gameObject, Name);
    }
}