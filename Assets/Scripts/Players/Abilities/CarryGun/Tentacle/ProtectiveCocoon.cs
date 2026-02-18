using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProtectiveCocoon : NetworkBehaviour
{
    [SerializeField] private const float _lifetime = 20f;
    [SerializeField] private const float _regenBuffDuration = 20f;
    private const float RegenMultiplier = 2f;

    private Character _target;
    private List<Skill> _disabledSkills = new();

    private Coroutine _lifeCoroutine;
    private Coroutine _regenBuffCoroutine;

    private float _originalRegenValue;

    public void Init(Character target)
    {
        _target = target;

        ApplyControl();
        ApplyRegenBuff();

        _lifeCoroutine = StartCoroutine(LifeTimer());
        _regenBuffCoroutine = StartCoroutine(RegenBuffTimer());
    }

    private void OnDisable()
    {
        if (_lifeCoroutine != null)
        {
            StopCoroutine(_lifeCoroutine);
            _lifeCoroutine = null;
        }

        if (_regenBuffCoroutine != null)
        {
            StopCoroutine(_regenBuffCoroutine);
            _regenBuffCoroutine = null;
        }

        RemoveControl();
        RemoveRegenBuff();
    }

    private void ApplyControl()
    {
        if (_target == null) return;

        _target.Move.IsMoveBlocked = true;

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

        _target.Move.IsMoveBlocked = false;

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

    private void RemoveRegenBuff()
    {
        if (_target == null) return;

        foreach (var resource in _target.GetComponents<Resource>())
        {
            if (resource.RegenerationValue > 0)
            {
                resource.RegenerationValue = _originalRegenValue;
            }
        }
    }

    private IEnumerator RegenBuffTimer()
    {
        yield return new WaitForSeconds(_regenBuffDuration);
        RemoveRegenBuff();
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(_lifetime);

        RemoveControl();

        if (isServer)
            NetworkServer.Destroy(gameObject);
    }

    public override void OnStopServer()
    {
        if (_lifeCoroutine != null)
        {
            StopCoroutine(_lifeCoroutine);
            _lifeCoroutine = null;
        }

        if (_regenBuffCoroutine != null)
        {
            StopCoroutine(_regenBuffCoroutine);
            _regenBuffCoroutine = null;
        }

        RemoveControl();
        RemoveRegenBuff();
    }
}
