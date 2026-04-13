using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class FearSkill : Skill
{
    [SerializeField] private ParticleSystem _fearEffect;
    [SerializeField] private float _aoeRadius = 1.5f;
    [SerializeField] private float _fearDuration = 6f;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("Fear");
    protected override bool IsCanCast => true;

    private bool IsEnemyTarget(Character target) =>
        target.gameObject.layer == LayerMask.NameToLayer("Enemy");

    public void AnimCastFear() => AnimStartCastCoroutine();
    public void AnimFearEnd()  => AnimCastEnded();

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override void ClearData()
    {
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        TargetInfo targetInfo = new TargetInfo();
        while (!GetMouseButton)
            yield return null;

        targetInfo.AddTarget(_hero);
        targetDataSavedCallback(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        CmdPlayEffect();

        Collider[] hits = Physics.OverlapSphere(transform.position, _aoeRadius, Targeting.Layer);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;

            CmdApplyFear(target.gameObject);
        }

        if (_fearEffect != null)
            yield return new WaitUntil(() => !_fearEffect.isPlaying);
        else
            yield return null;

        CmdStopEffect();

    }
    
    [Command]
    private void CmdPlayEffect() => RpcPlayEffect();

    [ClientRpc]
    private void RpcPlayEffect()
    {
        if (_fearEffect == null) return;
        _fearEffect.gameObject.SetActive(true);
        _fearEffect.Stop();
        _fearEffect.Play();
    }

    [Command]
    private void CmdStopEffect() => RpcStopEffect();

    [ClientRpc]
    private void RpcStopEffect()
    {
        if (_fearEffect == null) return;
        _fearEffect.Stop();
        _fearEffect.gameObject.SetActive(false);
    }

    [Command]
    private void CmdApplyFear(GameObject targetGO)
    {
        if (targetGO == null) return;
        if (!targetGO.TryGetComponent<Character>(out var target)) return;
        if (target.IsDead) return;

        target.CharacterState.AddState(
            States.Fear,
            _fearDuration,
            0,
            Hero.gameObject,
            name
        );
    }
}
