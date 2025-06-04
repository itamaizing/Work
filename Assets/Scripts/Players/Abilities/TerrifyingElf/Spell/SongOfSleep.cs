using Mirror;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SongOfSleep : Skill
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private DrawCircleAlternative drawCircle;
    [SerializeField] private float duration;

    private Coroutine _radiusJob;
    private Character _target;

    protected override bool IsCanCast => IsHaveCharge && _target != null && IsTargetInRadius(Radius, _target.transform);

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo) => _target = (Character)targetInfo.Targets[0];

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        StartRadiusRender();

        while (!GetMouseButton && !_disactive) yield return null;

        _target = Physics.OverlapSphere(transform.position, Radius, TargetsLayers).Select(character => character.GetComponent<Character>())
                  .Where(ch => ch != null && ch != _playerLinks).OrderBy(ch => Vector3.Distance(transform.position, ch.transform.position)).FirstOrDefault();

        StopRadiusRender();

        if (_target == null)
        {
            TryCancel(true);
            yield break;
        }

        TargetInfo targetInfo = new();
        targetInfo.Targets.Add(_target);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            _target.CharacterState.CmdAddState(States.Sleep, duration, 0, _playerLinks.gameObject, name);
            TryUseCharge();
        }

        ClearData();
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
        StopRadiusRender();
    }

    private void StartRadiusRender()
    {
        if (drawCircle == null) return;

        drawCircle.Draw(Radius);
        _radiusJob = StartCoroutine(RadiusColorJob());
    }

    private void StopRadiusRender()
    {
        if (_radiusJob != null)
        {
            StopCoroutine(_radiusJob);
            _radiusJob = null;
        }
        drawCircle?.Clear();
    }

    private IEnumerator RadiusColorJob()
    {
        var wait = new WaitForSeconds(0.1f);
        while (true)
        {
            bool enemyInside = Physics.OverlapSphere(transform.position, Radius, TargetsLayers).Any(col => col.TryGetComponent<Character>(out var ch) && ch != _playerLinks);

            drawCircle.SetColor(enemyInside ? Color.green : Color.red);
            yield return wait;
        }
    }
}
