using Mirror;
using System.Collections;
using UnityEngine;

public class IcyStreamShadow : NetworkBehaviour
{
    [Header("Stream Settings")]
    [SerializeField] private float _tickInterval = 0.3f;
    [SerializeField] private float _streamLength = 4f;
    [SerializeField] private float _streamWidth = 1f;
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
        if (_isStreaming) return;

        _isStreaming = true;

        SpawnIcyStreamEffect(_streamStartPoint.gameObject, _cachedTarget != null ? _cachedTarget.gameObject : null);

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
            yield return new WaitForSeconds(_tickInterval);
            ApplyTick(tick);
        }

        CleanupAndDestroy();
    }

    [Server]
    private void CleanupAndDestroy()
    {
        StopShadowStream();
        NetworkServer.Destroy(gameObject);
    }

    private void ApplyTick(int tickNumber)
    {
        if (!isServer) return;

        Damage damage = new Damage
        {
            Value = tickNumber,
            Type = _damageType,
            School = Schools.Water
        };

        Vector3 start = _streamStartPoint != null ? _streamStartPoint.position : transform.position;
        Vector3 end = start + transform.forward * _streamLength;

        Collider[] hits = Physics.OverlapCapsule(start, end, _streamWidth * 0.5f);

        foreach (var col in hits)
        {
            if (col.TryGetComponent<Character>(out var target))
            {
                if (target == _owner || target.IsDead) continue;

                ApplyDamage(target, damage);
                ApplyCooling(target);
            }
        }
    }

    private void ApplyDamage(Character target, Damage damage)
    {
        if (target == null || target.IsDead) return;

        if (_iceShadowObject != null && _iceShadowObject.SkillShadow != null)
        {
            _iceShadowObject.SkillShadow.ApplyDamage(damage, target.gameObject);
        }
        else
        {
            target.Health.TryTakeDamage(ref damage, null);
        }
    }

    private void ApplyCooling(Character target)
    {
        if (target == null || target.IsDead) return;

        target.CharacterState.AddState(
            States.Cooling, 
            12f, 
            0, 
            _owner != null ? _owner.gameObject : gameObject, 
            "IcyStreamShadow"
        );
    }

    public override void OnStopServer()
    {
        StopShadowStream();
    }

    public override void OnStopClient()
    {
        if (_activeEffect != null) Destroy(_activeEffect);
    }

    [Server]
    private void SpawnIcyStreamEffect(GameObject startPoint, GameObject targetPoint)
    {
        if (_icyStreamPrefab == null || startPoint == null) return;

        GameObject effect = Instantiate(_icyStreamPrefab, startPoint.transform.position, transform.rotation);

        NetworkServer.Spawn(effect);

        if (targetPoint != null)
        {
            RpcInitEffects(effect, startPoint, targetPoint);
        }

        _activeEffect = effect;
    }

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