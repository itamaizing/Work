using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProtectiveCocoon : NetworkBehaviour
{
    [SerializeField] private float _lifetime = 20f;
    [SerializeField] private float _regenBuffDuration = 20f;
    private const float RegenMultiplier = 2f;

    private Character _hero;
    private List<Skill> _disabledSkills = new();

    private Coroutine _lifeCoroutine;
    private Coroutine _regenBuffCoroutine;

    private float _originalRegenValue;

    public void Init(Character target)
    {
        _hero = target;

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
        if (_hero == null) return;

        _hero.Move.IsMoveBlocked = true;
        _hero.IsDisappeared = true;
        _hero.Collider.enabled = false;
        _hero.Rigidbody.isKinematic = true;

        foreach (var skill in _hero.Abilities.Skills)
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
        if (_hero == null) return;

        _hero.Move.IsMoveBlocked = false;
        _hero.IsDisappeared = false;
        _hero.Collider.enabled = true;
        _hero.Rigidbody.isKinematic = false;

        foreach (var skill in _disabledSkills)
        {
            if (skill != null)
                skill.Disactive = false;
        }

        _disabledSkills.Clear();
    }

    private void ApplyRegenBuff()
    {
        if (_hero == null) return;

        foreach (var resource in _hero.GetComponents<Resource>())
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
        if (_hero == null) return;

        foreach (var resource in _hero.GetComponents<Resource>())
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
