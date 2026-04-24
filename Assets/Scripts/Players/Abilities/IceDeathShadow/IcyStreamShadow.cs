using Mirror;
using System.Collections;
using UnityEngine;

public class IcyStreamShadow : NetworkBehaviour
{
    [Header("Stream Settings")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private Transform _streamStartPoint;

    [Header("Visual")]
    [SerializeField] private GameObject _icyStreamPrefab;

    [Header("Damage")]
    [SerializeField] private DamageType _damageType;

    [SerializeField] private IceShadowObject _iceShadowObject;

    private Character _owner;
    private Character _cachedTarget;

    private Coroutine _streamCoroutine;
    private GameObject _activeEffect;

    private bool _isStreaming;

    private int _startTick = 1;
    private int _maxTicks = 7;

    private const float FrostEnergyCoolingBonusPerStack = 1f;

    public void Init(Character owner, Character target, int startTick, int maxTicks)
    {
        _owner = owner;
        _cachedTarget = target;
        _startTick = Mathf.Max(1, startTick);
        _maxTicks = Mathf.Max(_startTick, maxTicks);
    }

    public void StartShadowStream()
    {
         if (!isServer) return;
        if (_cachedTarget == null) return;
        if (_isStreaming) return;

        _isStreaming = true;

        SpawnIcyStreamEffect(_streamStartPoint.gameObject, _cachedTarget.gameObject);

        _streamCoroutine = StartCoroutine(StreamRoutine());
    }

    public void StopShadowStream()
    {
        if (!isServer) return;

        if (_streamCoroutine != null)
        {
            StopCoroutine(_streamCoroutine);
            _streamCoroutine = null;
        }

        DestroyIcyStreamEffect();

        _isStreaming = false;
    }

    private IEnumerator StreamRoutine()
    {
        for (int tick = _startTick; tick <= _maxTicks; tick++)
        {
            if (_cachedTarget == null || _cachedTarget.IsDead) break;

            yield return new WaitForSeconds(_tickInterval);

            ApplyTick(tick);
        }

        StopShadowStream();
    }

    private void ApplyTick(int tickNumber)
    {
        if (!isServer) return;
        if (_cachedTarget == null) return;
        if (_cachedTarget.IsDead) return;

        Damage damage = new Damage
        {
            Value = tickNumber,
            Type = _damageType
        };

        ApplyDamage(_cachedTarget, damage);
        ApplyCooling(_cachedTarget);
    }

    private void ApplyDamage(Character target, Damage damage)
    {
        if (target == null || target.IsDead) return;

        target.Health.TryTakeDamage(ref damage, _iceShadowObject.SkillShadow);
    }

    private void ApplyCooling(Character target)
    {
        if (target == null) return;

        bool hasFrostEnergy = target.CharacterState.CheckForState(States.FrostEnergy);

        int currentStacks = target.CharacterState.CheckStateStacks(States.Cooling);
        int stacksAfter = currentStacks + 1;

        if (hasFrostEnergy)
        {
            float bonusDamage = stacksAfter * FrostEnergyCoolingBonusPerStack;

            Damage bonus = new Damage
            {
                Value = bonusDamage,
                Type = DamageType.Magical
            };

            target.Health.TryTakeDamage(ref bonus, _iceShadowObject.SkillShadow);
        }

        target.CharacterState.AddState(States.Cooling, 12f, 0, _owner.gameObject, "IcyStreamShadow");
    }

    public override void OnStopServer()
    {
        StopShadowStream();
    }

    public override void OnStopClient()
    {
        if (_activeEffect != null)
            Destroy(_activeEffect);
    }

    [Server]
    private void SpawnIcyStreamEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_icyStreamPrefab == null || startPoint == null || targetPoint == null) return;

        GameObject effect = Instantiate( _icyStreamPrefab, startPoint.transform.position, Quaternion.identity);

        NetworkServer.Spawn(effect);

        RpcInitEffects(effect, startPoint, targetPoint);

        _activeEffect = effect;
    }

    [Server]
    private void DestroyIcyStreamEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            _activeEffect = null;
        }
    }

    [ClientRpc]
    private void RpcInitEffects(GameObject effect, GameObject startPoint, GameObject targetPoint)
    {
        if (effect == null) return;

        var visuals = effect.GetComponentsInChildren<PullingHealthEffect>();

        foreach (var visual in visuals)
        {
            visual.Initialize(startPoint, targetPoint);
            visual.Activate();
        }
    }
}