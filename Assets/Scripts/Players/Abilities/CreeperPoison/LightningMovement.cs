using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightningMovement : Skill
{
    [Header("Talents & Abilities")]
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private PoisonSlap _poisonSlap;
    [SerializeField] private LightningStrikes _lightningStrikes;

    [Header("Movement Settings")]
    [SerializeField] private float _durationLeap;
    [SerializeField] private float _radiusAttack;
    [SerializeField] private float _returnCooldownDivider = 0.5f;
    [SerializeField] private float _castSpeedValue = 4f;

    [Header("Visuals")]
    [SerializeField] private AbilityLineRenderer _lineRendererPrefab;

    private AttributeModifier _cooldownModifier = new AttributeModifier(0, ModifierType.Multiplier);
    private AttributeModifier _castModifier = new AttributeModifier(0, ModifierType.Multiplier);

    private Vector3 _leapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint = Vector3.positiveInfinity;
    private bool _hasSecondLeapInput;
    private bool _enemyHitDuringLeap;

    private Vector3 _startPosition;
    private Coroutine _movementRoutine;
    private Coroutine _secondVectorDrawRoutine;

    private BoxArea _secondVectorLineInstance;

    private readonly HashSet<Character> _damagedCharacters = new HashSet<Character>();

    #region Talent
    private bool _isLightningEvade;
    public void LightningEvade(bool value) => _isLightningEvade = value;
    #endregion

    public bool IsInMovement { get; private set; }
    public Character Target { get; private set; }
    public float DurationLeap => _durationLeap;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerCastDelay => 0;
    protected override bool IsCanCast => !HasObstaclesBetween(_hero.transform.position, _leapPoint);

    private void OnEnable()
    {
        OnSkillCanceled += HandleSkillCanceled;
    }

    private void OnDisable()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    private void HandleSkillCanceled()
    {
        if (_movementRoutine != null)
        {
            StopCoroutine(_movementRoutine);
            _movementRoutine = null;
        }

        StopSecondVectorDraw();

        if (_hero?.Move != null)
        {
            Hero.Move.SetCanMove(true);
            _hero.Move.StopMoveAndAnimationMove();
        }

        FinalizeMovement();
        ClearData();
    }

    private bool HasObstaclesBetween(Vector3 start, Vector3 end)
    {
        var direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit hit;
        return Physics.SphereCast(start, 0.2f, direction, out hit, distance, _obstacle);
    }

    private bool HasEnemiesOnPath(Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        RaycastHit[] hits = Physics.SphereCastAll(start, _radiusAttack, direction, distance, Targeting.Layer);
        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<Character>(out var character) && character != _hero)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsValidLeapPoint(Vector3 point)
    {
        return !float.IsNaN(point.y) && !float.IsInfinity(point.y) && point.y > 0.01f;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
        {
            _leapPoint = targetInfo.Points[0];
        }

        if (targetInfo.Points.Count > 1)
        {
            _secondLeapPoint = targetInfo.Points[1];
            _hasSecondLeapInput = true;
        }
        else
        {
            _secondLeapPoint = Vector3.positiveInfinity;
            _hasSecondLeapInput = false;
        }
    }

    private void FinalizeMovement()
    {
        _lightningStrikes.IsUsedLightningStrikes = false;
        _poisonSlap.IsCanDamageDeal = false;
        if (DOTween.IsTweening(_hero.Rigidbody)) DOTween.Kill(_hero.Rigidbody);
        Hero.Move.SetCanMove(true);
        _hero.Move.StopMoveAndAnimationMove();

        IsInMovement = false;
        _movementRoutine = null;

        ClearData();
    }

    protected override void ClearData()
    {
        Target = null;
        _hasSecondLeapInput = false;
        _enemyHitDuringLeap = false;
        _secondLeapPoint = Vector3.positiveInfinity;
        _leapPoint = Vector3.positiveInfinity;
        _damagedCharacters.Clear();
        _castModifier.Value = 1;
        
        StopSecondVectorDraw();
        base.ClearData();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 firstPoint = Vector3.positiveInfinity;
        Vector3 secondPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(firstPoint.x) && !Disactive)
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = Targeting.GetMousePoint();
                if (Targeting.IsPointInRadius(AreaInfo.Radius, clickedPoint))
                {
                    firstPoint = CalculateLeapPoint(_hero.transform.position, clickedPoint);
                }
            }

            yield return null;
        }

        if (Disactive) yield break;

        while (GetMouseButton && !Disactive)
        {
            yield return null;
        }

        if (Disactive) yield break;

        bool enemiesAhead = HasEnemiesOnPath(_hero.transform.position, firstPoint);
        if (enemiesAhead)
        {
            SkillRender.StopDrawLine();
            StartSecondVectorDraw(firstPoint);

            float debounceTimer = 0.15f;
            while (debounceTimer > 0f && !Disactive)
            {
                debounceTimer -= Time.deltaTime;
                yield return null;
            }

            while (float.IsPositiveInfinity(secondPoint.x) && !Disactive)
            {
                if (GetMouseButton)
                {
                    Vector3 clickedPoint = Targeting.GetMousePoint();

                    if (IsValidLeapPoint(clickedPoint) &&
                        Targeting.IsPointInRadius(AreaInfo.Radius, clickedPoint))
                    {
                        secondPoint = CalculateLeapPoint(firstPoint, clickedPoint);
                    }
                }

                yield return null;
            }
            StopSecondVectorDraw();
        }

        if (Disactive) yield break;

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(firstPoint);

        if (IsValidLeapPoint(secondPoint) && !float.IsPositiveInfinity(secondPoint.x))
        {
            targetInfo.Points.Add(secondPoint);
        }

        callbackDataSaved(targetInfo);
    }

    #region Custom Line Renderer For Second Vector

    private void StartSecondVectorDraw(Vector3 originPoint)
    {
        StopSecondVectorDraw();
        _secondVectorDrawRoutine = StartCoroutine(SecondVectorDrawJob(originPoint));
    }

    private void StopSecondVectorDraw()
    {
        if (_secondVectorDrawRoutine != null)
        {
            StopCoroutine(_secondVectorDrawRoutine);
            _secondVectorDrawRoutine = null;
        }

        if (_secondVectorLineInstance != null)
        {
            Destroy(_secondVectorLineInstance.gameObject);
            _secondVectorLineInstance = null;
        }
    }

    private IEnumerator SecondVectorDrawJob(Vector3 originPoint)
    {
        if (_lineRendererPrefab == null || _lineRendererPrefab.Start == null)
        {
            yield break;
        }

        Damage damage = new Damage { Value = Damage, Type = Info.DamageType };

        _secondVectorLineInstance = Instantiate(_lineRendererPrefab.Start);
        _secondVectorLineInstance.transform.position = originPoint;
        _secondVectorLineInstance.SetColor(Color.yellow);

        while (true)
        {
            Vector3 mousePoint = Targeting.GetMousePoint();
            Vector3 finalSecondPoint = CalculateLeapPoint(originPoint, mousePoint);
            Vector3 dir = finalSecondPoint - originPoint;
            
            float dynamicLength = dir.magnitude;

            if (dynamicLength > 0.01f)
            {
                _secondVectorLineInstance.SetSize(AreaInfo.CastWidth, dynamicLength, damage);

                float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
                _secondVectorLineInstance.transform.rotation = Quaternion.Euler(90, -angle + 90, 0);
            }

            yield return null;
        }
    }

    #endregion


    protected override IEnumerator CastJob()
    {
        if (_movementRoutine != null) yield break;
        _cooldownModifier.Value = 1;
        _movementRoutine = StartCoroutine(MovementRoutine());
        yield return _movementRoutine;
    }

    private IEnumerator ExecuteLeapSecond(Vector3 pointSecond)
    {
        if (!IsValidLeapPoint(pointSecond) || float.IsPositiveInfinity(pointSecond.x))
            yield break;

        _hero.Move.SetAnimationMovement((pointSecond - _hero.transform.position).normalized * _hero.Move.CurrentSpeed);

        Tween returnTween = _hero.Rigidbody.DOMove(pointSecond, _durationLeap)
          .SetEase(Ease.OutSine)
          .OnUpdate(() =>
          {
              Vector3 velocity = (pointSecond - _hero.transform.position).normalized * _hero.Move.CurrentSpeed;
              _hero.Move.SetAnimationMovement(velocity);
          });

        yield return returnTween.WaitForCompletion();

        _hero.Move.StopMoveAndAnimationMove();
    }

    private void ApplyReturnCooldownReduction()
    {
        if (Cooldown == null) return;

        _cooldownModifier.Value = _returnCooldownDivider;
        _cooldownModifier.Source = this;

        if (!Attributes[SkillAttributeName.Cooldown].Modifiers.Contains(_cooldownModifier))
            Attributes[SkillAttributeName.Cooldown].AddModifier(_cooldownModifier);
    }

    private IEnumerator MovementRoutine()
    {
        IsInMovement = true;
        Hero.Move.SetCanMove(false);

        _startPosition = _hero.transform.position;
        _startPosition.y = 1f;

        if (_isLightningEvade) _hero.CharacterState.CmdAddState(States.LightningEvade, 3f, 0, _hero.gameObject, Name);
        _damagedCharacters.Clear();
        _enemyHitDuringLeap = false;

        _leapPoint = CalculateLeapPoint(_hero.transform.position, _leapPoint);

        if (!IsValidLeapPoint(_leapPoint))
        {
            FinalizeMovement();
            yield break;
        }

        Vector3 direction = (_leapPoint - _hero.transform.position).normalized;

        if (direction.sqrMagnitude > 0.001f)
            _hero.transform.rotation = Quaternion.LookRotation(direction);

        _lightningStrikes.IsUsedLightningStrikes = true;
        _poisonSlap.IsCanDamageDeal = true;

        StartCoroutine(DamageCheckRoutine());

        _hero.Move.SetAnimationMovement(direction * _hero.Move.CurrentSpeed);

        if (Vector3.Distance(_hero.transform.position, _leapPoint) >= 0.1f)
        {
            Tween moveTween = _hero.Rigidbody.DOMove(_leapPoint, _durationLeap)
                .SetEase(Ease.InSine)
                .OnUpdate(() =>
                {
                    Vector3 velocity = (_leapPoint - _hero.transform.position).normalized * _hero.Move.CurrentSpeed;
                    _hero.Move.SetAnimationMovement(velocity);
                });

            yield return moveTween.WaitForCompletion();
        }

        _hero.Move.StopMoveAndAnimationMove();

        if (_hasSecondLeapInput && _enemyHitDuringLeap && IsValidLeapPoint(_secondLeapPoint))
        {
            if (_isLightningEvade) _hero.CharacterState.CmdAddState(States.LightningEvade, 3f, 0, _hero.gameObject, Name);

            _damagedCharacters.Clear();

            yield return ExecuteLeapSecond(_secondLeapPoint);

            ApplyReturnCooldownReduction();
        }

        FinalizeMovement();
        _movementRoutine = null;
    }

    private void RegisterHit(Character character)
    {
        _damagedCharacters.Add(character);
        _enemyHitDuringLeap = true;
    }

    private IEnumerator DamageCheckRoutine()
    {
        _castModifier.Value = _castSpeedValue;
        _castModifier.Source = this;

        if (!_poisonSlap.Attributes[SkillAttributeName.CastSpeed].Modifiers.Contains(_castModifier))
        {
            _poisonSlap.Attributes[SkillAttributeName.CastSpeed].AddModifier(_castModifier);
            _creeperStrike.Attributes[SkillAttributeName.CastSpeed].AddModifier(_castModifier);
            _lightningStrikes.Attributes[SkillAttributeName.CastSpeed].AddModifier(_castModifier);
        }

        while (IsInMovement)
        {
            List<TargetData> targets = _creeperStrike.Targeting.FindTargets(_hero.transform.position, _radiusAttack);

            if (targets != null && targets.Count > 0)
            {
                bool isLightningPreparing = _hero.Abilities.SelectedSkills.Contains(_lightningStrikes) && _lightningStrikes.IsPreparing;
                bool isPoisonPreparing = _hero.Abilities.SelectedSkills.Contains(_poisonSlap) && _poisonSlap.IsPreparing;

                foreach (TargetData targetData in targets)
                {
                    var character = targetData.Character;

                    if (character && !_damagedCharacters.Contains(character))
                    {
                        TargetInfo hitInfo = new TargetInfo();
                        hitInfo.AddTarget((ITargetable)character);

                        if (isLightningPreparing)
                        {
                            _lightningStrikes.TryCancel(true);
                            _lightningStrikes.OnLightningStrikesEnd += HandleLightningStrikesEnd;
                            _lightningStrikes.TryCast(hitInfo);
                            RegisterHit(character);
                            continue;
                        }

                        if (isPoisonPreparing)
                        {
                            _poisonSlap.TryCancel(true);
                            _poisonSlap.OnPoisonSlapEnd += HandlePoisonSlapEnd;
                            _poisonSlap.TryCast(hitInfo);
                            RegisterHit(character);
                            continue;
                        }

                        _creeperStrike.OnCreeperStrikeEnd += HandleCreeperStrikeEnd;
                        _creeperStrike.MarkNextHitFromLightningMovement();
                        _creeperStrike.TryCast(hitInfo);
                        RegisterHit(character);
                    }
                }
            }

            yield return new WaitForSeconds(0.05f);
        }
    }

    private Vector3 CalculateLeapPoint(Vector3 origin, Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - origin).normalized;

        Vector3 leapPoint = origin + direction * Mathf.Min(AreaInfo.Radius, Vector3.Distance(origin, targetPoint));
        leapPoint.y = 1f;

        Vector3 heroPos = _hero.transform.position;
        heroPos.y = 1f;

        float distanceFromHero = Vector3.Distance(heroPos, leapPoint);
    
        if (distanceFromHero > AreaInfo.Radius)
        {
            Vector3 dirFromHero = (leapPoint - heroPos).normalized;
            leapPoint = heroPos + dirFromHero * AreaInfo.Radius;
            leapPoint.y = 1f;
        }

        return leapPoint;
    }

    private void HandleCreeperStrikeEnd()
    {
        _creeperStrike.ClearDataCreeperStrike();
        _creeperStrike.OnCreeperStrikeEnd -= HandleCreeperStrikeEnd;
    }

    private void HandlePoisonSlapEnd()
    {
        _poisonSlap.ClearDataPoisonSlap();
        _poisonSlap.OnPoisonSlapEnd -= HandlePoisonSlapEnd;
    }

    private void HandleLightningStrikesEnd()
    {
        _lightningStrikes.ClearDataLightningStrikes();
        _lightningStrikes.OnLightningStrikesEnd -= HandleLightningStrikesEnd;
    }
}