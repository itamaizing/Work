using Mirror;
using System.Collections;
using UnityEngine;

public class TestDispel : Skill
{
    private Character _target;

    protected override bool IsCanCast => _target != null && Vector3.Distance(_target.transform.position, transform.position) <= Radius;

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();
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
        targetState.ServerDispelStates(StateType.Magic, targetTeamIndex, playerTeamIndex, true);
    }

    private Character GetRaycastTarget()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, 100f, TargetsLayers);

        if (hit.collider != null && hit.collider.TryGetComponent(out Character target))
        {
            return target;
        }
        return null;
    }
}
