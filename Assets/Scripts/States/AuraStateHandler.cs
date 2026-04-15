using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public abstract class AuraStateHandler : NetworkBehaviour
{
    [Header("Aura Settings")]
    [SerializeField] protected float _radius = 8f;
    [SerializeField] protected float _checkInterval = 0.5f;
    [SerializeField] LayerMask _targetLayer;

    protected Character _owner;
    protected readonly HashSet<Character> _currentTargets = new();

    private Coroutine _checkCoroutine;
    private bool _isActive = false;

    public bool IsActive => _isActive;

    public void SetActive(bool active)
    {
        if (_isActive == active) return;

        _isActive = active;
        
        if (_checkCoroutine != null)
        {
            StopCoroutine(_checkCoroutine);
            _checkCoroutine = null;
        }

        RemoveEffectsFromAllTargets();

        if (active)
        {
            if (_owner == null)
                _owner = GetComponent<Character>();

            _checkCoroutine = StartCoroutine(CheckTargetsRoutine());
            OnAuraEnabled();
        }
        else
        {
            OnAuraDisabled();
        }
    }

    private IEnumerator CheckTargetsRoutine()
    {
        yield return null;
        
        while (_isActive && _owner != null && !_owner.IsDead)
        {
            var colliders = Physics.OverlapSphere(transform.position, _radius, _targetLayer);
            var newTargets = new HashSet<Character>();

            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (col.TryGetComponent<Character>(out var character) &&
                    character != _owner &&
                    !character.IsDead)
                {
                    newTargets.Add(character);
                }
            }

            foreach (var old in _currentTargets.ToArray())
            {
                if (old == null) continue;
                if (!newTargets.Contains(old))
                    OnTargetExit(old);
            }

            foreach (var newTarget in newTargets)
            {
                if (!_currentTargets.Contains(newTarget))
                    OnTargetEnter(newTarget);
            }

            _currentTargets.Clear();
            _currentTargets.UnionWith(newTargets);

            yield return new WaitForSeconds(_checkInterval);
        }
    }

    protected void RemoveEffectsFromAllTargets()
    {
        foreach (var target in _currentTargets.ToArray())
        {
            if (target == null) continue;
            OnTargetExit(target);
        }

        _currentTargets.Clear();
    }

    private void OnDestroy()
    {
        SetActive(false);
    }

    protected abstract void OnTargetEnter(Character target);
    protected abstract void OnTargetExit(Character target);

    protected virtual void OnAuraEnabled() { }
    protected virtual void OnAuraDisabled() { }

    protected virtual void OnTargetStay(Character target) { }
}
