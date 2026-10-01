using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class BurningMatterTile : AuraStateHandler
{
    [SerializeField] private ParticleSystem _particleSystem;

    private readonly HashSet<Character> _affectedTargets = new();

    public void Init(float duration, float radius, Character owner)
    {
        _owner  = owner;
        _radius = radius;

        Destroy(this, duration);
        RpcInit(duration, radius, owner.gameObject);
    }

    [ClientRpc]
    private void RpcInit(float duration, float radius, GameObject ownerObj)
    {
        if (ownerObj == null) return;
        _owner  = ownerObj.GetComponent<Character>();
        _radius = radius;

        if (_particleSystem != null)
        {
            var shape = _particleSystem.shape;
            shape.radius = radius;
            _particleSystem.Play();
        }

        if (isOwned)
            ActivateAura(true, duration);
    }

    protected override void OnTargetEnter(Character target)
    {
        CmdOnTargetEnter(target.gameObject);
    }
    
    [Command]
    private void CmdOnTargetEnter(GameObject targetObj)
    {
        if (targetObj == null) return;

        var target = targetObj.GetComponent<Character>();
        if (target == null || _affectedTargets.Contains(target)) return;

        target.CharacterState.AddState(
            States.BurningMatter, 6f, 1000f,
            Schools.Air, _owner.gameObject, nameof(BurningMatter));

        _affectedTargets.Add(target);
    }


    protected override void OnTargetExit(Character target)
    {
    }

    protected override void OnAuraDisabled()
    {
        _affectedTargets.Clear();
        _particleSystem?.Stop();
    }

    private void OnDestroy()
    {
        if (isServer)
        {
            NetworkServer.Destroy(this.gameObject);
        }
        
        ActivateAura(false);
    }
}