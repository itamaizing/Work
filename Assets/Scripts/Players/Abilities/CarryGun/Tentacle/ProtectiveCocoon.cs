using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProtectiveCocoon : NetworkBehaviour
{
    private const float Lifetime = 6f;
    private const float RegenBuffDuration = 20f;
    private const float RegenMultiplier = 2f;

    [SyncVar] private uint _targetNetId;

    private Character _target;
    private List<Skill> _disabledSkills = new();

    private Coroutine _lifeCoroutine;
    private Coroutine _regenBuffCoroutine;

    private float _originalRegenValue;

    public void Init(Character target)
    {
        if (!isServer) return;

        _target = target;
        _targetNetId = target.netId;

        ApplyControl();
        ApplyRegenBuff();

        _lifeCoroutine = StartCoroutine(LifeTimer());
        _regenBuffCoroutine = StartCoroutine(RegenBuffTimer());
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

    private void ApplyRegenBuff()
    {
        if (_target == null) return;

        foreach (var resource in _target.GetComponents<Resource>())
        {
            if (resource.RegenerationValue > 0)
            {
                _originalRegenValue = resource.RegenerationValue;
                resource.RegenerationValue *= RegenMultiplier;
            }
        }
    }

    private IEnumerator RegenBuffTimer()
    {
        yield return new WaitForSeconds(RegenBuffDuration);

        if (_target == null) yield break;

        foreach (var resource in _target.GetComponents<Resource>())
        {
            if (resource.RegenerationValue > 0)
            {
                resource.RegenerationValue = _originalRegenValue;
            }
        }
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
