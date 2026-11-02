using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class WaveParalyzingPoison : Skill
{
    [SerializeField] private Character _player;

    [Header("Wave Settings")]
    [SerializeField] private float _stepTime = 0.2f;
    [SerializeField] private float _radiusStep = 1f;
    [SerializeField] private ParticleSystemController _particleSystem;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;

    [Header("Effect")]
    [SerializeField] private float _paralyzingPoisonDuration = 1f;

    private float _previousRadius;
    private float _currentRadius;
    private HashSet<Character> _affectedTargets = new();
    
    private const float DurationPerPoisonBoneStack = 0.5f;

    protected override int AnimTriggerCast => Animator.StringToHash("Spell");
    protected override int AnimTriggerPrepare => 0;

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

    protected override IEnumerator CastJob()
    {
        Vector3 origin = _player.transform.position;
        float maxRadius = AreaInfo.Radius;

        _currentRadius = 0f;
        _previousRadius = 0f;
        _affectedTargets.Clear();

        while (_currentRadius < maxRadius)
        {
            yield return new WaitForSeconds(_stepTime);

            _previousRadius = _currentRadius;
            _currentRadius = Mathf.Min(
                _currentRadius + _radiusStep,
                maxRadius);

            ExpandWave(origin);
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
        Collider[] hits = Physics.OverlapSphere(origin, _currentRadius, Targeting.InteractionMask);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target))
                continue;

            if (target.IsDead)
                continue;

            if (_player.RelationTo(target) != CharacterRelation.Enemy)
                continue;

            float distance = Vector3.Distance(origin, target.transform.position);

            if (distance <= _previousRadius || distance > _currentRadius)
                continue;

            if (!_affectedTargets.Add(target))
                continue;

            CmdApplyParalyzingPoison(target.gameObject);
        }
    }

    [Command]
    private void CmdApplyParalyzingPoison(GameObject targetObject)
    {
        if (_player == null || _player.IsDead || targetObject == null)
            return;

        if (!targetObject.TryGetComponent<Character>(out var target))
            return;

        if (target.IsDead)
            return;

        if (_player.RelationTo(target) != CharacterRelation.Enemy)
            return;

        CharacterState targetStates = target.CharacterState;

        if (targetStates == null)
            return;

        int poisonBoneStacks = targetStates.CheckStateStacks(States.PoisonBone);

        float duration = _paralyzingPoisonDuration + DurationPerPoisonBoneStack * poisonBoneStacks;

        targetStates.AddState(States.ParalyzingPoison, duration, 0f, Schools.Physical, _player.gameObject, Name);

        if (_creeperPoisonAura != null && _creeperPoisonAura.IsFeelingPoisoning)
        {
            _player.CharacterState.AddState(States.FeelingPoisoning, 2f, 0f, Schools.None, _player.gameObject, Name);
        }
    }
}