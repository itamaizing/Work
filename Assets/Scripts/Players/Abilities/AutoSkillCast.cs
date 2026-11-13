using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSkillCast
{
    private Skill _currentSkill;
    private TargetInfo _targetInfo;
    private bool _followSelection;
    private Coroutine _tryCastCoroutine;
    private MonoBehaviour _parentForCoroutine;
    
    public TargetInfo CurrentTargetInfo => _targetInfo;
    public event Action<Skill, TargetInfo> SkillSet;
    public event Action SkillCleared;

    public bool IsBusy { get { return _currentSkill != null; } }
    public Skill CurrentSkill => _currentSkill;

    public AutoSkillCast(MonoBehaviour parentForCoroutine)
    {
        _parentForCoroutine = parentForCoroutine;
    }

    public void SetSkill(Skill skill, TargetInfo targetInfo)
    {
        if (_currentSkill != null)
            DeleteSkill();

        _currentSkill = skill;
        _followSelection = targetInfo.UsesSelectedTarget;
        _targetInfo = _followSelection ? targetInfo : new TargetInfo();
        if (!_followSelection)
        {
            _targetInfo.AddTargets(targetInfo.GetTargets());
            _targetInfo.Points = new(targetInfo.Points);
        }
        _tryCastCoroutine = _parentForCoroutine.StartCoroutine(TryCastJob());

        _currentSkill.SkillRender.StartDrawAutoAttackRadius(_currentSkill.AreaInfo.Radius);
        
        SkillSet?.Invoke(skill, _targetInfo);
    }

    public void DeleteSkill()
    {
        if (_currentSkill == null) return;

        var skill = _currentSkill;

        StopTryCastCoroutine();
        _currentSkill = null;
        _targetInfo = null;

        skill.TryCancel(true);

        if (skill.Hero != null && skill.Hero.Move != null)
            skill.Hero.Move.StopLookAt();
        SkillCleared?.Invoke();
    }


    public void Pause()
    {
        if (_currentSkill == null) return;

        if (_tryCastCoroutine != null)
        {
            _parentForCoroutine.StopCoroutine(_tryCastCoroutine);
            _tryCastCoroutine = null;
        }

        _currentSkill.TryCancel(true);

        if (_currentSkill.Hero != null && _currentSkill.Hero.Move != null)
            _currentSkill.Hero.Move.StopLookAt();
    }

    public void Continue()
    {
        if (_tryCastCoroutine == null && _currentSkill != null)
        {
            _tryCastCoroutine = _parentForCoroutine.StartCoroutine(TryCastJob());

            _currentSkill.SkillRender.StartDrawAutoAttackRadius(_currentSkill.AreaInfo.Radius);
        }
    }

    private void StopTryCastCoroutine()
    {
        if (_tryCastCoroutine != null)
        {
            _parentForCoroutine.StopCoroutine(_tryCastCoroutine);
            _tryCastCoroutine = null;
        }

        if (_currentSkill != null && _currentSkill.SkillRender != null)
        {
            _currentSkill.SkillRender.StopDrawAutoAttackRadius();
        }

        if (_followSelection)
        {
            SetSelectionCircle(_targetInfo?.SelectedTarget, false);
        }
        else if (_targetInfo != null)
        {
            foreach (var item in _targetInfo.GetTargets())
            {
                if (item is Character character && character?.SelectedCircle != null)
                {
                    character.SelectedCircle.SwitchSelectCircle(false);
                }
            }
        }
    }


    private IEnumerator TryCastJob()
    {
        if (_followSelection)
        {
            SetSelectionCircle(_targetInfo.SelectedTarget, true);
        }
        else
        {
            foreach (var item in _targetInfo.GetTargets())
            {
                if (item is Character character)
                    character.SelectedCircle.SwitchSelectCircle(true);
            }
        }

        while (true)
        {
            if (_followSelection)
            {
                if (!_currentSkill.IsExecuting)
                    TryCastAtSelection();

                yield return new WaitForSeconds(_currentSkill.AutoAttackDelay);
                continue;
            }

            if (_targetInfo.GetTargets().Count > 0 )
            {
                if (_targetInfo.GetTargets()[0] is Character character && _targetInfo.GetTargets()[0] != null)
                {
                    _currentSkill.Hero.Move.LookAtTransform(character.transform);
                    _currentSkill.Hero.Move.IsLookAtCursor = false;
                }
            }
            else if (_targetInfo.Points.Count > 0)
            {
                _currentSkill.Hero.Move.LookAtPosition(_targetInfo.Points[0]);
                _currentSkill.Hero.Move.IsLookAtCursor = false;
            }

            _currentSkill.TryCast(_targetInfo);

            yield return new WaitForSeconds(_currentSkill.AutoAttackDelay);
        }
    }

    private void TryCastAtSelection()
    {
        TargetInfo next = null;
        _currentSkill.TryBuildSelectedTargetInfo(info => next = info);
        if (!ReferenceEquals(_targetInfo.SelectedTarget, next?.SelectedTarget))
        {
            SetSelectionCircle(_targetInfo.SelectedTarget, false);
            _targetInfo = next ?? new TargetInfo();
            SetSelectionCircle(_targetInfo.SelectedTarget, true);
            SkillSet?.Invoke(_currentSkill, _targetInfo);
        }

        if (next != null)
            _currentSkill.TryCast(next);
    }

    private static void SetSelectionCircle(Character target, bool visible)
    {
        if (target != null && target.SelectedCircle != null)
            target.SelectedCircle.SwitchSelectCircle(visible);
    }

}
