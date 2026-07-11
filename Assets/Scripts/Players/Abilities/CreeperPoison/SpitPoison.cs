using Mirror;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public struct SpitPoisonSpawnPointInfo : NetworkMessage
{
    public float SpawnPointX;
    public float SpawnPointY;
    public float SpawnPointZ;
}

public class SpitPoison : Skill, IAltAbility
{
    [Header("Talents")]
    //[SerializeField] private RestorationOfGlands _restorationOfGlands;
    //[SerializeField] private HealingSpitPoison _healingSpitPoison;
    //[SerializeField] private HealingPoisonCloud _healPoisonCloud;
    //[SerializeField] private TransparentPoisons _transparentPoisons;
    //[SerializeField] private EatingAcid _eatingAcid;

    [Header("Ability Properties")]
    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _player;
    [SerializeField] private GameObject _spawnPoint;
    [SerializeField] private CreeperPoisonAura _creeperPoisonAura;
    [SerializeField] private ColdBlood _coldBlood;

    [SerializeField] private float durationErodedArmor = 6f;

    #region PoisonCloud

    [SerializeField] private PoisonDamagingCloudPrefab _poisonDamagingCloudPrefab;
    [SerializeField] private PoisonHealingCloudPrefab _poisonHealingCloudPrefab;
    private PoisonDamagingCloudPrefab _poisonDamagingCloud;
    private PoisonHealingCloudPrefab _poisonHealingCloud;
    private float _durationPoisonCloud = 6f;

    #endregion

    private Vector3 _mousePos = Vector3.positiveInfinity;

    private int _poisonBoneStack = 0;

    private float _originalCooldown;
    private float _radiusTargetCheck = 0.5f;
    private float _increaseManaCostValue = 1.3f;
    private float _baseIncreaseManaCostValue = 1f;

    private bool _isActiveRestorationOfGlands;
    private bool _isHealingPoisonCloud = false;
    private bool _isPlayerInvisible = false;

    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;
    private bool _isAbilityActive = false;

    #region Talent


    private bool _isActiveHealingSpitPoison = false;
    private bool _canSpawnPoisonCloud = false;
    private bool _isErodedArmorState = false;
    private bool _isTransparentPoisons = false;
    private bool _isColdBloodCrit = false;

    public bool IsTransparentPoisons
    {
        get => _isTransparentPoisons;
        set
        {
            if (_isTransparentPoisons != value)
            {
                _isTransparentPoisons = value;

                Debug.Log("талант прозрачности активен");
                if (_isTransparentPoisons) Buff.ManaCost.IncreasePercentage(_increaseManaCostValue);
                else Buff.ManaCost.IncreasePercentage(_baseIncreaseManaCostValue);
            }
        }
    }

    public void ColdBloodStrike(bool value) => _isColdBloodCrit = value;
    public void ErodedArmorState(bool value) => _isErodedArmorState = value;
    public void ActiveHealingSpitPoison(bool value) => _isActiveHealingSpitPoison = value;
    public void TransparentPoisons(bool value) => IsTransparentPoisons = value;

    public void SetPoisonCloudEnabled(bool value)
    {
        _canSpawnPoisonCloud = value;
    }

    #endregion

    public bool IsAltAbility { get; set; }
    public int PoisonBoneStack { get => _poisonBoneStack; set => _poisonBoneStack = value; }

    public event Action ResetAbilityParameters;
    public event Action AbilityChange;

    private static readonly int spitPoisonTrigger = Animator.StringToHash("SpitPoisonCastAnimTrigger");

    protected override int AnimTriggerCast => spitPoisonTrigger;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => CheckCanCast();
    private bool IsAllyTarget(IDamageable target) => target.gameObject.layer == LayerMask.NameToLayer("Allies");

    public void AnimSpitPoisonCast()
    {
        AnimStartCastCoroutine();
    }

    public void AnimSpitPoisonCastEnd()
    {
        AnimCastEnded();
    }

