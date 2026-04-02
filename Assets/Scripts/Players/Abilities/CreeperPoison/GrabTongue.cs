using Mirror;
using System;
using System.Collections;
using UnityEngine;

public class GrabTongue : Skill
{
    [Header("Refs")]
    [SerializeField] private Character _player;
    [SerializeField] private GrabTongueProjectile _projectile;

    [Header("Settings")]
    [SerializeField] private float _maxDistance = 3f;
    [SerializeField] private float _throwDuration = 0.2f;
    [SerializeField] private float _pullDuration = 0.6f;

    private Character _target;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;

    protected override bool IsCanCast =>
        _target != null &&
        Vector3.Distance(transform.position, _target.transform.position) <= _maxDistance;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callback)
    {
        TargetInfo info = new TargetInfo();

        while (true)
        {
            if (GetMouseButton)
            {
                Targeting.FindTempTarget(Targeting.GetMousePoint(), _maxDistance);

                var temp = Targeting.GetTempTarget()?.Targetable as Character;

                if (temp != null && temp != _hero)
                {
                    _target = temp;

                    info.AddTarget(_target);
                    info.Points.Add(_target.transform.position);

                    callback?.Invoke(info);
                    yield break;
                }
            }

            yield return null;
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo?.GetTargets()?.Count > 0)
        {
            _target = targetInfo.GetTargets()[0] as Character;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target == null) yield break;

        CancelTargetSkills(_target);

        bool canPull = _target.CharacterState.CheckForState(States.InAir);

        Vector3 start = transform.position;
        Vector3 end = _target.transform.position;

        CmdSpawnProjectile(_target.netIdentity, start, end, canPull);

        yield return null;
    }

    private void CancelTargetSkills(Character target)
    {
        if (target == null) return;

        foreach (var skill in target.Abilities.Skills)
        {
            if (skill == null) continue;

            if (skill.IsCasting || skill.IsPreparing) skill.CmdCancelActiveSkill();
        }
    }

    [Command]
    private void CmdSpawnProjectile(NetworkIdentity targetId, Vector3 start, Vector3 end, bool canPull)
    {
        if (targetId == null) return;

        GameObject go = Instantiate(_projectile.gameObject, start, Quaternion.identity);

        var proj = go.GetComponent<GrabTongueProjectile>();
        var target = targetId.GetComponent<Character>();

        proj.InitializationProjectile(_player, target, start, end, false);

        NetworkServer.Spawn(go);
    }

    protected override void ClearData()
    {
        _target = null;
    }
}