using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class ScratchClaws : Skill
{
    [SerializeField] private Animator animator;
    [SerializeField] private Character _playerLinks;
    [SerializeField] private float _bleedingDuration = 3f;
    [SerializeField, Range(0, 1f)] private float _bleedingChance = 0.15f;

    //private IDamageable _target;
    //private Character _runtimeTarget;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => GetTarget() != null && Vector3.Distance(GetTarget().transform.position, transform.position) <= Radius && NoObstacles(GetTarget().transform.position, transform.position, _obstacle);

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0 && targetInfo.GetTargets()[0] is Character character) SetTarget(character);
    }

    private void OnEnable()
    {
        Damage = UnityEngine.Random.Range(1f, 4f);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        if (Damage <= 0) Damage = UnityEngine.Random.Range(1f, 4f);
        //_runtimeTarget = null;

        while (GetTarget() == null && !_disactive)
        {
            if (GetMouseButton)
            {
                FindTarget();
               // _target = GetRaycastTarget();

                /*if (_target != null)
                {
                    if (_target is Character characterTarget) _runtimeTarget = characterTarget;
                }*/
            }
            yield return null;
        }

        TargetInfo info = new();
        info.AddTarget(GetTarget());
        targetDataSavedCallback?.Invoke(info);

        animator.SetTrigger("AttackScared");
    }

    protected override IEnumerator CastJob()
    {
        if (GetTarget() == null) yield break;
        CmdApplyScratch(GetTarget().gameObject);

        yield return null;
    }

    protected override void ClearData()
    {
        ClearTarget();
        //_target = null;
        Damage = 0;
    }

    [Command]
    private void CmdApplyScratch(GameObject target)
    {
        if (target == null) return;
        
        Damage damage = new Damage
        {
            Value = Damage,
            Type = DamageType.Physical
        };

        ApplyDamage(damage, target);
        if (GetTarget() != null && UnityEngine.Random.value <= _bleedingChance) GetTarget().CharacterState.AddState(States.Bleeding, _bleedingDuration, Damage, _playerLinks.gameObject, name);
    }
}
