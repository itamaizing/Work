using Mirror;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

public class MinionComponent : Character
{
    [SerializeField] protected int _expForDieKill = 5;
    [SerializeField] protected NavMeshAgent _navMeshAgent;

    protected HeroComponent _myHeroParent;

    public int ExpForDieKill { get => _expForDieKill; }

    public event Action<MinionComponent> Destroyed;
    public event Action<MinionComponent> Intercepted;

    public virtual void SetAuthority(NetworkConnectionToClient con)
    {
        var temp = GetComponent<NetworkIdentity>();
        temp.RemoveClientAuthority();
        temp.AssignClientAuthority(con);

        Intercepted?.Invoke(this);
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
    }

    protected override void OnDied()
    {
        base.OnDied();
        _navMeshAgent.enabled = false;
    }

    protected override void ResetAll()
    {
        base.ResetAll();
        _navMeshAgent.enabled = true;
    }
}
