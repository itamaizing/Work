using Mirror;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AI;

public class MinionComponent : Character
{
    public enum MinionOrder
    {
        None,
        Move,
        AutoAttack
    }
    
    [SerializeField] protected int _expForDieKill = 5;
    [SerializeField] private float _costCall;
    [SerializeField] protected NavMeshAgent _navMeshAgent;

    private Character _lastAttacker;
    private MinionCamp _camp;

    public MinionCamp Camp { get => _camp; set => _camp = value; }

    protected HeroComponent _myHeroParent;
    [SyncVar] private bool _isIntercepted = false;

    private MinionOrder _lastOrder = MinionOrder.None;
    public MinionOrder LastOrder { get => _lastOrder; set => _lastOrder = value; }

    public int ExpForDieKill { get => _expForDieKill; }
    public bool IsIntercepted { get => _isIntercepted; }
    public float CostCall => _costCall;

    private bool _destroyedNotified;

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

    private void GiveExpToKiller()
    {
        if (_lastAttacker == null) return; if (_lastAttacker == null) return;
        if (_lastAttacker.IsDead) return; if (_lastAttacker.IsDead) return;

        _lastAttacker.LVL.AddEXP(_expForDieKill); _lastAttacker.LVL.AddEXP(_expForDieKill);
    }

    public override bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        bool b = base.TryTakeDamage(ref damage, skill);
        if (b && skill != null && skill.Hero != null)
        {
            if (_camp != null)
                _camp.AddAttacker(skill.Hero.gameObject); _camp.AddAttacker(skill.Hero.gameObject);

            _lastAttacker = skill.Hero; _lastAttacker = skill.Hero;
        }
        return b;
    }

    private void OnDestroy()
    {
        RaiseDestroyed();
    }

    protected override void OnDied()
    {
        base.OnDied();
        if (_navMeshAgent != null) _navMeshAgent.enabled = false;

        if (isServer)
        {
            GiveExpToKiller();
            RaiseDestroyed();
            Destroy(gameObject, 3f);

        }
    }

    private void RaiseDestroyed()
    {
        if (_destroyedNotified) return;
        _destroyedNotified = true;
        Destroyed?.Invoke(this);
    }

    protected override void ResetAll()
    {
        base.ResetAll();
        if (_navMeshAgent != null) _navMeshAgent.enabled = true;
    }
}
