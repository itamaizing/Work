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
    private float _minDistance = 0.1f;
    private float _baseIncreasedDamage = 0.05f;
    private float _maxIncreasedDamage = 0.2f;
    private float _increaseDamageStandingStill = 0.1f;
    private float _additionalDamageInProcentage;

    private bool _isTarget = false;
    private bool _isJumpDone = false;
    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    public bool IsJumpDone { get => _isJumpDone; set => _isJumpDone = value; }

    protected override bool IsCanCast => CheckCanCast();

    protected override void ClearData()
    {
        _target = null;
        _mousePosition = Vector3.positiveInfinity;
        _isTarget = false;
    }

    protected override IEnumerator PrepareJob()
    {
        _castDeley = _delayBeforeJump;

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
        _isJumpDone = true;

        float distanceBetweenPlayerAndTarget = Vector2.Distance(_target.transform.position, _player.transform.position);

        float normalizedDistance = NormalizeDistance(distanceBetweenPlayerAndTarget);

        if (normalizedDistance < _minDistance)
        {
            _additionalDamageInProcentage = _increaseDamageStandingStill;
        }
        else if (normalizedDistance >= _distanceJump)
        {
            _additionalDamageInProcentage = _maxIncreasedDamage;
        }
        else
        {
            int wholeValues = Mathf.FloorToInt(normalizedDistance);

            _additionalDamageInProcentage = wholeValues * _baseIncreasedDamage;

            _additionalDamageInProcentage = Mathf.Clamp(_additionalDamageInProcentage, _baseIncreasedDamage, _maxIncreasedDamage);
        }

        Vector3 direction = (_target.transform.position - transform.position).normalized;

        CmdExecuteJump(_player.gameObject, _target.gameObject, direction, _additionalDamageInProcentage);

        Invoke("ResetBool", 1f);
    }

    private float NormalizeDistance(float distance)
    {
        float minDistance = 2.2f;
        float maxDistance = 8f;
        float newMaxDistance = _distanceJump;
        float newMinDistance = _minDistance;

        float normizedDistance = (distance - minDistance) / (maxDistance - minDistance) * (newMaxDistance - newMinDistance) + newMinDistance;

        return normizedDistance;
    }

    private void ResetBool()
    {
        _isJumpDone = false;
    }

    [Command]
    private void CmdExecuteJump(GameObject player, GameObject target, Vector3 direction, float additionalDamage)
    {
        MoveComponent playerMove = player.GetComponent<MoveComponent>();

        playerMove.TargetRpcDoMove((Vector3)_player.transform.position + direction * (_distanceJump * GlobalVariable.cellSize), _durationJump);
        DamageDeal(target, additionalDamage);
    }

    [ClientRpc]
    private void DamageDeal(GameObject target, float additionalDamage)
    {
        _cheliceraeStrike.DealDamage(target, additionalDamage);
    }
}
