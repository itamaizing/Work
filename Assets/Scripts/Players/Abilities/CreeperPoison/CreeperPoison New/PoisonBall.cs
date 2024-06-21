using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoisonBall : Ability
{
    [SerializeField] private Character _playerLinks;
    [SerializeField] private PoisonBallProjectile _projectile;
    [SerializeField] private GameObject pointPref;
    private float angle;
    private float _currentSpeed;
    private float timeFastMovementCast = 1.8f;
    private float timeSlowMovementCast = 0.4f;

    public Vector2 targetOrPointPosition;
    public Vector2 firstMousePosition;
    public Vector2 secondMousePosition;

    private bool _enabled = false;
    public bool _firstClickDone = false;
    public bool _secondClickDone = false;
    private bool isFast;

    private void Update()
    {
        if (!_enabled) return;


        if (Input.GetMouseButtonDown(0))
        {
            if (!_firstClickDone)
            {
                FirstClickMouse();
                Debug.Log("firstCLickDone!");
            }
            else if (!_secondClickDone)
            {
                SecondClickMouse();
                Debug.Log("secondCLickDone!");
                if (_firstClickDone && _secondClickDone)
                {
                    Debug.Log("firstCLickDone and secondCLickDone!" + _firstClickDone + " " + _secondClickDone);
                    Debug.Log(firstMousePosition + " first mouse position");
                    Debug.Log(secondMousePosition + " secon mouse position");
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
    }

    private void FirstClickMouse()
    {
        firstMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _firstClickDone = true;

        var point = pointPref.transform.position;
        pointPref.transform.position = firstMousePosition;

        Instantiate(pointPref, pointPref.transform);
    }

    private void SecondClickMouse()
    {
        secondMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _secondClickDone = true;
    }
    private void IsFast()
    {
        isFast = Vector2.Distance(_playerLinks.transform.position, secondMousePosition) > Vector2.Distance(_playerLinks.transform.position, firstMousePosition);
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
        Debug.Log("ShootProjectile");
        targetOrPointPosition = firstMousePosition;
        PoisonBallProjectile projectile = Instantiate(_projectile, PlayerMove.transform.position, Quaternion.Euler(0.0f, 0.0f, angle));
        projectile.dad = _playerLinks;
        projectile.energyDad = _playerLinks.Stamina.Value;
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
        projectile.MoveBall(targetOrPointPosition, _isFast);
        Cancel();
    }
}