    protected void Start()
    {
        _originalCooldown = Cooldown.CooldownTime;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.GetTargets().Count > 0) Targeting.SetTarget(targetInfo.GetTargets()[0]);
        _mousePos = targetInfo.Points[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _isAbilityActive = true;
        Vector3 targetPoint = Vector3.positiveInfinity;

        CooldownChange();

        ////CheckActiveTalents(); //TODO: Вернуть

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                //_currentTarget = GetRaycastTarget(true);

                Targeting.FindTempTarget(Targeting.GetMousePoint(), _radiusTargetCheck);
                targetPoint = Targeting.GetMousePoint();

                if (Targeting.GetTempTarget()?.Targetable is IDamageable damageable)
                {
                    ChooseTarget(damageable);
                }
                else
                {
                    _isOriginalTargetPlayer = false;
                    _isOriginalTargetAllies = false;
                    _isOriginalTargetEnemy = false;
                }
            }
            yield return null;
        }

        Targeting.SetTarget(Targeting.GetTempTarget()?.Targetable);

        TargetInfo targetInfo = new();
        targetInfo.AddTarget(Targeting.GetTarget()?.Targetable);
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        Vector3 spawnPosition = _spawnPoint != null
            ? _spawnPoint.transform.position
            : _player.transform.position;

        spawnPosition += Vector3.up;

        Shoot(Targeting.GetTarget()?.Damageable, spawnPosition);

        ResetAbilityParameters?.Invoke();

        yield return null;
    }

    protected override void ClearData()
    {
        _isAbilityActive = false;

        _isHealingPoisonCloud = false;

        _isOriginalTargetAllies = false;
        _isOriginalTargetEnemy = false;
        _isOriginalTargetPlayer = false;

        Targeting.ClearTarget();

        _mousePos = Vector3.positiveInfinity;
    }

    private bool CheckCanCast()
    {
        if (Targeting.GetTarget() == null) return Vector3.Distance(_mousePos, transform.position) <= AreaInfo.CastLength && Targeting.NoObstacles(_mousePos, _obstacle);

        return Vector3.Distance(_mousePos, transform.position) <= AreaInfo.CastLength && Targeting.NoObstacles(_mousePos, _obstacle) ||
               Vector3.Distance(Targeting.GetTarget().Transform.position, transform.position) <= AreaInfo.CastLength && Targeting.NoObstacles(Targeting.GetTarget().Transform.position, _obstacle);
    }

    private void CooldownChange()
    {
        if (_isActiveHealingSpitPoison)
        {
            if (_isOriginalTargetAllies || _isOriginalTargetPlayer)
            {
                if (Cooldown.CooldownTime == _originalCooldown)
                {
                    Cooldown.CooldownTime /= 3;
                }
            }
            else
            {
                Cooldown.CooldownTime = _originalCooldown;
            }
        }
        else
        {
            Cooldown.CooldownTime = _originalCooldown;
        }
    }

    //private void CheckActiveTalents()
    //{
    //    //_isActiveEatingAcid = _eatingAcid.Data.IsOpen;
    //    _isActiveHealingSpitPoison = _healingSpitPoison.Data.IsOpen;
    //    _isActiveRestorationOfGlands = _restorationOfGlands.Data.IsOpen;
    //}

    private void ChooseTarget(IDamageable damageable)
    {
        if (damageable == null)
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;
            return;
        }

        GameObject obj = damageable.gameObject;

        if (obj == _player.gameObject)
        {
            _isOriginalTargetPlayer = true;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;
        }
        else if (obj.layer == LayerMask.NameToLayer("Allies"))
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = true;
            _isOriginalTargetEnemy = false;
        }
        else if (obj.layer == LayerMask.NameToLayer("Enemy"))
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = true;
        }
        else
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;
        }
    }

    private void Shoot(IDamageable damageable, Vector3 spawnPosition)
    {
        if (damageable != null)
        {
            CmdInstantiateProjectileToTarget(
                damageable.gameObject,
                spawnPosition,
                _player.Resource.CurrentValue,
                _isActiveHealingSpitPoison,
                _isActiveRestorationOfGlands,
                IsAltAbility,
                _isOriginalTargetPlayer,
                _isOriginalTargetEnemy,
                _isOriginalTargetAllies,
                _isTransparentPoisons
            );
        }
        else
        {
            CmdInstantiateProjectileToPoint(
                _mousePos,
                spawnPosition,
                _player.Resource.CurrentValue,
                _isActiveHealingSpitPoison,
                _isActiveRestorationOfGlands,
                IsAltAbility,
                _isOriginalTargetPlayer,
                _isOriginalTargetEnemy,
                _isOriginalTargetAllies,
                _isTransparentPoisons
            );
        }

        _player.Move.SetCanMove(true);

        if (_isErodedArmorState && (_isOriginalTargetAllies || _isOriginalTargetPlayer))
            Cooldown.Modify(-3f);

        if (_canSpawnPoisonCloud)
            CmdApplyPoisonCloud(_isHealingPoisonCloud, _durationPoisonCloud);

        if (_isErodedArmorState)
            _player.CharacterState.CmdAddState(States.ErodedArmor, durationErodedArmor, 0, _player.gameObject, Name);
    }

    #region Command Methods
    [Command]
    private void CmdInstantiateProjectileToTarget(
    GameObject target,
    Vector3 spawnPosition,
    float manaValue,
    bool isActiveHealingSpitPoison,
    bool isActiveRestorationOfGlands,
    bool isPlayerInvisible,
    bool isTargetPlayer,
    bool isTargetEnemy,
    bool isTargetAllies,
    bool isTransparentPoisons)
    {
        int ownerLayer = _player.gameObject.layer;

        GameObject item = Instantiate(_projectile.gameObject, spawnPosition, Quaternion.identity);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(
            _player,
            this,
            _player.Resource.CurrentValue,
            isActiveHealingSpitPoison,
            isActiveRestorationOfGlands,
            isPlayerInvisible,
            isTargetPlayer,
            isTargetEnemy,
            isTargetAllies,
            PoisonBoneStack,
            _creeperPoisonAura.IsFeelingPoisoning,
            isTransparentPoisons,
            ownerLayer,
            _isColdBloodCrit && _coldBlood.IsCanCrit
        );

        if (_isColdBloodCrit)
            _coldBlood.IsCanCrit = false;

        projectile.MoveBallToTarget(target.transform.position);

        NetworkServer.Spawn(item);
        projectile.RpcInitTransparent(isTransparentPoisons, ownerLayer);
    }

    [Command]
    private void CmdInstantiateProjectileToPoint(
    Vector3 point,
    Vector3 spawnPosition,
    float manaValue,
    bool isActiveHealingSpitPoison,
    bool isActiveRestorationOfGlands,
    bool isPlayerInvisible,
    bool isTargetPlayer,
    bool isTargetEnemy,
    bool isTargetAllies,
    bool isTransparentPoisons)
    {
        int ownerLayer = _player.gameObject.layer;

        GameObject item = Instantiate(_projectile.gameObject, spawnPosition, Quaternion.identity);

        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(
            _player,
            this,
            _player.Resource.CurrentValue,
            isActiveHealingSpitPoison,
            isActiveRestorationOfGlands,
            isPlayerInvisible,
            isTargetPlayer,
            isTargetEnemy,
            isTargetAllies,
            PoisonBoneStack,
            _creeperPoisonAura.IsFeelingPoisoning,
            isTransparentPoisons,
            ownerLayer,
            _isColdBloodCrit && _coldBlood.IsCanCrit
        );

        if (_isColdBloodCrit)
            _coldBlood.IsCanCrit = false;

        Vector3 direction = point - spawnPosition;
        direction.y = 0;
        direction = direction.normalized;

        point = spawnPosition + direction * AreaInfo.CastLength;
        point.y = spawnPosition.y;

        projectile.ScheduleAutoDestroy(point, _projectile.Speed);
        projectile.MoveBallOnMaxDistance(point);

        NetworkServer.Spawn(item);
        projectile.RpcInitTransparent(isTransparentPoisons, ownerLayer);
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

                //SceneManager.MoveGameObjectToScene(_poisonDamagingCloudPrefab.PoisonDamageCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonDamagingCloudPrefab.PoisonDamageCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
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

                //SceneManager.MoveGameObjectToScene(_poisonHealingCloudPrefab.PoisonHealingCloud.gameObject, _hero.NetworkSettings.MyRoom);

                _poisonHealingCloudPrefab.PoisonHealingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();

                NetworkServer.Spawn(_poisonHealingCloud.gameObject);

            }
            else
            {
                _player.CharacterState.AddState(States.HealingPoisonCloud, duration, 0, _player.gameObject, Name);
                _poisonHealingCloudPrefab.PoisonHealingCloud.AddStack();
            }
        }

        if (_creeperPoisonAura.IsFeelingPoisoning) _player.CharacterState.AddState(States.FeelingPoisoning, 2f, 0, _player.gameObject, name);

        RpcApply(_poisonDamagingCloudPrefab.PoisonDamageCloud, _poisonHealingCloudPrefab.PoisonHealingCloud, duration, isHealingCloud);
    }


    #endregion

    #region ClientRpc Methods

    [ClientRpc]
    private void RpcApply(PoisonDamagingCloudPrefab poisonDamagingCloud, PoisonHealingCloudPrefab poisonHealingCloud, float duration, bool isHealingCloud)
    {
        if (poisonDamagingCloud != null)
        {
            poisonDamagingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
            poisonDamagingCloud.AddStack();
        }

        if (poisonHealingCloud != null && isHealingCloud)
        {
            poisonHealingCloud.InitializationProjectile(_player, duration, this, _creeperPoisonAura.IsFeelingPoisoning);
            poisonHealingCloud.AddStack();
        }
    }

    #endregion
}
