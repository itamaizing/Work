using System;
using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class CampMinionComponent : Character
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

    protected HeroComponent _myHeroParent;

    [SyncVar] private bool _isIntercepted = false;
    [SyncVar(hook = nameof(OnInvulnerableChanged))] private bool _invulnerable;
    [SyncVar(hook = nameof(OnOwnerTeamChanged))] private int _ownerTeam = CampTeam.None;
    [SyncVar] private int _level = 1;

    private MinionOrder _lastOrder = MinionOrder.None;
    public MinionOrder LastOrder { get => _lastOrder; set => _lastOrder = value; }

    public MinionCamp Camp { get => _camp; set => _camp = value; }
    public int ExpForDieKill => _expForDieKill;
    public bool IsIntercepted => _isIntercepted;
    public float CostCall => _costCall;
    
    public int Level => _level;
    public bool IsInvulnerable => _invulnerable;

    private bool _destroyedNotified;

    public event Action<CampMinionComponent> Destroyed;
    public event Action<CampMinionComponent> Intercepted;

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        ApplyCollider(_invulnerable);
        if (_ownerTeam != CampTeam.None)
            CampTeam.WhenLocalPlayerReady(this, ApplyLocalLayer);
    }

    [Server] public void SetLevel(int level) => _level = level;
    [Server] public void SetExpReward(int xp) => _expForDieKill = xp;

    [Server]
    public void SetInvulnerable(bool value)
    {
        _invulnerable = value;
        ApplyCollider(value);
    }
    
    [Server]
    public void SetOwnerTeam(int teamIndex)
    {
        _ownerTeam = teamIndex;

        if (NetworkSettings.TeamIndex != teamIndex)
            NetworkSettings.TeamIndex = (byte)teamIndex;

        if (isClient) ApplyLocalLayer();
    }
    
    [Server]
    public virtual void ClearNegativeEffectsFrom(int teamIndex)
    {
        var states = new System.Collections.Generic.List<StateBasic>(CharacterState.CurrentStates);
        foreach (var state in states)
        {
            if (state.BaffDebaff != BaffDebaff.Debaff) continue;

            var caster = state.SourceCaster;
            if (caster == null || caster.NetworkSettings.TeamIndex != teamIndex) continue;

            CharacterState.RemoveState(state.State);
        }
    }

    public virtual void SetAuthority(NetworkConnectionToClient con)
    {
        var temp = GetComponent<NetworkIdentity>();
        temp.RemoveClientAuthority();
        temp.AssignClientAuthority(con);

        _isIntercepted = true;
        Intercepted?.Invoke(this);
    }

    private void OnInvulnerableChanged(bool oldValue, bool newValue) => ApplyCollider(newValue);

    private void OnOwnerTeamChanged(int oldValue, int newValue)
    {
        if (newValue != CampTeam.None) ApplyLocalLayer();
    }

    private void ApplyCollider(bool invulnerable)
    {
        if (Collider != null) Collider.enabled = !invulnerable;
    }

    private void ApplyLocalLayer()
    {
        if (_ownerTeam == CampTeam.None) return;
        if (!CampTeam.TryGetLocalTeam(out int localTeam)) return;

        gameObject.layer = CampTeam.LayerFor(_ownerTeam, localTeam);
    }

    private void GiveExpToKiller()
    {
        if (_lastAttacker == null) return;
        if (_lastAttacker.IsDead) return;

        _lastAttacker.LVL.AddEXP(_expForDieKill);
    }

    public override bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        if (_invulnerable) return false;

        Character attacker = skill != null ? skill.Hero : null;
        if (attacker != null) _lastAttacker = attacker;

        bool damaged = base.TryTakeDamage(ref damage, skill);

        if (damaged && attacker != null && _camp != null)
            _camp.NotifyUnitDamaged(this, attacker);

        return damaged;
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
