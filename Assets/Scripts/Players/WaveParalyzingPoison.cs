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
    [SerializeField] private ParticleSystemController _particleSystem;

    [Header("Effect")]
    [SerializeField] private float _paralyzingPoisonDuration = 2f;

    private float _previousRadius;
    private float _currentRadius;
    private HashSet<Character> _affectedTargets = new();

    protected override int AnimTriggerCast => Animator.StringToHash("Spell");
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo)
    {

    }

    public void WaveParalyzingPoisonCast()
    {
        AnimStartCastCoroutine();
        _particleSystem.Play();
    }

    public void WaveParalyzingPoisonEnded()
    {
        AnimCastEnded();
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

        while (_currentRadius <= AreaInfo.Radius)
        {
            ExpandWave(origin);

            _previousRadius = _currentRadius;
            _currentRadius += _radiusStep;

            yield return new WaitForSeconds(_stepTime);
        }

        AnimCastEnded();
    }

    protected override void ClearData()
    {
        _affectedTargets.Clear();
        _currentRadius = 0;
        _previousRadius = 0;
    }

    private void ExpandWave(Vector3 origin)
    {
        Collider[] hits = Physics.OverlapSphere(origin, _currentRadius, _targetsLayers);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (target == _player) continue;

            float distance = Vector3.Distance(origin, target.transform.position);

            if (distance <= _previousRadius || distance > _currentRadius) continue;
            if (_affectedTargets.Contains(target)) continue;

            _affectedTargets.Add(target);

            ApplyStun(target);
        }
    }

    private void ApplyStun(Character target)
    {
        target.CharacterState.AddState(States.ParalyzingPoison, _paralyzingPoisonDuration, 0, _player.gameObject, Name);
    }
}