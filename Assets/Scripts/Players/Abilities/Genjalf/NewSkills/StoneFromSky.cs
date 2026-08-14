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
            _clickPoint = targetInfo.Points[0];
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
        TargetInfo targetInfo = new();

        while (!GetMouseButton)
            yield return null;

        Vector3 clickPoint = Targeting.GetMousePoint();

        targetInfo.Points.Add(clickPoint);

        callbackDataSaved(targetInfo);
    }
    
    protected override IEnumerator CastJob()
    {
        Vector3 castPoint = _clickPoint;

        CmdSpawnTemporaryStone(castPoint);

        yield return null;
    }
    
    [Command]
    private void CmdSpawnTemporaryStone(Vector3 position)
    {
        RpcSpawnTemporaryStone(position);
    }
    
    [ClientRpc]
    private void RpcSpawnTemporaryStone(Vector3 position)
    {
        if (_stoneVFX == null) return;

        VisualEffect tempVFX = Instantiate(_stoneVFX, position, Quaternion.identity);

        tempVFX.gameObject.SetActive(true);
        tempVFX.Stop();
        tempVFX.Play();

        tempVFX.outputEventReceived += (args) =>
        {
            OnTemporaryVFXOutput(tempVFX, position, args);
        };
    }
    
    private void OnTemporaryVFXOutput(VisualEffect tempVFX, Vector3 castPoint, VFXOutputEventArgs args)
    {
        if (args.nameId == _onFinishedEventId)
        {
            if (isOwned)
                ApplyAreaDamage(castPoint);
        }

        if (args.nameId == _onDecalsEventId)
        {
            Destroy(tempVFX.gameObject);
        }
    }

    private void ApplyAreaDamage(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, _aoeRadius, Targeting.Layer);

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
