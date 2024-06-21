using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBall : Ability
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private LayerMask _enemyLayerMask;
   // [SerializeField] private int maxCharges = 2;
   // [SerializeField] private float cooldownCharge = 14f;
    [SerializeField] private VisualRender abilityRender;
    private float angle;
    private float timeFastMovementCast = 1.8f;
    private float timeSlowMovementCast = 0.4f;

    private Vector2 targetOrPointPosition;
    private Vector2 _target;
    private Vector2 firstMousePosition;
    private Vector2 secondMousePosition;

    private bool _firstClickDone = false;
    private bool _secondClickDone = false;
    private bool _isEnemy = false;
    private bool _enabled = false;
    private bool isFast;

    //private float[] _cooldownTimers;
    //private new void Start()
    //{
    //    _maxCharges = maxCharges;
    //    _currentChargers = _maxCharges;
    //    _chargeCooldown = cooldownCharge;
    //    _cooldownTimers = new float[maxCharges];
    //}
    private void Update()
    {
        //StartCooldownForCharge();
        if (!_enabled) return;

        Debug.Log("_currentChargers > 0 / " + _currentChargers);
        if (Input.GetMouseButtonDown(0))
        {
            if (!_firstClickDone)
            {
                FirstClickMouse();
                Debug.Log("FirstClickDone currentCharge = " + _currentChargers);
            }
            else if (!_secondClickDone)
            {
                SecondClickMouse();
                if (_firstClickDone && _secondClickDone)
                {
                    PayCost();
                    IsFast();
                }
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            Cancel();
        }
    }

    protected override void Cast()
    {
        Debug.Log("Cast");
        _enabled = true;
    }

    protected override void Cancel()
    {
        _enabled = false;
        ResetBools();
    }
    private void ResetBools()
    {
        _firstClickDone = false;
        _secondClickDone = false;
        _isEnemy = false;
    }

    private void FirstClickMouse()
    {
        firstMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hitEnemy = Physics2D.Raycast(_playerLinks.transform.position, (firstMousePosition - (Vector2)_playerLinks.transform.position).normalized, 12.0f, _enemyLayerMask);
        if (hitEnemy.collider != null)
        {
            _target = hitEnemy.collider.transform.position;
            _isEnemy = true;
        }
        _firstClickDone = true;
    }

    private void SecondClickMouse()
    {
        secondMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _secondClickDone = true;
    }

    private void IsFast()
    {
        if (_isEnemy)
        {
            isFast = Vector2.Distance(_playerLinks.transform.position, secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, _target);
        }
        else
        {
            isFast = Vector2.Distance(_playerLinks.transform.position, secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, firstMousePosition);
        }
        Debug.Log("Cast method isFast = " + isFast);
        StartCoroutine(isFast ? FastMoveShoot() : SlowMoveShoot());
    }

    private IEnumerator FastMoveShoot()
    {
        Debug.Log("FastMoveShoot()");
        yield return new WaitForSeconds(timeFastMovementCast);
        ShootProjectile(true);
    }

    private IEnumerator SlowMoveShoot()
    {
        Debug.Log("SlowMoveShoot()");
        yield return new WaitForSeconds(timeSlowMovementCast);
        ShootProjectile(false);
    }

    private void ShootProjectile(bool _isFast)
    {
        targetOrPointPosition = _isEnemy ? _target : firstMousePosition;
        Debug.Log("ShootProjectile targetOrPointPosition = " + targetOrPointPosition);
        PoisonBallProjectile projectile = Instantiate(_projectile, PlayerMove.transform.position, Quaternion.Euler(0.0f, 0.0f, angle));
        projectile.dad = _playerLinks;
        projectile.energyDad = _playerLinks.Stamina.Value;
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
        projectile.MoveBall(targetOrPointPosition, _isFast);
        Cancel();
    }

    //private void StartCooldownForCharge()
    //{
    //    while (true)
    //    {
    //        for (int i = 0; i < maxCharges; i++)
    //        {
    //            if (_cooldownTimers[i] > 0)
    //            {
    //                _cooldownTimers[i] -= Time.deltaTime;
    //                if (_cooldownTimers[i] <= 0)
    //                {
    //                    Debug.Log("cooldown = " + _chargeCooldown);
    //                    _cooldownTimers[i] = 0;
    //                    _currentChargers++;
    //                    if (_currentChargers > maxCharges)
    //                    {
    //                        _currentChargers = maxCharges;
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}
}

