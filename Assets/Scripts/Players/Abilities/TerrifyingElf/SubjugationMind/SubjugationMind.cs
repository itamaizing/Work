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

    private byte _originalTeamIndex;
    private bool _teamWasChanged;

    private NetworkConnectionToClient _originalOwner;

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

    [Server]
    private IEnumerator ReturnHeroControlAfterDelay(HeroComponent heroTarget, NetworkIdentity netId)
    {
        yield return new WaitForSeconds(4);

        if (netId == null) yield break;

        Vector3 finalPosition = heroTarget.transform.position;
        Quaternion finalRotation = heroTarget.transform.rotation;

        netId.RemoveClientAuthority();

        if (_originalOwner != null)
        {
            TargetRpcSetKinematicTrue(connectionToClient, heroTarget.netId);
            TargetRpcSetKinematicFalse(_originalOwner, heroTarget.netId);

            TargetRpcResetControlState(connectionToClient, heroTarget.netId);

            TargetRpcSetTransform(_originalOwner, heroTarget.netId, finalPosition, finalRotation);

            netId.AssignClientAuthority(_originalOwner);

            RestoreControlledTeam(heroTarget);
        }

        _originalOwner = null;
    }

    [Server]
    private void ApplyControlledTeam(HeroComponent heroTarget)
    {
        if (heroTarget == null) return;

        var targetSettings = heroTarget.GetComponent<UserNetworkSettings>();
        var casterSettings = Hero.GetComponent<UserNetworkSettings>();

        if (targetSettings == null || casterSettings == null) return;

        _originalTeamIndex = targetSettings.TeamIndex;
        _teamWasChanged = true;

        targetSettings.TeamIndex = casterSettings.TeamIndex;

        RefreshAllUsersLayers();
    }

    [Server]
    private void RestoreControlledTeam(HeroComponent heroTarget)
    {
        if (!_teamWasChanged || heroTarget == null) return;

        var targetSettings = heroTarget.GetComponent<UserNetworkSettings>();
        if (targetSettings == null) return;

        targetSettings.TeamIndex = _originalTeamIndex;
        _teamWasChanged = false;

        RefreshAllUsersLayers();
    }

    [Server]
    private void RefreshAllUsersLayers()
    {
        var allUsers = FindObjectsOfType<UserNetworkSettings>();
        foreach (var user in allUsers) if (user != null) user.RpcUpdateLayers();
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

        if (character is HeroComponent heroTarget)
        {
            var netId = heroTarget.GetComponent<NetworkIdentity>();
            if (netId == null) return;

            _originalOwner = netId.connectionToClient != null ? netId.connectionToClient : null;

            netId.RemoveClientAuthority();
            netId.AssignClientAuthority(connectionToClient);

            ApplyControlledTeam(heroTarget);

            TargetRpcSetKinematicFalse(connectionToClient, heroTarget.netId);
            if (_originalOwner != null) TargetRpcSetKinematicTrue(_originalOwner, heroTarget.netId);

            StartCoroutine(ReturnHeroControlAfterDelay(heroTarget, netId));
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

    [TargetRpc]
    private void TargetRpcSetKinematicFalse(NetworkConnection target, uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var identity)) return;

        var rb = identity.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
    }

    [TargetRpc]
    private void TargetRpcSetKinematicTrue(NetworkConnection target, uint netId)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var identity)) return;

        var rb = identity.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    [TargetRpc]
    private void TargetRpcSetTransform(NetworkConnection target, uint netId, Vector3 pos, Quaternion rot)
    {
        if (!NetworkClient.spawned.TryGetValue(netId, out var identity)) return;

        identity.transform.position = pos;
        identity.transform.rotation = rot;
    }

    [TargetRpc]
    private void TargetRpcResetControlState(NetworkConnection target, uint heroNetId)
    {
        if (!NetworkClient.spawned.TryGetValue(heroNetId, out var identity)) return;

        var hero = identity.GetComponent<HeroComponent>();
        if (hero == null) return;

        var skillManager = hero.Abilities;

        skillManager.CancleAllSkills();
        skillManager.OnSelect(false);

        var input = hero.GetComponent<InputHandler>();
        if (input != null) input.enabled = false;
    }
}
