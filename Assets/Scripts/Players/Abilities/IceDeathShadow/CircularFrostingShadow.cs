using Mirror;
using System.Collections;
using UnityEngine;

public class CircularFrostingShadow : NetworkBehaviour
{
    [SerializeField] private ParticleSystemController _particleSystem;

    private Character _owner;
    private float _remainingDelay;
    private float _radius;

    private Coroutine _routine;

    public void Init(Character owner, float remainingDelay, float radius)
    {
        _owner = owner;
        _remainingDelay = remainingDelay;
        _radius = radius;
    }

    public void StartShadowFrost()
    {
        if (_owner == null) return;
        if (_routine != null) return;

        _routine = StartCoroutine(ShadowRoutine());
    }

    private IEnumerator ShadowRoutine()
    {
        if (_remainingDelay > 0f) yield return new WaitForSeconds(_remainingDelay);

        if (_owner == null || _owner.IsDead) yield break;

        ExecuteFrostingLogic();
        EffectFrosting();
    }

    private void ExecuteFrostingLogic()
    {
        Collider[] hits = Physics.OverlapSphere(_owner.transform.position, _radius);

        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out Character target)) continue;
            if (target == _owner) continue;

            target.CharacterState.AddState(States.Frosting, 4f, 0, _owner.gameObject, "CircularFrostingShadow");
        }
    }

    private void DestroyInvoke() => NetworkServer.Destroy(gameObject);

    [Server]
    private void EffectFrosting()
    {
        RpcPlayEffect();
        Invoke("DestroyInvoke", 0.5f);
    }

    [ClientRpc]
    private void RpcPlayEffect()
    {
        _particleSystem?.Play();
    }

    public override void OnStopServer()
    {
        if (_routine != null) StopCoroutine(_routine);
    }
}