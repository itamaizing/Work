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
        _currentSkill.TryCancel(true);

        _currentSkill = null;

        if (_tryCastCoroutine != null)
        {
            _parentForCoroutine.StopCoroutine(_tryCastCoroutine);
            _tryCastCoroutine = null;

            _currentSkill.SkillRender.StopDrawAutoAttackRadius();
        }
    }

    public void Pause()
    {
        _currentSkill.TryCancel(true);

        if (_tryCastCoroutine != null)
        {
            _parentForCoroutine.StopCoroutine(_tryCastCoroutine);
            _tryCastCoroutine = null;

            _currentSkill.SkillRender.StopDrawAutoAttackRadius();
        }
    }

    public void Continue()
    {
        if (_tryCastCoroutine == null && _currentSkill != null)
        {
            _tryCastCoroutine = _parentForCoroutine.StartCoroutine(TryCastJob());

            _currentSkill.SkillRender.StartDrawAutoAttackRadius(_currentSkill.Radius);
        }
    }

    private IEnumerator TryCastJob()
    {
        while (true)
        {
            _currentSkill.TryCast(_targetInfo);
            yield return null;
        }
    }
}
