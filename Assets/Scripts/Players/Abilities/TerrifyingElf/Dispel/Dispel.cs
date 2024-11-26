using Mirror;
using System.Collections;
using UnityEngine;

public class Dispel : Skill
{
    private Character _target;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetNearestTargetInRadius();
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            var targetCharacter = _target.GetComponent<CharacterState>();
            if (targetCharacter != null)
            {
                CmdDispelState(targetCharacter, _target.NetworkSettings.TeamIndex, Hero.NetworkSettings.TeamIndex);
            }
        }

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    [Command]
    private void CmdDispelState(CharacterState targetState, int targetTeamIndex, int playerTeamIndex)
    {
        targetState.DispelStates(StateType.Magic, targetTeamIndex, playerTeamIndex, true);
    }

    private Character GetNearestTargetInRadius()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, Radius, TargetsLayers);
        Character nearestTarget = null;
        float shortestDistance = Radius;

        foreach (var collider in colliders)
        {
            if (collider.TryGetComponent(out Character character) && character != Hero)
            {
                float distance = Vector3.Distance(transform.position, character.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestTarget = character;
                }
            }
        }
        return nearestTarget;
    }
}
