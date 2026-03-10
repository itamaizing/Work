using System.Collections;
using UnityEngine;

public class ParalyzingTentacles : Skill
{
    [SerializeField] private LineRenderer _line;
    [SerializeField] private Transform _startPoint;

    private Character _target;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(System.Action<TargetInfo> callback)
    {
        TargetInfo info = new TargetInfo();
        Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

        if (Targeting.GetTempTarget()?.Targetable != null)
        {
            info.AddTarget(Targeting.GetTempTarget()?.Targetable);
        }

        callback?.Invoke(info);
        yield return null;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0)
            _target = targetInfo.GetTargets()[0] as Character;
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null)
            yield break;

        if (_line != null)
        {
            _line.positionCount = 2;
            _line.enabled = true;
        }

        float timer = 0f;

        while (timer < CastStreamDuration && _target != null)
        {
            UpdateLine();

            timer += Time.deltaTime;
            yield return null;
        }

        StopLine();
    }

    protected override void ClearData()
    {
        StopLine();
        _target = null;
    }

    private void UpdateLine()
    {
        if (_line == null || _target == null)
            return;

        Vector3 start = _startPoint != null ? _startPoint.position : Hero.transform.position;
        Vector3 end = _target.transform.position + Vector3.up * 0.5f;

        _line.SetPosition(0, start);
        _line.SetPosition(1, end);
    }

    private void StopLine()
    {
        if (_line != null)
            _line.enabled = false;
    }
}