using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonSlap : TargetOrAreaAbility
{
    [SerializeField] private Character _dad;
    [SerializeField] private PoisonBall _poisonBall;
    [SerializeField] private PoisonBallProjectile _poisonBallProjectile;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private LightningStrikes _lightningStrikes;

    private const float _increasingExecutionSpeedFromCreeperStrike = 0.5f;
    private const float _increasingExecutionSpeedFromLightningStrikes = 1.0f;
    private float _timeCast = 1.6f;

    private float _baseDamage = 30f;
    private float _distancePush = 3.0f;
    private float _durationPushInSeconds = 1.0f;

    public bool IsActive = false;

    private Coroutine _useCoroutine;
    private Transform _startPosition;
    private GameObject _currentTarget => Target.gameObject;


    protected override void Start()
    {
        base.Start();
        InitializationComponents();
        _startPosition.transform.position = new Vector3(_dad.transform.position.x, _dad.transform.position.y + 1.5f);
    }
    protected override void CastAction()
    {
       // _useCoroutine = StartCoroutine(UseAbilityCoroutine());
    }

    protected override void Cancel()
    {
        throw new System.NotImplementedException();
    }

    //private IEnumerator UseAbilityCoroutine()
    //{

    //}

    private IEnumerator InstantiatePoisonBall()
    {
        _castDeley = _timeCast;
        yield return GetCastDeleyCoroutine();

        GameObject item = Instantiate(_poisonBallProjectile.gameObject, _startPosition);
        PoisonBallProjectile poisonBall = item.GetComponent<PoisonBallProjectile>();




    }

    private void Attack()
    {

    }
    
    private void IncreaseExecutionSpeedFromCreeperStrike()
    {

    }

    private void IncreaseExecutionSpeedFromLightningStrikes()
    {

    }

    private void InitializationComponents()
    {
        _dad = GetComponent<Character>();
        _poisonBall = _dad.GetComponentInChildren<PoisonBall>();
        _creeperStrike = _dad.GetComponentInChildren<CreeperStrike>();
        _lightningStrikes = _dad.GetComponentInChildren<LightningStrikes>();
    }

    [Command]
    private void CmdInstantiatePoisonBall()
    {

    }
}
