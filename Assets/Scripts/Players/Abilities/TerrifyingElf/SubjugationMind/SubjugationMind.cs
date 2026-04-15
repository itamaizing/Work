using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SubjugationMind : Skill
{
    [SerializeField] private GameObject _subjugationMindPrefab;

    private bool _isStreaming;
    private bool _streamFinished;
    private Character _cachedTarget;
    private bool _isInterceptApplied;

    protected override bool IsCanCast => !_isStreaming && Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("PullingHealthCastDelay");
    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;

        if (target == null)
            yield break;

        _cachedTarget = target;

        AfterCastJob();

        StartCoroutine(StreamDuration());

        while (!_streamFinished)
            yield return null;
    }

    protected override void ClearData()
    {
        _isStreaming = false;
        _cachedTarget = null;
        Targeting.ClearTarget();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        var multiMagic = Hero.CharacterState.GetState(States.MultiMagic) as MultiMagic;

        TargetInfo targetInfo = new TargetInfo();

        while (Targeting.GetTempTarget()?.Targetable == null && !_disactive)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), 0.5f);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null)
                {
                    Targeting.SetTarget(temp);

                    if (multiMagic != null)
                        multiMagic.LastTarget = temp;

                    break;
                }
            }

            yield return null;
        }

        var target = Targeting.GetTarget()?.Character;

        if (target != null)
        {
            targetInfo.AddTarget(target);
            callbackDataSaved(targetInfo);
        }
    }

    private IEnumerator StreamDuration()
    {
        _isStreaming = true;
        _streamFinished = false;

        float elapsed = 0f;

        if (!_isInterceptApplied)
        {
            CmdIntercept(_cachedTarget);
            _isInterceptApplied = true;
        }

        while (elapsed < CastStreamDuration)
        {
            if (_cachedTarget == null || _cachedTarget.IsDead)
            {
                StopStream();
                yield break;
            }

            if (Vector3.Distance(transform.position, _cachedTarget.transform.position) > AreaInfo.Radius)
            {
                StopStream();
                yield break;
            }

            if (!_isInterceptApplied)
            {
                CmdIntercept(_cachedTarget);
                _isInterceptApplied = true;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        StopStream();
    }

    private void StopStream()
    {
        _isStreaming = false;
        _streamFinished = true;

        Targeting.ClearTarget();
    }

    [Command]
    private void CmdIntercept(Character character)
    {
        if (character == null) return;

        if (character is MinionComponent minion)
        {
            minion.SetAuthority(connectionToClient);

            if (Hero is HeroComponent hero)
            {
                hero.SpawnComponent.AddUnit(minion);
            }
        }
        else if (character is HeroComponent heroTarget)
        {
            var networkIdentity = heroTarget.GetComponent<NetworkIdentity>();
            if (networkIdentity == null) return;

            networkIdentity.RemoveClientAuthority();
            networkIdentity.AssignClientAuthority(connectionToClient);

            if (Hero is HeroComponent currentHero)
            {
                currentHero.SpawnComponent.AddUnit(heroTarget);
            }

            StartCoroutine(ReturnHeroControlAfterDelay(heroTarget, networkIdentity));
        }
    }

    private IEnumerator ReturnHeroControlAfterDelay(HeroComponent heroTarget, NetworkIdentity networkIdentity)
    {
        yield return new WaitForSeconds(4);

        if (networkIdentity != null)
        {
            networkIdentity.RemoveClientAuthority();
            networkIdentity.AssignClientAuthority(heroTarget.connectionToClient);

            if (Hero is HeroComponent currentHero)
            {
                currentHero.SpawnComponent.CmdRemoveUnit(heroTarget);
            }
        }
    }
}
