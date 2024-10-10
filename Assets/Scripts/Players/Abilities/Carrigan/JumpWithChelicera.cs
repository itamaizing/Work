using Mirror;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class JumpWithChelicera : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private CheliceraStrike _cheliceraeStrike;

    [SerializeField] private float _distanceJump;
    [SerializeField] private float _durationJump;

    private Character _target;
    private Vector3 _mousePosition = Vector3.positiveInfinity;

    private float _delayBeforeJump = 0.3f;

    private float _baseIncreasedDamage = 0.05f;
    private float _maxIncreasedDamage = 0.2f;
    private float _increaseDamageStandingStill = 0.1f;

    private bool _isTarget = false;
    //private Coroutine

    protected override bool IsCanCast => CheckCanCast();

    protected override void ClearData()
    {
        _target = null;
        _mousePosition = Vector3.positiveInfinity;
        _isTarget = false;
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null && float.IsPositiveInfinity(_mousePosition.x))
        {
            if (GetMouseButton)
            {
                _target = GetRaycastTarget();
                _mousePosition = GetMousePoint();

                if (_target != null)
                {
                    _isTarget = true;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_isTarget)
        {
            _castDeley = _delayBeforeJump;
            StartCastDeleyCoroutine();
            ExecuteJump();
        }
         yield return null;  
    }

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector2.Distance(_mousePosition, transform.position) <= Radius && NoObstacles(_mousePosition, _obstacle);
        else
            return Vector2.Distance(_mousePosition, transform.position) <= Radius && NoObstacles(_mousePosition, _obstacle)
                || Vector2.Distance(_target.transform.position, transform.position) <= Radius 
                && NoObstacles(_target.transform.position, _obstacle);
    }

    private void ExecuteJump()
    {
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        CmdExecuteJump(_player.gameObject, _target.gameObject, direction);
    }

    [Command]
    private void CmdExecuteJump(GameObject player, GameObject target, Vector3 direction)
    {
        MoveComponent playerMove = player.GetComponent<MoveComponent>();

        playerMove.TargetRpcDoMove((Vector3)_player.transform.position + direction * _distanceJump, _durationJump);
        DamageDeal(target);
    }

    [ClientRpc]
    private void DamageDeal(GameObject target)
    {
        _cheliceraeStrike.DealDamage(target);
    }
}
