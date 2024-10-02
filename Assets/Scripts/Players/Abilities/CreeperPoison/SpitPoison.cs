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

    #region PoisonCloud
    [SerializeField] private PoisonDamagingCloudPrefab _poisonDamagingCloudPrefab;
    [SerializeField] private PoisonHealingCloudPrefab _poisonHealingCloudPrefab;
    private PoisonDamagingCloudPrefab _poisonDamagingCloud;
    private PoisonHealingCloudPrefab _poisonHealingCloud;
    private float _durationPoisonCloud = 6f;
    #endregion

    private float _originalCooldown;
    private float _angleRotation;

    private Vector3 _mousePos = Vector3.positiveInfinity;

    private Character _currentTarget;

    private bool _isActiveHealingSpitPoison;
    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;
    private bool _isHealingPoisonCloud = false;
    private bool _isAlly;

    protected override bool IsCanCast => CheckCanCast();

    protected void Start()
    {
        _originalCooldown = _cooldownTime;
    }

    protected override IEnumerator PrepareJob()
    {
       if (_healingSpitPoison.IsActive)
       {
           _isActiveHealingSpitPoison = _healingSpitPoison.IsActive;
       }
       else
       {
           _isActiveHealingSpitPoison = _healingSpitPoison.IsActive;
       }

        while (_currentTarget == null && float.IsPositiveInfinity(_mousePos.x))
        {
            if (GetMouseButton)
            {
                _currentTarget = GetRaycastTarget(true);
                Debug.Log("PrepareJob / _currentTarget = " + _currentTarget);
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
        _isHealingPoisonCloud = false;
        _isActiveHealingSpitPoison = false;
        _isOriginalTargetAllies = false;
        _isOriginalTargetEnemy = false;
        _isOriginalTargetPlayer = false;

        _currentTarget = null; 
        _mousePos = Vector3.positiveInfinity;
    }

    private void CooldownChange()
    {
        if (_isActiveHealingSpitPoison)
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
                if (_healPoisonCloud.IsActive && _isActiveHealingSpitPoison)
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
                if (_isActiveHealingSpitPoison && _isActiveHealingSpitPoison)
                {
                    if (_healPoisonCloud.IsActive)
                    {
                        _isHealingPoisonCloud = true;
                        Debug.Log($"ChooseTarget / Allies / _isHealingPoisonCloud = {_isHealingPoisonCloud}");
                    }
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                Debug.Log("Target == Enemy");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
                if (_healPoisonCloud.IsActive && _isActiveHealingSpitPoison)
                {
                    _isHealingPoisonCloud = false;
                    Debug.Log($"ChooseTarget / Enemy / _isHealingPoisonCloud = {_isHealingPoisonCloud}");
                }
            }
        }
        else
        {
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
        if (_currentTarget == null)
            return Vector3.Distance(_mousePos, transform.position) <= Radius;

        return Vector3.Distance(_mousePos, transform.position) <= Radius ||
               Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius;
    }

    private void Shoot()
    {
        if (_currentTarget != null)
        {
            CmdInstantiateProjectileToTarget(_currentTarget.gameObject, _angleRotation, _player.Stamina.CurrentValue, 
                _isActiveHealingSpitPoison, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

            CmdApplyPoisonCloud(_isHealingPoisonCloud, _durationPoisonCloud);
        }
        else
        {
            CmdInstantiateProjectileToPoint(_mousePos, _angleRotation, _player.Stamina.CurrentValue, 
                _isActiveHealingSpitPoison, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

            CmdApplyPoisonCloud(_isHealingPoisonCloud, _durationPoisonCloud);
        }
    }

    #region Command Methods

    [Command]
    private void CmdInstantiateProjectileToTarget(GameObject target, float angleRotation, float manaValue, 
        bool isActiveHealingSpitPoison, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        PlayerTeamIndex(target);
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.Euler(0, 0, angleRotation));

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_player, this, manaValue, isActiveHealingSpitPoison, isTargetPlayer, isTargetEnemy, isTargetAllies, _isAlly);

        projectile.MoveBallToTarget(target.transform.position);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdInstantiateProjectileToPoint(Vector3 point, float angleRotation, float manaValue, 
        bool isActiveHealingSpitPoison, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        PlayerTeamIndex(_player.gameObject);
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.Euler(0, 0, angleRotation));

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_player, this, _player.Stamina.CurrentValue, _isActiveHealingSpitPoison, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies, _isAlly);

        projectile.MoveBallOnMaxDistance(point);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdApplyPoisonCloud(bool isHealingCloud, float duration)
    {
        //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud");
        if (!isHealingCloud)
        {
            if (_poisonDamagingCloud == null && _poisonDamagingCloudPrefab.PoisonDamageCloud == null)
            {
                //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud / if (_poisonDamagingCloud == null)");
                _player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonDamagingCloud = Instantiate(_poisonDamagingCloudPrefab, transform.position, Quaternion.identity);
                _poisonDamagingCloudPrefab.PoisonDamageCloud = _poisonDamagingCloud;

                SceneManager.MoveGameObjectToScene(_poisonDamagingCloudPrefab.PoisonDamageCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonDamagingCloudPrefab.PoisonDamageCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.gameObject);
                Debug.Log("SpitPoison / CmdApplyPoisonCloud / if / _poisonDamagingCloud = " + _poisonDamagingCloud);
                Debug.Log("SpitPoison / CmdApplyPoisonCloud / if / _poisonDamagingCloudPrefab.PoisonDamageCloud = " + _poisonDamagingCloudPrefab.PoisonDamageCloud);
                //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud / if (_poisonDamagingCloud == null) / _poisonDamagingCloud = " + _poisonDamagingCloud);
            }
            else
            {
                Debug.Log("SpitPoison / CmdApplyPoisonCloud / else / _poisonDamagingCloud = " + _poisonDamagingCloudPrefab.PoisonDamageCloud);
                //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud / else");
                _player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();
            }
        }
        else
        {
            if (_poisonHealingCloud == null && _poisonHealingCloudPrefab.PoisonHealingCloud == null)
            {
                //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud / if (_poisonHealingCloud == null)");
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonHealingCloud = Instantiate(_poisonHealingCloudPrefab, transform.position, Quaternion.identity);
                _poisonHealingCloudPrefab.PoisonHealingCloud = _poisonHealingCloud;

                SceneManager.MoveGameObjectToScene(_poisonHealingCloudPrefab.PoisonHealingCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonHealingCloudPrefab.PoisonHealingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();

                NetworkServer.Spawn(_poisonHealingCloud.gameObject);

                //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud / if (_poisonHealingCloud == null) / _poisonHealingCloud = " + _poisonHealingCloud);
            }
            else
            {
                //Debug.Log("SpitPoison / CmdTestApplyPoisonCloud / else");
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();
            }
        }
        RpcApply(_poisonDamagingCloudPrefab.PoisonDamageCloud, _poisonHealingCloudPrefab.PoisonHealingCloud, duration, isHealingCloud);
    }


    #endregion

    #region ClientRpc Methods

    [ClientRpc]
    private void RpcApply(PoisonDamagingCloudPrefab poisonDamagingCloud, PoisonHealingCloudPrefab poisonHealingCloud, float duration, bool isHealingCloud)
    {
        //Debug.Log("PoisonBall / RpcApply / poisonDamagingCloud = " + poisonDamagingCloud);
            Debug.Log("SpitPoison / RpcApply / if (poisonDamagingCloud != null) = " + poisonDamagingCloud);
        if (poisonDamagingCloud != null)
        {
            poisonDamagingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
            poisonDamagingCloud.AddStack();
        }

        if (poisonHealingCloud != null && isHealingCloud)
        {
           // Debug.Log("PoisonBall / RpcApply / if (poisonHealingCloud != null) = " + poisonHealingCloud);
            poisonHealingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
            poisonHealingCloud.AddStack();
        }
    }

    [ClientRpc]
    private void PlayerTeamIndex(GameObject target)
    {
        int teamIndex = target.GetComponentInParent<UserNetworkSettings>().TeamIndex;
        var localPlayer = NetworkClient.connection.identity.GetComponent<UserNetworkSettings>();
        _isAlly = localPlayer.TeamIndex == teamIndex;
    }

    #endregion
}
