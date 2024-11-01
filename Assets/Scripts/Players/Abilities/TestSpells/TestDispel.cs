using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDispel : Skill
{
    //[SerializeField] private float damageIfMinion;
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
                //if (_target.GetComponent<MinionComponent>() != null)
                //{
                //    if (_target.Data.MagPhys == "Magic")
                //        ApplyDamage(damageIfMinion * 2, DamageType.Magical, _target);
                //    else
                //        ApplyDamage(damageIfMinion, DamageType.Magical, _target);
                //}

                CmdRemoveState(targetCharacter, _target.NetworkSettings.TeamIndex, Hero.NetworkSettings.TeamIndex);
            }
        }

        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    [Command]
    private void CmdRemoveState(CharacterState targetState, int targetTeamIndex, int playerTeamIndex)
    {
        ClientRpcRemoveState(targetState, targetTeamIndex, playerTeamIndex);
    }

    [ClientRpc]
    private void ClientRpcRemoveState(CharacterState targetState, int targetTeamIndex, int playerTeamIndex)
    {
        if (targetState == null)
        {
            Debug.LogError("Target state is null. Cannot remove state.");
            return;
        }

        targetState.DispelStates(targetTeamIndex, playerTeamIndex);
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

    //private void ApplyDamage(float damage, DamageType damageType, Character target)
    //{
    //    Damage _damage = new Damage
    //    {
    //        Value = damage,
    //        Type = damageType,
    //    };

    //    target.Health.CmdTryTakeDamage(_damage, this.gameObject);
    //}
}
