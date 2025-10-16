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

    private Character _target;
    private Character _runtimeTarget;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius && NoObstacles(_target.transform.position, transform.position, _obstacle);

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Targets.Count > 0 && targetInfo.Targets[0] is Character character) _target = character;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> targetDataSavedCallback)
    {
        while (_target == null && !_disactive)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget(true);
                if (_target != null) _runtimeTarget = _target;
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
        CmdApplyScratch(_target);

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    [Command]
    private void CmdApplyScratch(Character character)
    {

        float dmgValue = UnityEngine.Random.Range(1f, 4f);
        Damage damage = new Damage
        {
            Value = dmgValue,
            Type = DamageType.Physical
        };

        ApplyDamage(damage, character.gameObject);
        if (UnityEngine.Random.value <= _bleedingChance) character.CharacterState.AddState(States.Bleeding, _bleedingDuration, Damage, _playerLinks.gameObject, name);
    }
}
