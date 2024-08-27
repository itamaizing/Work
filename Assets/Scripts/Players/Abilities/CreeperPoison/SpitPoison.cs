using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Drawing;

public class SpitPoison : Skill
{
    [Header("Talents")]
    [SerializeField] private HealingSpitPoison _healingSpitPoison;
    [SerializeField] private HealPoisonCloud _healPoisonCloud;

    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _player;

    private float _originalCooldown;
    private float _angleRotation;
    private float _durationPoisonCloud = 6f;

    private Vector3 _mousePos = Vector3.positiveInfinity;

    private Character _currentTarget;

    private bool _isActiveTalent;
    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;
    private bool _isHealingPoisonCloud = false;

    public bool Enabled;

    protected override bool IsCanCast => CheckCanCast();

    protected void Start()
    {
        _originalCooldown = _cooldownTime;
    }

    protected override IEnumerator PrepareJob()
    {
       if (_healingSpitPoison.IsActive)
       {
           _isActiveTalent = _healingSpitPoison.IsActive;
       }
       else
       {
           _isActiveTalent = _healingSpitPoison.IsActive;
       }

        while (_currentTarget == null && float.IsPositiveInfinity(_mousePos.x))
        {
            if (Input.GetMouseButton(0))
            {
                _currentTarget = GetRaycastTarget(true);
                ChooseTarget();

                _mousePos = GetMousePoint();
                CalculateAngleRotation();
            }
            CooldownChange();
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        Shoot();
        yield return null;
    }

    protected override void ClearData()
    {
        _currentTarget = null; 
        _mousePos = Vector3.positiveInfinity;
    }

    private void CooldownChange()
    {
        if (_isActiveTalent)
        {
            if (_isOriginalTargetAllies || _isOriginalTargetPlayer)
            {
                if (_cooldownTime == _originalCooldown)
                {
                    _cooldownTime /= 3;
                }
            }
            else
            {
                _cooldownTime = _originalCooldown;
            }
        }
        else
        {
            _cooldownTime = _originalCooldown;
        }
    }
    private void CalculateAngleRotation()
    {
        Vector3 rotationDirection = _mousePos - _player.transform.position;
        _angleRotation = Mathf.Atan2(rotationDirection.y, rotationDirection.x) * Mathf.Rad2Deg - 90f;
    }

    private void ChooseTarget()
    {
        Debug.Log("ChooseTarget");
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _player.gameObject)
            {
                Debug.Log("Target == Player");
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
                if (_healPoisonCloud.IsActive)
                {
                    _isHealingPoisonCloud = true;
                    Debug.Log($"ChooseTarget / Player / _isHealingPoisonCloud = {_isHealingPoisonCloud}");
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                Debug.Log("Target == Allies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
                if (_healPoisonCloud.IsActive)
                {
                    _isHealingPoisonCloud = true;
                    Debug.Log($"ChooseTarget / Allies / _isHealingPoisonCloud = {_isHealingPoisonCloud}");
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("Target == Enemy");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
                if (_healPoisonCloud.IsActive)
                {
                    _isHealingPoisonCloud = false;
                    Debug.Log($"ChooseTarget / Enemy / _isHealingPoisonCloud = {_isHealingPoisonCloud}");

                }
            }
        }
        else
        {
            Debug.Log($"Else ChooseTarget / _currentTarget == {_currentTarget}");

            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;

            if (_mousePos != Vector3.zero)
            {
                _currentTarget = null;
            }
        }
    }

    private bool CheckCanCast()
    {
        Debug.Log("CheckCanCast");

        if (_currentTarget == null)
            return Vector3.Distance(_mousePos, transform.position) <= Radius;

        return Vector3.Distance(_mousePos, transform.position) <= Radius ||
               Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius;
    }

    private void Shoot()
    {
        Debug.Log("Shoot SpitPoison");
        if (_currentTarget != null)
        {
            CmdInstantiateProjectileToTarget(_currentTarget.gameObject, _angleRotation, _player.Stamina.Value, 
                _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
            CmdApplyCloudPoison(_healPoisonCloud.IsActive, _isHealingPoisonCloud);
        }
        else
        {
            CmdInstantiateProjectileToPoint(_mousePos, _angleRotation, _player.Stamina.Value, 
                _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
            CmdApplyCloudPoison(_healPoisonCloud.IsActive, _isHealingPoisonCloud);
        }
    }

    #region Command Methods

    [Command]
    private void CmdInstantiateProjectileToTarget(GameObject target, float angleRotation, float manaValue, 
        bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.Euler(0, 0, angleRotation));

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_player, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);

        projectile.MoveBallToTarget(target.transform.position);

        NetworkServer.Spawn(item);

        RpcInstantiateProjectile(target, projectile, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    }

    [Command]
    private void CmdInstantiateProjectileToPoint(Vector3 point, float angleRotation, float manaValue, 
        bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.Euler(0, 0, angleRotation));

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_player, _player.Stamina.Value, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

        projectile.MoveBallOnMaxDistance(point);

        NetworkServer.Spawn(item);

        RpcInstantiateProjectileToPoint(point, projectile, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    }

    [Command]
    private void CmdApplyCloudPoison(bool isActiveTalent, bool isHealingCloud)
    {
        Debug.Log($"SpitPoison / CmdApplyCloudPoison");
        Debug.Log($"SpitPoison / ApplyCloudPoison / if (_healPoisonCloud.IsActive = {isActiveTalent} && _isHealingPoisonCloud = {isHealingCloud})");
        if (isActiveTalent && isHealingCloud)
        {
            _player.CharacterState.CmdAddState(States.HealingPoisonCloud, _durationPoisonCloud, 0);
        }
        else
        {
            _player.CharacterState.CmdAddState(States.PoisonCloud, _durationPoisonCloud, 0);
        }
    }

    #endregion

    #region ClientRpc Methods

    [ClientRpc]
    private void RpcInstantiateProjectile(GameObject target, SpitPoisonProjectile projectile, float manaValue,
        bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        projectile.InitializationProjectile(_player, _player.Stamina.Value, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
    }

    [ClientRpc]
    private void RpcInstantiateProjectileToPoint(Vector3 point, SpitPoisonProjectile projectile, float manaValue,
        bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    { 
        projectile.InitializationProjectile(_player, _player.Stamina.Value, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
    }

    #endregion

}
