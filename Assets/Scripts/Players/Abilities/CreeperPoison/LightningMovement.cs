using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightningMovement : Skill
{
    [Header("Dependencies")]
    [SerializeField] private Character _player;

    [Header("Talents & Abilities")]
    [SerializeField] private SuperFastScales _superFastScales;
    [SerializeField] private HeatedGlands _heatedGlands;
    [SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private PoisonSlap _poisonSlap;
    [SerializeField] private LightningStrikes _lightningStrikes;

    [Header("Settings")]
    [SerializeField] private float _rangeLeap;
    [SerializeField] private float _durationLeap;
    [SerializeField] private float _radiusAttack;

    private Vector3 _leapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint;
    private bool _hasSecondLeap;

    private HashSet<Character> _damagedCharacters = new HashSet<Character>();

    public bool IsInMovement { get; private set; }
    public Character Target { get; private set; }
    public float DurationLeap => _durationLeap;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => !HasObstaclesBetween(_player.transform.position, _leapPoint);

    private bool HasObstaclesBetween(Vector3 start, Vector3 end)
    {
        var direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit hit;
        return Physics.SphereCast(start, 1, direction, out hit, distance, _obstacle);
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_leapPoint.x) && !Disactive)
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint))
                {
                    _leapPoint = CalculateLeapPoint(GetMousePoint());
                }
            }

            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        IsInMovement = true;
        _player.Move.CanMove = false;
        _damagedCharacters.Clear();

        if (_superFastScales.Data.IsOpen)
            _superFastScales.IncreasingResistance(Target);

        if (_heatedGlands.Data.IsOpen)
            _player.CharacterState.AddState(States.HeatedGlands, 4f, 0, _player.gameObject, null);

        _player.CharacterState.CmdAddState(States.Immateriality, _durationLeap, 0, _player.gameObject, Name);

        Vector3 direction = (_leapPoint - _player.transform.position).normalized;
        if (direction.sqrMagnitude > 0.001f) _player.transform.rotation = Quaternion.LookRotation(direction);

        _lightningStrikes.IsUsedLightningStrikes = true;
        _poisonSlap.IsCanDamageDeal = true;

        StartCoroutine(DamageCheckRoutine());

        bool secondLeapRequested = false;

        _player.Move.SetAnimationMovement((_leapPoint - _player.transform.position).normalized * _player.Move.CurrentSpeed);

        _player.Rigidbody.DOMove(_leapPoint, _durationLeap)
            .SetEase(Ease.OutSine)
            .OnUpdate(() =>
            {
                Vector3 velocity = (_leapPoint - _player.transform.position).normalized * _player.Move.CurrentSpeed;
                _player.Move.SetAnimationMovement(velocity);
            })
            .OnComplete(() =>
            {
                Debug.Log($"Первый прыжок выполнен успешно");
                _lightningStrikes.IsUsedLightningStrikes = false;
                _poisonSlap.IsCanDamageDeal = false;
                _player.Move.StopMoveAnimation();
        });


        float elapsed = 0;

        while (elapsed < _durationLeap)
        {
            elapsed += Time.deltaTime;
            if (Input.GetMouseButtonDown(0) && !_hasSecondLeap)
            {
                _secondLeapPoint = CalculateLeapPoint(GetMousePoint());
                 
                if (!HasObstaclesBetween(_player.transform.position, _secondLeapPoint)) secondLeapRequested = true;
                else _secondLeapPoint = Vector3.positiveInfinity;
            }
            yield return null;
        }

        if (secondLeapRequested && _damagedCharacters.Count > 0) ExecuteLeapSecond(_secondLeapPoint);
        else ClearData();
    }

    private void ExecuteLeapSecond(Vector3 pointSecond)
    {
        if (!float.IsPositiveInfinity(pointSecond.x))
        {
            _player.Move.SetAnimationMovement((pointSecond - _player.transform.position).normalized * _player.Move.CurrentSpeed);

            _player.Rigidbody.DOMove(pointSecond, _durationLeap)
              .SetEase(Ease.OutSine)
              .OnUpdate(() =>
              {
                  Vector3 velocity = (pointSecond - _player.transform.position).normalized * _player.Move.CurrentSpeed;
                  _player.Move.SetAnimationMovement(velocity);
              })
              .OnComplete(() =>
              {
                  Debug.Log($"Второй прыжок выполнен успешно");
                  _player.Move.StopMoveAnimation(); 
              ClearData();
              });
        }
    }

    private IEnumerator DamageCheckRoutine()
    {
        while (IsInMovement)
        {
            Collider[] hits = Physics.OverlapSphere(_player.transform.position, _radiusAttack, _targetsLayers);

            foreach (Collider hit in hits)
            {
                var character = hit.GetComponent<Character>();

                if (character && !_damagedCharacters.Contains(character))
                {
                    if (_player.Abilities.SelectedSkills.Contains(_lightningStrikes) && _lightningStrikes.IsPreparing)
                    {
                        _lightningStrikes.SetTarget(character);
                        _lightningStrikes.TryCast();
                        _damagedCharacters.Add(character);
                    }

                    if (_player.Abilities.SelectedSkills.Contains(_poisonSlap) && _poisonSlap.IsPreparing)
                    {
                        _poisonSlap.SetTarget(character);
                        _poisonSlap.TryCast();
                        _damagedCharacters.Add(character);
                    }

                    if (_lightningFastPoisonSlap.Data.IsOpen && _poisonSlap.IsCanDamageDeal && _poisonSlap.RemainingCooldownTime <= 0.2f)
                        _poisonSlap.UsePoisonSlapOfLightningMovement();
                    else
                        _creeperStrike.DamageDeal(character, _lightningStrikes.IsUsedLightningStrikes);

                    _damagedCharacters.Add(character);
                }
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    private Vector3 CalculateLeapPoint(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        Vector3 leapPoint = transform.position + direction * Mathf.Min(_rangeLeap, Vector3.Distance(transform.position, targetPoint));
        leapPoint.y = 1f;
        return leapPoint;
    }

    protected override void ClearData()
    {
        IsInMovement = false;
        _player.Move.CanMove = true;
        Target = null;
        _hasSecondLeap = false;
        _secondLeapPoint = Vector3.positiveInfinity;
        _leapPoint = Vector3.positiveInfinity;
    }
}