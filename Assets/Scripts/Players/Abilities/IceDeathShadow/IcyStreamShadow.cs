using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class IcyStreamShadow : Skill
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
    private int _startTick = 1;
    private int _maxTicksOverride = 7;

    private const float FrostEnergyCoolingBonusPerStack = 1f;

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
        if (_cachedTarget == null) yield break;

        _isStreaming = true;

        CmdSpawnIcyStreamEffect(_streamStartPoint.gameObject, _cachedTarget.gameObject);

        _streamCoroutine = StartCoroutine(StreamRoutine());

        yield return _streamCoroutine;

        CmdDestroyIcyStreamEffect();
        _isStreaming = false;
    }

    private IEnumerator StreamRoutine()
    {
        for (int tick = _startTick; tick <= _maxTicksOverride; tick++)
        {
            yield return new WaitForSeconds(_tickInterval);

            ApplyTick(tick);
        }
    }

    public void InitFromStreamState(Character target, int startTick, int maxTicks)
    {
        _cachedTarget = target;
        _startTick = startTick;
        _maxTicksOverride = maxTicks;
    }

    public void StartShadowStream()
    {
        if (_cachedTarget == null) return;

        _streamCoroutine = StartCoroutine(StreamRoutine());
    }

    private void ApplyTick(int tickNumber)
    {
        Collider[] hits = Physics.OverlapSphere(_cachedTarget.transform.position, 2f, _targetsLayers);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent(out IDamageable damageable)) continue;
            if (!hit.TryGetComponent(out Character character)) continue;
            if (character == Hero) continue;

            Damage damage = new Damage
            {
                Value = tickNumber,
                Type = Info.DamageType
            };

            CmdApplyDamage(damage, character.gameObject);
            CmdAddFrozen(character);
        }
    }

    private void ApplyCoolingWithFrostEnergyBonus(Character target)
    {
        bool hasFrostEnergy = target.CharacterState.CheckForState(States.FrostEnergy);

        int currentStacks = target.CharacterState.CheckStateStacks(States.Cooling);
        int stacksAfterApply = currentStacks + 1;

        if (hasFrostEnergy)
        {
            float bonusDamage = stacksAfterApply * FrostEnergyCoolingBonusPerStack;

            Damage bonus = new Damage
            {
                Value = bonusDamage,
                Type = DamageType.Magical
            };

            target.Health.TryTakeDamage(ref bonus, this);
        }

        target.CharacterState.AddState(States.Cooling, 12f, 0, Hero.gameObject, Name);
    }

    [Command]
    private void CmdSpawnIcyStreamEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_icyStreamPrefab == null || startPoint == null || targetPoint == null)
            return;

        GameObject effectInstance = Instantiate(_icyStreamPrefab, startPoint.transform.position, Quaternion.identity);

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

    [Command]
    private void CmdAddFrozen(Character character)
    {
        if (character == null) return;

        ApplyCoolingWithFrostEnergyBonus(character);
    }

    [ClientRpc]
    private void RpcInitEffects(GameObject effectGameObject, GameObject startPoint, GameObject targetPoint)
    {
        if (effectGameObject == null) return;

        PullingHealthEffect[] effects = effectGameObject.GetComponentsInChildren<PullingHealthEffect>();

        foreach (var effect in effects)
        {
            effect.Initialize(startPoint, targetPoint);
            effect.Activate();
        }
    }

    protected override void ClearData()
    {
        Targeting.ClearTarget();

        if (_streamCoroutine != null) ;
    }
}
