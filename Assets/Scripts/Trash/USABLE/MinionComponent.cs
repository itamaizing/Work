using Mirror;
using System;
using UnityEngine;
using UnityEngine.AI;

public class MinionComponent : Character
{
    [SerializeField] protected int _expForDieKill = 5;
    [SerializeField] private float _costCall;
    [SerializeField] protected NavMeshAgent _navMeshAgent;

    protected HeroComponent _myHeroParent;
    [SyncVar] private bool _isIntercepted = false;

    public MinionCamp MyCamp;
    public int ExpForDieKill { get => _expForDieKill; }
    public bool IsIntercepted { get => _isIntercepted; }
    public float CostCall => _costCall;

    public event Action<MinionComponent> Destroyed;
    public event Action<MinionComponent> Intercepted;

    public virtual void SetAuthority(NetworkConnectionToClient con)
    {
        var temp = GetComponent<NetworkIdentity>();
        temp.RemoveClientAuthority();
        temp.AssignClientAuthority(con);

        _isIntercepted = true;
        Intercepted?.Invoke(this);
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);
        if (MyCamp != null)
        {
            MyCamp.OnMinionDied(gameObject);
            MyCamp.RemoveDeadMinion(this);
        }
    }

    protected override void OnDied()
    {
        base.OnDied();
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;

        if (isServer)
        {
            Destroyed?.Invoke(this);
            Destroy(gameObject,3f);
        }
    }
    
    public override bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        bool b = base.TryTakeDamage(ref damage, skill);
        if (b && skill != null && skill.Hero != null && MyCamp != null)
        {
            MyCamp.AddAttacker(skill.Hero.gameObject);
        }
        return b;
    }

    protected override void ResetAll()
    {
        base.ResetAll();
        if (_navMeshAgent != null) _navMeshAgent.enabled = true;
    }
}
