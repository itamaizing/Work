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
    //[SerializeField] private SuperFastScales _superFastScales;
    //[SerializeField] private HeatedGlands _heatedGlands;
    //[SerializeField] private LightningFastPoisonSlap _lightningFastPoisonSlap;
    [SerializeField] private CreeperStrike _creeperStrike;
    [SerializeField] private PoisonSlap _poisonSlap;
    [SerializeField] private LightningStrikes _lightningStrikes;

    [SerializeField] private float _durationLeap;
    [SerializeField] private float _radiusAttack;
    [SerializeField] private float _returnCooldownDivider = 0.5f;
    [SerializeField] private float _castSpeedValue = 4f;

    private AttributeModifier _cooldownModifier = new AttributeModifier(0,ModifierType.Multiplier);
    private AttributeModifier _castModifier = new AttributeModifier(0,ModifierType.Multiplier);
    

    private Vector3 _leapPoint = Vector3.positiveInfinity;
    private Vector3 _secondLeapPoint;
    private bool _hasSecondLeap;

    private Vector3 _startPosition;

    private Coroutine _movementRoutine;

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

    private bool IsValidLeapPoint(Vector3 point)
    {
        return !float.IsNaN(point.y) && !float.IsInfinity(point.y) && point.y > 0.01f;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _leapPoint = targetInfo.Points[0];
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
        _hasSecondLeap = false;
        _secondLeapPoint = Vector3.positiveInfinity;
        _leapPoint = Vector3.positiveInfinity;
        _damagedCharacters.Clear();
        _castModifier.Value = 1;
        base.ClearData();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x) && !Disactive)
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = Targeting.GetMousePoint();

                if (Targeting.IsPointInRadius(AreaInfo.Radius, clickedPoint))
                {
                    targetPoint = CalculateLeapPoint(Targeting.GetMousePoint());
                }
            }

            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

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
        
        if(!Attributes[SkillAttributeName.Cooldown].Modifiers.Contains(_cooldownModifier)) 
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

        _leapPoint = CalculateLeapPoint(_leapPoint);

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

        if (Vector3.Distance(_hero.transform.position, _leapPoint) < 0.1f)
        {
            FinalizeMovement();
            yield break;
        }

        Tween moveTween = _hero.Rigidbody.DOMove(_leapPoint, _durationLeap)
            .SetEase(Ease.InSine)
            .OnUpdate(() =>
            {
                Vector3 velocity = (_leapPoint - _hero.transform.position).normalized * _hero.Move.CurrentSpeed;
                _hero.Move.SetAnimationMovement(velocity);
            });

        yield return moveTween.WaitForCompletion();

        _hero.Move.StopMoveAndAnimationMove();

        if (!IsValidLeapPoint(_leapPoint))
        {
            FinalizeMovement();
            yield break;
        }

        if (_hasSecondLeap && _damagedCharacters.Count > 0 && IsValidLeapPoint(_secondLeapPoint))
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

        if (!_hasSecondLeap)
        {
            _hasSecondLeap = true;
            _secondLeapPoint = _startPosition;
        }
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

            if (targets != null)
            {
                foreach (TargetData targetData in targets)
                {
                    var character = targetData.Character;

                    if (character && !_damagedCharacters.Contains(character))
                    {
                        TargetInfo hitInfo = new TargetInfo();
                        hitInfo.AddTarget((ITargetable)character);

                        if (_hero.Abilities.SelectedSkills.Contains(_lightningStrikes) && _lightningStrikes.IsPreparing)
                        {
                            _lightningStrikes.TryCancel(true);
                            _lightningStrikes.OnLightningStrikesEnd += HandleLightningStrikesEnd;
                            _lightningStrikes.TryCast(hitInfo);
                            RegisterHit(character);
                            break;
                        }

                        if (_hero.Abilities.SelectedSkills.Contains(_poisonSlap) && _poisonSlap.IsPreparing)
                        {
                            _poisonSlap.TryCancel(true);
                            _poisonSlap.OnPoisonSlapEnd += HandlePoisonSlapEnd;
                            _poisonSlap.TryCast(hitInfo);
                            RegisterHit(character);
                            break;
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

    private Vector3 CalculateLeapPoint(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        Vector3 leapPoint = transform.position + direction * Mathf.Min(AreaInfo.Radius, Vector3.Distance(transform.position, targetPoint));
        leapPoint.y = 1f;
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