using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IcyStream : Skill
{
    [Header("Stream Settings")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private Transform _streamStartPoint;

    [Header("Visual")]
    [SerializeField] private GameObject _icyStreamPrefab;

    private Character _cachedTarget;
    private Coroutine _streamCoroutine;
    private GameObject _activeEffect;

    private bool _isStreaming;
    private const int MaxTicks = 7;

    protected override bool IsCanCast => !_isStreaming && Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null)
                {
                    Targeting.SetTarget(temp);

                    break;
                }
            }

            yield return null;
        }

        var target = Targeting.GetTarget()?.Character;

        if (target != null)
        {
            targetInfo.AddTarget(target);
            callbackDataSaved(targetInfo);
        }
    }

    protected override IEnumerator CastJob()
    {
        _cachedTarget = Targeting.GetTarget()?.Character;
        if (_cachedTarget == null)
            yield break;

        _isStreaming = true;

        CmdSpawnIcyStreamEffect(
            _streamStartPoint.gameObject,
            _cachedTarget.gameObject
        );

        _streamCoroutine = StartCoroutine(StreamRoutine());

        yield return _streamCoroutine;

        CmdDestroyIcyStreamEffect();
        _isStreaming = false;
    }

    private IEnumerator StreamRoutine()
    {
        for (int tick = 1; tick <= MaxTicks; tick++)
        {
            yield return new WaitForSeconds(_tickInterval);
            ApplyTick(tick);
        }
    }

    private void ApplyTick(int tickNumber)
    {
        Collider[] hits = Physics.OverlapSphere(
            _cachedTarget.transform.position,
            2f,
            _targetsLayers
        );

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out IDamageable damageable))
                continue;

            if (!hit.TryGetComponent(out Character character))
                continue;

            if (character == Hero)
                continue;

            Damage damage = new Damage
            {
                Value = tickNumber,
                Type = Info.DamageType
            };

            CmdApplyDamage(damage, character.gameObject);

            character.CharacterState.AddState(States.Frozen, 0.3f, 0, Hero.gameObject, Name);
        }
    }

    [Command]
    private void CmdSpawnIcyStreamEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_icyStreamPrefab == null || startPoint == null || targetPoint == null)
            return;

        GameObject effectInstance =
            Instantiate(_icyStreamPrefab, startPoint.transform.position, Quaternion.identity);

        NetworkServer.Spawn(effectInstance);

        RpcInitEffects(effectInstance, startPoint, targetPoint);

        _activeEffect = effectInstance;
    }

    [Command]
    private void CmdDestroyIcyStreamEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            _activeEffect = null;
        }
    }

    [ClientRpc]
    private void RpcInitEffects(GameObject effectGameObject, GameObject startPoint, GameObject targetPoint)
    {
        if (effectGameObject == null)
            return;

        PullingHealthEffect[] effects =
            effectGameObject.GetComponentsInChildren<PullingHealthEffect>();

        foreach (var effect in effects)
        {
            effect.Initialize(startPoint, targetPoint);
            effect.Activate();
        }
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();

        if (_streamCoroutine != null)
            StopCoroutine(_streamCoroutine);

        CmdDestroyIcyStreamEffect();
        _isStreaming = false;
    }
}