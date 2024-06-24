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

    public bool _firstClickDone = false;
    public bool _secondClickDone = false;
    public bool _isEnemy = false;
    public bool _enabled = false;
    public bool isFast;

    private void Update()
    {
        //StartCooldownForCharge();
        if (!_enabled) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (!_firstClickDone)
            {
                FirstClickMouse();
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

        //Debug.Log("Cast method isFast = " + isFast);

        StartCoroutine(isFast ? FastMoveShoot() : SlowMoveShoot());
    }

    private IEnumerator FastMoveShoot()
    {
        //Debug.Log("FastMoveShoot()");

        _castDeley = timeFastMovementCast;
        yield return GetCastDeleyCoroutine();
        ShootProjectile(true);
    }

    private IEnumerator SlowMoveShoot()
    {
        //Debug.Log("SlowMoveShoot()");

        _castDeley = timeSlowMovementCast;
        yield return GetCastDeleyCoroutine();
        ShootProjectile(false);
    }

    private void ShootProjectile(bool _isFast)
    {
        //Debug.Log("ShootProjectile targetOrPointPosition = " + targetOrPointPosition);

        targetOrPointPosition = _isEnemy ? _target : firstMousePosition;
        PoisonBallProjectile projectile = Instantiate(_projectile, PlayerMove.transform.position, Quaternion.Euler(0.0f, 0.0f, angle));
        projectile.dad = _playerLinks;
        projectile.energyDad = _playerLinks.Stamina.Value;
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
        projectile.MoveBall(targetOrPointPosition, _isFast);
        Cancel();
    }
}

