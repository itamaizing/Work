using Mirror;
using System.Collections;
using UnityEngine;

public class CircularFrostingShadow : NetworkBehaviour
{
    [SerializeField] private ParticleSystemController _particleSystem;

    private Character _owner;
    //private float _remainingDelay;
    private float _radius;

    private Coroutine _routine;

    public void Init(Character owner, float remainingDelay, float radius)
    {
        _owner = owner;
        //_remainingDelay = remainingDelay;
        _radius = radius;
    }

    public override void OnStartServer()
    {
        if (_routine == null) _routine = StartCoroutine(ShadowRoutine());
    }

    private IEnumerator ShadowRoutine()
    {
        yield return new WaitForSeconds(1);

        if (_owner == null || _owner.IsDead) yield break;

        ExecuteFrostingLogic();
    }

    [Server]
    private void ExecuteFrostingLogic()
    {
        Collider[] hits = Physics.OverlapSphere(_owner.transform.position, _radius);

        RpcPlayEffect();

        foreach (var col in hits)
        {
            if (!col.TryGetComponent(out Character target)) continue;
            if (target == _owner) continue;

            target.CharacterState.AddState(States.Frosting, 4f, 0, _owner.gameObject, "CircularFrostingShadow");
        }

        NetworkServer.Destroy(gameObject);
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