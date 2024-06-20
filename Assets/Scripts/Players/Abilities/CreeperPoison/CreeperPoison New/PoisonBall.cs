using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonBall : Ability
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PoisonBallProjectile _projectile;

    private float angle;
    private float _currentSpeed;
    private float timeFastMovementCast = 1.8f;
    private float timeSlowMovementCast = 0.4f;

    public Vector2 targetOrPointPosition;
    public Vector2 firstMousePosition;
    public Vector2 secondMousePosition;

    private bool _enabled = true;
    public bool _firstClickDone = false;
    private bool isFast;

    private void Update()
    {
        if (!_enabled) return;

         
        if (Input.GetMouseButtonDown(0))
        {
            if (!_firstClickDone)
            {
                firstMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                _firstClickDone = true;
            }
            else
            {
                secondMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Cast();
            }
            ResetBools();
        }
        if (Input.GetMouseButtonDown(1))
        {
            Cancel();
        }
    }

    protected override void Cast()
    {
        _enabled = true;
        isFast = Vector2.Distance(PlayerMove.transform.position, secondMousePosition) > Vector2.Distance(PlayerMove.transform.position, firstMousePosition);
        Debug.Log("Cast method isFast = " + isFast);
        StartCoroutine(isFast ? FastMoveShoot() : SlowMoveShoot());
    }

    protected override void Cancel()
    {
        _enabled = false;
    }
    private void ResetBools()
    {
        _firstClickDone = false;
    }

    private IEnumerator FastMoveShoot()
    {
        yield return new WaitForSeconds(timeFastMovementCast);
        ShootProjectileFast(true);
    }

    private IEnumerator SlowMoveShoot()
    {
        yield return new WaitForSeconds(timeSlowMovementCast);
        ShootProjectileSlow(false);
    }

    private void ShootProjectileFast(bool _isFast)
    {
        targetOrPointPosition = firstMousePosition;
        Debug.Log("ShootFast");
        PoisonBallProjectile projectile = Instantiate(_projectile, PlayerMove.transform.position, Quaternion.identity);
        projectile.dad = _playerLinks;
        projectile.energyDad = _playerLinks.Stamina.Value;
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
        projectile.MoveBall(targetOrPointPosition, _isFast);
    }

    private void ShootProjectileSlow(bool _isFast)
    {
        targetOrPointPosition = firstMousePosition;
        Debug.Log("ShootSlow");
        PoisonBallProjectile projectile = Instantiate(_projectile, PlayerMove.transform.position, Quaternion.identity);
        projectile.dad = _playerLinks;
        projectile.energyDad = _playerLinks.Stamina.Value;
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
        projectile.MoveBall(targetOrPointPosition, _isFast);
    }
}

