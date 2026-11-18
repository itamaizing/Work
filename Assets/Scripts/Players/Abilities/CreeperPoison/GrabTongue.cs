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
    [SerializeField] private float _throwDuration = 0.2f;
    [SerializeField] private float _pullDuration = 0.6f;

    private Character _castTarget;

    private const float SearchMouseRadius = 0.2f;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => 0;

    public override void Init(SkillRenderer render, Character hero)
    {
        base.Init(render, hero);

        if (_player == null)
            _player = hero;
    }


    protected override IEnumerator CastJob()
    {
        _castTarget = Targeting.GetTarget()?.Character;
        if (_castTarget == null)
            yield break;

        CancelTargetSkills(_castTarget);

        bool canPull = _castTarget.CharacterState.CheckForState(States.InAir);

        Vector3 end = _castTarget.transform.position;
        if (_castTarget.AttachmentPoints != null && _castTarget.AttachmentPoints.TryGetPoint(AttachmentPointType.Torso, out Transform torso))
            end = torso.position;

        CmdSpawnProjectile(_castTarget.netIdentity, end, canPull);

        yield return null;
    }

    private void CancelTargetSkills(Character target)
    {
        if (target == null)
            return;

        foreach (var skill in target.Abilities.Skills)
        {
            if (skill == null)
                continue;

            skill.CmdCancelActiveSkill();
        }
    }

    [Command]
    private void CmdSpawnProjectile(NetworkIdentity targetId, Vector3 end, bool canPull)
    {
        if (targetId == null)
            return;
        
        Character target = targetId.GetComponent<Character>();

        if (target == null)
            return;

        Transform origin = _player.transform;
        if (_player.AttachmentPoints != null && _player.AttachmentPoints.TryGetPoint(AttachmentPointType.Head, out Transform head))
            origin = head;
        Vector3 start = origin.position;

        GameObject go = Instantiate(_projectile.gameObject, start, Quaternion.identity);

        GrabTongueProjectile proj = go.GetComponent<GrabTongueProjectile>();

        if (proj == null)
            return;

        proj.Init(_player, target, end);

        NetworkServer.Spawn(go);

        RpcInitGrabTongueProjectile(go, target.gameObject, end);
    }

    [ClientRpc]
    private void RpcInitGrabTongueProjectile(GameObject projectileObject, GameObject targetObject, Vector3 end)
    {
        if (projectileObject == null || targetObject == null)
            return;

        GrabTongueProjectile proj = projectileObject.GetComponent<GrabTongueProjectile>();
        Character target = targetObject.GetComponent<Character>();

        if (proj == null || target == null)
            return;

        proj.Init(_player, target, end);
    }

    protected override void ClearData()
    {
        _castTarget = null;

        Targeting.ClearTarget();
        Targeting.ClearTempTarget();

        if (Hero != null)
            Hero.Move.StopLookAt();
    }
}
