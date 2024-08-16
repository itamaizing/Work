using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class SpitPoison : TargetOrAreaAbility
{
    [Header("Talents")]
    [SerializeField] private HealingSpitPoison _healingSpitPoison;

    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _playerCharacter;

    private float _angle;
    private float _originalCooldown;

    private Vector2 _mousePos;

    private Character _currentTarget;

    private Coroutine _useCoroutine;
    private Coroutine _shootCoroutine;
    private Coroutine _mouseDirectionCoroutine;

    private bool _isPlayer;
    private bool _isActiveTalent;

    private bool _isOriginalTargetEnemy;
    private bool _isOriginalTargetAllies;
    private bool _isOriginalTargetPlayer;

    public bool Enabled;

    [Header("Test ParticleSystem")]
    [SerializeField] private ParticleSystem _soulDrainPrefab;
    private ParticleSystem _soulDrain;

    protected override void Start()
    {
        base.Start();
        _originalCooldown = _cooldown;
    }

    protected override IEnumerator UseCoroutine()
    {
        if (_healingSpitPoison.IsActive)
        {
            _isCanTargetHimself = _healingSpitPoison.IsCanTargetHimself;
            _isActiveTalent = _healingSpitPoison.IsActive;
            //Debug.Log("CanTargetHimself == " + _isCanTargetHimself);
        }
        else
        {
            _isCanTargetHimself = false;
            _isActiveTalent = _healingSpitPoison.IsActive;
        }
        yield return _chooseTargetJob = StartCoroutine(ChooseTargetCoroutine(Radius));
        CastAction();
    }

    protected override void CastAction()
    {
        //Debug.Log("CastAction SpitPoison");
        _useCoroutine = StartCoroutine(UseAbilityCoroutine()); 
    }

    protected override void Cancel()
    {
        if (_useCoroutine != null)
            StopCoroutine(UseAbilityCoroutine());

        if (_shootCoroutine != null)
            StopCoroutine(CallShootCoroutine());

        if (_mouseDirectionCoroutine != null)
            StopCoroutine(MouseDirectionCoroutine());
    }

    private IEnumerator UseAbilityCoroutine()
    {
        yield return _mouseDirectionCoroutine = StartCoroutine(MouseDirectionCoroutine());
        _shootCoroutine = StartCoroutine(CallShootCoroutine());
    }

    private IEnumerator MouseDirectionCoroutine()
    {
        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDir = _mousePos - _playerCharacter.Rb.position;
        _angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        ChooseTarget();
        yield return null;
    }

    private IEnumerator CallShootCoroutine()
    {
        if (_isActiveTalent)
        {
            if (_isOriginalTargetAllies || _isOriginalTargetPlayer)
            {
                if (_cooldown == _originalCooldown)
                {
                    _cooldown /= 3;
                }
                Debug.Log("if Cooldown == " + _cooldown);
                PayCost();
            }
            else
            {
                _cooldown = _originalCooldown;
                Debug.Log("else Cooldown == " + _cooldown);
                PayCost();
            }
        }
        else
        {
            Debug.Log("Else Talent is Active == " + _isActiveTalent);
            PayCost();
        }
        Shoot();
        yield return null;
    }
    
    private void Shoot()
    {
        if (_currentTarget != null)
        {
            CmdInstantiateProjectile(_currentTarget.gameObject, _angle, _playerCharacter.Stamina.Value, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
        }
        else
        {
            CmdInstantiateProjectileToPoint(Point, _angle, _playerCharacter.Stamina.Value, _isActiveTalent, _isOriginalTargetPlayer, _isOriginalTargetEnemy, _isOriginalTargetAllies);
        }

        _playerCharacter.Stamina.Use(_playerCharacter.Stamina.Value);

        Cancel();
    }

    private void ChooseTarget()
    {
        _currentTarget = Target;
        if (_currentTarget != null)
        {
            if (_currentTarget.gameObject == _playerCharacter.gameObject)
            {
                // Debug.Log("Target == Player");
                _isOriginalTargetPlayer = true;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = false;
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Allies"))
            {
                //.Log("Target == Allies");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = true;
                _isOriginalTargetEnemy = false;
            }
            else if (_currentTarget.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                //Debug.Log("Target == Enemy");
                _isOriginalTargetPlayer = false;
                _isOriginalTargetAllies = false;
                _isOriginalTargetEnemy = true;
            }
        }
        else
        {
            _isOriginalTargetPlayer = false;
            _isOriginalTargetAllies = false;
            _isOriginalTargetEnemy = false;

            if (Point != Vector3.zero)
            {
                _currentTarget = null;
            }
        }
    }

    private void ApplyCloudPoison()
    {
        _playerCharacter.CharacterState.CmdAddState(States.PoisonCloud, 6f, 0);
    }

    #region Command Methods

    [Command]
    private void CmdInstantiateProjectile(GameObject target, float angle, float manaValue, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        //Debug.Log("CmdInstProj / isActiveTalent == " + isActiveTalent);

        GameObject item = Instantiate(_projectile.gameObject, _playerCharacter.Rb.position, Quaternion.Euler(0, 0, angle));
        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_playerCharacter, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
        NetworkServer.Spawn(item);

        RpcInstantiateProjectile(target, projectile, angle, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
        ApplyCloudPoison();
    }

    [Command]
    private void CmdInstantiateProjectileToPoint(Vector3 point, float angle, float manaValue, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        //Debug.Log("CmdInstProj / isActiveTalent == " + isActiveTalent);

        GameObject item = Instantiate(_projectile.gameObject, _playerCharacter.Rb.position, Quaternion.Euler(0, 0, angle));
        SpitPoisonProjectile projectile = item.GetComponent<SpitPoisonProjectile>();

        projectile.InitializationProjectile(_playerCharacter, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
        NetworkServer.Spawn(item);

        RpcInstantiateProjectileToPoint(point, projectile, angle, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
        ApplyCloudPoison();
    }
    #endregion

    #region ClientRpc Methods

    [ClientRpc]
    private void RpcInstantiateProjectile(GameObject target, SpitPoisonProjectile projectile, float angle, float manaValue, 
        bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    {
        projectile.InitializationProjectile(_playerCharacter, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    }

    [ClientRpc]
    private void RpcInstantiateProjectileToPoint(Vector3 point, SpitPoisonProjectile projectile, float angle, float manaValue, bool isActiveTalent, bool isTargetPlayer, bool isTargetEnemy, bool isTargetAllies)
    { 
        projectile.InitializationProjectile(_playerCharacter, manaValue, isActiveTalent, isTargetPlayer, isTargetEnemy, isTargetAllies);
    }
    #endregion

}
