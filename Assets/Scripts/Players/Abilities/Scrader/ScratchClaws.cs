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

    private IDamageable _target;
    private Character _runtimeTarget;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0 && targetInfo.Targets[0] is Character character) _target = character;
    }

    private void OnEnable()
    {
        Damage = UnityEngine.Random.Range(1f, 4f);
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        if (Damage <= 0) Damage = UnityEngine.Random.Range(1f, 4f);
        _runtimeTarget = null;

        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();

                if (_target != null)
                {
                    if (_target is Character characterTarget) _runtimeTarget = characterTarget;
                }
            }
            yield return null;
        }

        TargetInfo info = new();
        info.Targets.Add(_runtimeTarget);
        targetDataSavedCallback?.Invoke(info);

        animator.SetTrigger("AttackScared");
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield break;
        CmdApplyScratch(_target.gameObject);

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
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
        if (_runtimeTarget != null && UnityEngine.Random.value <= _bleedingChance) _runtimeTarget.CharacterState.AddState(States.Bleeding, _bleedingDuration, Damage, _playerLinks.gameObject, name);
    }
}
