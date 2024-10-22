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
    [SerializeField] private RestorationOfGlands _restorationOfGlands;
    [SerializeField] private HealingSpitPoison _healingSpitPoison;
    [SerializeField] private HealPoisonCloud _healPoisonCloud;
    [SerializeField] private TransparentPoisons _transparentPoisons;

    [Header("Ability Properties")]
    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _player;

    #region PoisonCloud
    [SerializeField] private PoisonDamagingCloudPrefab _poisonDamagingCloudPrefab;
    [SerializeField] private PoisonHealingCloudPrefab _poisonHealingCloudPrefab;
    private PoisonDamagingCloudPrefab _poisonDamagingCloud;
    private PoisonHealingCloudPrefab _poisonHealingCloud;
    private float _durationPoisonCloud = 6f;
    #endregion

    private int _poisonBoneStack = 0;

    private float _originalCooldown;
    private float _angleRotation;

    private Vector3 _mousePos = Vector3.positiveInfinity;

    private Character _currentTarget;

    private bool _isActiveHealingSpitPoison;
    private bool _isHealingPoisonCloud = false;
    private bool _isPlayerInvisible = false;

    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;

    public RestorationOfGlands RestorationOfGlandsTalent { get; set; }
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }
    protected override bool IsCanCast => CheckCanCast();

    protected void Start()
    {
        _originalCooldown = _cooldownTime;
    }

    protected override IEnumerator PrepareJob()
    {
        CheckActiveTalents();

        while (_currentTarget == null && float.IsPositiveInfinity(_mousePos.x))
        {
            if (GetMouseButton)
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
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _player.gameObject)
            {
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
                if (_healPoisonCloud.Data.IsOpen && _isActiveHealingSpitPoison)
                {
                    _isHealingPoisonCloud = true;
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
                if (_isActiveHealingSpitPoison && _isActiveHealingSpitPoison)
                {
                    if (_healPoisonCloud.Data.IsOpen)
                    {
                        _isHealingPoisonCloud = true;
                    }
                }
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
                if (_healPoisonCloud.Data.IsOpen && _isActiveHealingSpitPoison)
                {
                    _isHealingPoisonCloud = false;
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

    private void CheckActiveTalents()
    {
        if (_transparentPoisons.Data.IsOpen && _player.IsInvisible)
        {
            _isPlayerInvisible = true;
        }
        else
        {
            _isPlayerInvisible = false;
        }

        if (_healingSpitPoison.Data.IsOpen)
        {
            _isActiveHealingSpitPoison = _healingSpitPoison.Data.IsOpen;
        }
        else
        {
            _isActiveHealingSpitPoison = _healingSpitPoison.Data.IsOpen;
        }
    }

    private bool CheckCanCast()
    {
        if (_currentTarget == null)
            return Vector3.Distance(_mousePos, transform.position) <= Radius && NoObstacles(_mousePos, _obstacle);

        return Vector3.Distance(_mousePos, transform.position) <= Radius && NoObstacles(_mousePos, _obstacle) ||
               Vector3.Distance(_currentTarget.transform.position, transform.position) <= Radius && NoObstacles(_currentTarget.transform.position, _obstacle);
    }

    private void Shoot()
    {
        if (_currentTarget != null)
        {
            CmdInstantiateProjectileToTarget(_currentTarget.gameObject, _angleRotation, _player.Stamina.CurrentValue, 
                _isActiveHealingSpitPoison, _isPlayerInvisible,
                _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

            //CmdApplyPoisonCloud(_isHealingPoisonCloud, _durationPoisonCloud);
        }
        else
        {
            CmdInstantiateProjectileToPoint(_mousePos, _angleRotation, _player.Stamina.CurrentValue, 
                _isActiveHealingSpitPoison, _isPlayerInvisible,
                _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);

            //CmdApplyPoisonCloud(_isHealingPoisonCloud, _durationPoisonCloud);
        }
        _player.Move.CanMove = true;
    }

    #region Command Methods

    [Command]
    private void CmdInstantiateProjectileToTarget(GameObject target, float angleRotation, float manaValue, 
        bool isActiveHealingSpitPoison, bool isPlayerInvisible, 
        bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        RestorationOfGlandsTalent = _restorationOfGlands;
        Debug.Log("SpitPoison / CmdInstTarget / RestorationOfGlandsTalent = " + RestorationOfGlandsTalent);

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.Euler(0, 0, angleRotation));

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_player, this, manaValue, 
            isActiveHealingSpitPoison, isPlayerInvisible, 
            isTargetPlayer, isTargetEnemy, isTargetAllies, PoisonBoneStack);

        projectile.MoveBallToTarget(target.transform.position);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdInstantiateProjectileToPoint(Vector3 point, float angleRotation, float manaValue, 
        bool isActiveHealingSpitPoison, bool isPlayerInvisible, 
        bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        RestorationOfGlandsTalent = _restorationOfGlands;
        Debug.Log("SpitPoison / CmdInstPoint / RestorationOfGlandsTalent = " + RestorationOfGlandsTalent);

        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.Euler(0, 0, angleRotation));

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_player, this, _player.Stamina.CurrentValue, 
            isActiveHealingSpitPoison, isPlayerInvisible, 
            isTargetPlayer, isTargetEnemy, isTargetAllies, PoisonBoneStack);

        projectile.MoveBallOnMaxDistance(point);

        NetworkServer.Spawn(item);
    }

    [Command]
    private void CmdApplyPoisonCloud(bool isHealingCloud, float duration)
    {
        if (!isHealingCloud)
        {
            if (_poisonDamagingCloud == null && _poisonDamagingCloudPrefab.PoisonDamageCloud == null)
            {
                _player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonDamagingCloud = Instantiate(_poisonDamagingCloudPrefab, transform.position, Quaternion.identity);
                _poisonDamagingCloudPrefab.PoisonDamageCloud = _poisonDamagingCloud;

                SceneManager.MoveGameObjectToScene(_poisonDamagingCloudPrefab.PoisonDamageCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonDamagingCloudPrefab.PoisonDamageCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();

                NetworkServer.Spawn(_poisonDamagingCloud.gameObject);
            }
            else
            {

                _player.CharacterState.AddState(States.PoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonDamagingCloudPrefab.PoisonDamageCloud.AddStack();
            }
        }
        else
        {
            if (_poisonHealingCloud == null && _poisonHealingCloudPrefab.PoisonHealingCloud == null)
            {
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);

                _poisonHealingCloud = Instantiate(_poisonHealingCloudPrefab, transform.position, Quaternion.identity);
                _poisonHealingCloudPrefab.PoisonHealingCloud = _poisonHealingCloud;

                SceneManager.MoveGameObjectToScene(_poisonHealingCloudPrefab.PoisonHealingCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonHealingCloudPrefab.PoisonHealingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();

                NetworkServer.Spawn(_poisonHealingCloud.gameObject);

            }
            else
            {
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
        if (poisonDamagingCloud != null)
        {
            poisonDamagingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
            poisonDamagingCloud.AddStack();
        }

        if (poisonHealingCloud != null && isHealingCloud)
        {
            poisonHealingCloud.InitializationProjectile(_player, 5, duration, 3.5f, Name);
            poisonHealingCloud.AddStack();
        }
    }

    #endregion
}
