using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.VFX;

public class StoneFromSky : Skill
{
    [SerializeField] private VisualEffect _stoneVFX;
    [SerializeField] private float _aoeRadius = 2;
    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => Animator.StringToHash("StoneFromSky");

    private Vector3 _clickPoint;
    private static readonly int _onFinishedEventId = Shader.PropertyToID("OnFinished");
    private static readonly int _onDecalsEventId = Shader.PropertyToID("OnDecalsEnded");

    private bool CheckCanCast()
    {
        return Vector3.Distance(_clickPoint, transform.position) <= AreaInfo.Radius;
    }
    
    private bool IsEnemyTarget(Character target) => target.gameObject.layer == LayerMask.NameToLayer("Enemy");
    
    public void AnimCastStone()
    {
        AnimStartCastCoroutine();
    }

    public void AnimStoneEnd()
    {
        AnimCastEnded();
    }   
    

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _clickPoint = (Vector3)targetInfo.Points[0];
    }

    protected override void ClearData()
    {
    }

    private void ClearPoints()
    {
        _clickPoint = Vector3.zero;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        if (_stoneVFX != null)
            _stoneVFX.outputEventReceived -= OnVFXOutputEvent;
        TargetInfo targetInfo = new TargetInfo();

        while (!GetMouseButton)
            yield return null;

        _clickPoint = Targeting.GetMousePoint();
        targetInfo.Points.Add(_clickPoint);
        callbackDataSaved(targetInfo);
    }
    
    protected override IEnumerator CastJob()
    {
        CmdMoveAndPlayVFX(_clickPoint);
        if (_stoneVFX != null)
            _stoneVFX.outputEventReceived += OnVFXOutputEvent;
        yield return null;
    }

    private void OnVFXOutputEvent(VFXOutputEventArgs args)
    {
        if (args.nameId == _onFinishedEventId)
            OnVFXFinished();
        if (args.nameId == _onDecalsEventId)
            OnVFXDecalsEnded();

    }

    private void OnVFXFinished()
    {
        if (!isOwned) return;

        ApplyAreaDamage(_clickPoint);
    }

    private void OnVFXDecalsEnded()
    {
        CmdDisableVFX();
    }

    [Command]
    private void CmdDisableVFX()
    {
        RpcDisableVFX();
    }

    [ClientRpc]
    private void RpcDisableVFX()
    {
        if (_stoneVFX == null) return;

        _stoneVFX.Stop();
        _stoneVFX.gameObject.SetActive(false);
        
        _stoneVFX.transform.SetParent(transform);
        
        ClearPoints();
    }
    
    
    [Command]
    private void CmdMoveAndPlayVFX(Vector3 position)
    {
        RpcMoveAndPlayVFX(position);
    }

    [ClientRpc]
    private void RpcMoveAndPlayVFX(Vector3 position)
    {
        if (_stoneVFX == null) return;
        
        _stoneVFX.transform.SetParent(null);
        _stoneVFX.gameObject.SetActive(true);
        _stoneVFX.transform.position = position;
        _stoneVFX.Stop();
        _stoneVFX.Play();
    }

    private void ApplyAreaDamage(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, _aoeRadius, _targetsLayers);

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<Character>(out var target)) continue;
            if (!IsEnemyTarget(target)) continue;
            if (target.IsDead) continue;

            Damage damage = new Damage
            {
                Value = Buff.Damage.GetBuffedValue(Damage),
                Type = Info.DamageType,
            };

            CmdApplyDamage(damage, target.gameObject);

            CmdAddStunState(target.gameObject);
        }
    }

    [Command]
    private void CmdAddStunState(GameObject target)
    {
        target.GetComponent<Character>().CharacterState.AddState(States.Stun, 2, 0, Hero.gameObject, name);
    }
}
