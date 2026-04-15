using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class SubjugationMind : Skill
{
    [SerializeField] private GameObject _subjugationMindPrefab;
    [SerializeField] private float _breakDistance = 6f;

    private GameObject _activeEffect;
    private bool _isStreaming;
    private bool _streamFinished;
    private Character _cachedTarget;
    private bool _isInterceptApplied;

    protected override bool IsCanCast => !_isStreaming && Targeting.GetTarget() != null && Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.Radius;

    private const string SubjugationMindMidTrigger = "PullingHealthMidTrigger";
    private const string SubjugationMindCastDelayExit = "PullingHealthCastDelayExit";

    private int _midTriggerHash = Animator.StringToHash(SubjugationMindMidTrigger);
    private int _castDelayHash = Animator.StringToHash("PullingHealthCastDelay");

    private void OnEnable()
    {
        OnSkillCanceled += HandleCancel;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleCancel;
    }

    private void HandleCancel() => EndAnim();

    protected override int AnimTriggerCastDelay => _castDelayHash;
    protected override int AnimTriggerCast => 0;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget((ITargetable)(targetInfo.GetTargets()[0] as Character));
    }

    protected override void ClearData()
    {
        _isStreaming = false;
        _cachedTarget = null;
        _isInterceptApplied = false;

        CmdDestroyEffect();
        Targeting.ClearTarget();
    }

    private void StopStream()
    {
        EndAnim();

        _isStreaming = false;
        _streamFinished = true;

        CmdDestroyEffect();
        Targeting.ClearTarget();
    }

    private void EndAnim()
    {
        _hero.Animator.ResetTrigger(_midTriggerHash);
        _hero.NetworkAnimator.ResetTrigger(_midTriggerHash);

        _hero.Animator.CrossFade(SubjugationMindCastDelayExit, 0.1f);

        Hero.Move.IsMoveBlocked = false;
        Hero.Move.StopLookAt();
        Hero.Animator.speed = 1;
    }

    protected override IEnumerator CastJob()
    {
        var target = Targeting.GetTarget()?.Character;
        _streamFinished = false;

        if (target == null) yield break;

        if (target is MinionComponent)
        {
            CmdIntercept(target);
            AfterCastJob();
            yield break;
        }

        _cachedTarget = target;

        _hero.Animator.SetTrigger(_midTriggerHash);
        _hero.NetworkAnimator.SetTrigger(_midTriggerHash);

        Hero.Move.StopMoveAndAnimationMove();
        Hero.Move.IsMoveBlocked = true;

        AfterCastJob();

        StartCoroutine(StreamDuration());

        while (!_streamFinished) yield return null;
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
        _isInterceptApplied = false;

        if (_cachedTarget == null)
        {
            StopStream();
            yield break;
        }

        CmdSpawnEffect(gameObject, _cachedTarget.gameObject);

        float elapsed = 0f;
        Vector3 dir = (_cachedTarget.transform.position - transform.position).normalized;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        while (elapsed < CastStreamDuration)
        {
            if (_cachedTarget == null || _cachedTarget.IsDead)
            {
                StopStream();
                yield break;
            }

            float distance = Vector3.Distance(transform.position, _cachedTarget.transform.position);

            if (distance > _breakDistance)
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

    [Command]
    private void CmdSpawnEffect(GameObject start, GameObject target)
    {
        if (_subjugationMindPrefab == null) return;

        GameObject effect = Instantiate(_subjugationMindPrefab, start.transform.position, Quaternion.identity);
        NetworkServer.Spawn(effect);

        RpcInitEffect(effect, start, target);

        _activeEffect = effect;
    }

    [Command]
    private void CmdIntercept(Character character)
    {
        if (character == null) return;

        if (character is MinionComponent minion)
        {
            minion.SetAuthority(connectionToClient);

            if (Hero is HeroComponent hero) hero.SpawnComponent.AddUnit(minion);
            return;
        }

        else if (character is HeroComponent heroTarget)
        {
            var networkIdentity = heroTarget.GetComponent<NetworkIdentity>();
            if (networkIdentity == null) return;

            networkIdentity.RemoveClientAuthority();
            networkIdentity.AssignClientAuthority(connectionToClient);

            if (Hero is HeroComponent currentHero) currentHero.SpawnComponent.AddUnit(heroTarget);

            StartCoroutine(ReturnHeroControlAfterDelay(heroTarget, networkIdentity));
        }
    }

    [Command]
    private void CmdDestroyEffect()
    {
        if (_activeEffect != null)
        {
            NetworkServer.Destroy(_activeEffect);
            RpcDestroyEffect(_activeEffect);
            _activeEffect = null;
        }
    }

    [ClientRpc]
    private void RpcDestroyEffect(GameObject effect)
    {
        if (effect != null) Destroy(effect);
    }

    [ClientRpc]
    private void RpcInitEffect(GameObject effect, GameObject start, GameObject target)
    {
        if (effect == null) return;

        var effects = effect.GetComponentsInChildren<PullingHealthEffect>();

        foreach (var e in effects)
        {
            e.Initialize(start, target);
            e.Activate();
        }
    }
}
