using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoSkillCast
{
    private Skill _currentSkill;
    private TargetInfo _targetInfo;
    private Coroutine _tryCastCoroutine;
    private MonoBehaviour _parentForCoroutine;

    public bool IsBusy { get { return _currentSkill != null; } }

    public AutoSkillCast(MonoBehaviour parentForCoroutine)
    {
        _parentForCoroutine = parentForCoroutine;
    }

    public void SetSkill(Skill skill, TargetInfo targetInfo)
    {
        _currentSkill = skill;
        _targetInfo = new();
        _targetInfo.Targets = new(targetInfo.Targets);
        _targetInfo.Points = new(targetInfo.Points);
        _tryCastCoroutine = _parentForCoroutine.StartCoroutine(TryCastJob());

        _currentSkill.SkillRender.StartDrawAutoAttackRadius(_currentSkill.Radius);
    }

    public void DeleteSkill()
    {
        if (_currentSkill == null) return;

        _currentSkill.TryCancel(true);

        _currentSkill.SkillRender.StopDrawAutoAttackRadius();

        StopTryCastCoroutine();

        _currentSkill = null;
    }

    public void Pause()
    {
        _currentSkill.TryCancel(true);

        StopTryCastCoroutine();
    }

    public void Continue()
    {
        if (_tryCastCoroutine == null && _currentSkill != null)
        {
            _tryCastCoroutine = _parentForCoroutine.StartCoroutine(TryCastJob());

            _currentSkill.SkillRender.StartDrawAutoAttackRadius(_currentSkill.Radius);
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

        if (_targetInfo?.Targets != null)
        {
            foreach (var item in _targetInfo.Targets)
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
        foreach (var item in _targetInfo.Targets)
        {
            if (item is Character character)
            {
                character.SelectedCircle.SwitchSelectCircle(true);
            }
        }
        while (true)
        {
            _currentSkill.TryCast(_targetInfo);
            yield return null;
        }
    }
}
