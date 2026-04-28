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

    protected virtual float GetCurrentRadius() => _radius;
    protected Character _owner;
    protected readonly HashSet<Character> _currentTargets = new();
    protected Skill _fromSkill;

    private Coroutine _checkCoroutine;
    private Coroutine _durationCoroutine;
    private bool _isActive = false;

    public bool IsActive => _isActive;

    public void ActivateAura(bool active, float duration = -1f, bool isAffectOnOwner = false, Skill fromSkill = null)
    {
        if (_isActive == active) return;

        _isActive = active;

        if (fromSkill != null) _fromSkill = fromSkill;

        if (_checkCoroutine != null)
        {
            StopCoroutine(_checkCoroutine);
            _checkCoroutine = null;
        }
        if (_durationCoroutine != null)
        {
            StopCoroutine(_durationCoroutine);
            _durationCoroutine = null;
        }

        RemoveEffectsFromAllTargets();

        if (active)
        {
            if (_owner == null)
                _owner = GetComponent<Character>();

            _checkCoroutine = StartCoroutine(CheckTargetsRoutine(isAffectOnOwner));
            OnAuraEnabled();

            if (duration > 0f)
            {
                _durationCoroutine = StartCoroutine(DurationRoutine(duration));
            }
        }
        else
        {
            OnAuraDisabled();
        }
    }
    
    private IEnumerator DurationRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (_isActive)
        {
            ActivateAura(false);
        }
    }

    private IEnumerator CheckTargetsRoutine(bool isAffectOnOwner)
    {
        yield return null;
        
        while (_isActive && _owner != null && !_owner.IsDead)
        {
            var colliders = Physics.OverlapSphere(transform.position, GetCurrentRadius(), _targetLayer);
            var newTargets = new HashSet<Character>();

            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (col.TryGetComponent<Character>(out var character) && !character.IsDead)
                {
                    if (character == _owner && isAffectOnOwner)
                        newTargets.Add(character);
                    else if(character != _owner)
                        newTargets.Add(character);
                }
            }
            foreach (var old in _currentTargets.ToArray())
            {
                if (old == null)
                {
                    continue;
                }
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
        ActivateAura(false);
    }

    protected abstract void OnTargetEnter(Character target);
    protected abstract void OnTargetExit(Character target);

    protected virtual void OnAuraEnabled() { }
    protected virtual void OnAuraDisabled() { }

    protected virtual void OnTargetStay(Character target) { }
    
    [Command]
    protected void CmdApplyStateToTarget(GameObject target, States state, float duration, Schools school, GameObject source, string skillName)
    {
        target.GetComponent<CharacterState>().AddState(state, duration, 0, school, source, skillName);
    }

    [Command]
    protected void CmdRemoveStateFromTarget(GameObject target, States state)
    {
        target.GetComponent<CharacterState>().RemoveState(state);
    }
    
    
}
