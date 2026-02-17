using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProtectiveCocoon : NetworkBehaviour
{
    private const float Lifetime = 6f;

    [SyncVar] private uint _targetNetId;

    private Character _target;
    private List<Skill> _disabledSkills = new();

    private Coroutine _lifeCoroutine;

    public void Init(Character target)
    {
        if (!isServer) return;

        _target = target;
        _targetNetId = target.netId;

        ApplyControl();

        _lifeCoroutine = StartCoroutine(LifeTimer());
    }

    private void ApplyControl()
    {
        if (_target == null) return;

        _target.Move.SetCanMove(false);

        foreach (var skill in _target.Abilities.Skills)
        {
            if (!skill.Disactive)
            {
                skill.Disactive = true;
                _disabledSkills.Add(skill);
            }
        }
    }

    private void RemoveControl()
    {
        if (_target == null) return;

        _target.Move.SetCanMove(true);

        foreach (var skill in _disabledSkills)
        {
            if (skill != null)
                skill.Disactive = false;
        }

        _disabledSkills.Clear();
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(Lifetime);

        RemoveControl();

        if (isServer)
            NetworkServer.Destroy(gameObject);
    }


    public override void OnStopServer()
    {
        RemoveControl();
    }
}
