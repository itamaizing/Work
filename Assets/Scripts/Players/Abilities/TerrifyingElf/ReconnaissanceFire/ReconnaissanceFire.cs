using Mirror;
using System.Collections;
using UnityEngine;
using HeathenEngineering.UnityPhysics;
using HeathenEngineering.UnityPhysics.API;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class ReconnaissanceFire : Skill
{
    [Header("Reconnaissance Fire Settings")]
    [SerializeField] private TrickShot _trickShot;
    [SerializeField] private ReconnaissanceFireAura _fireAura;
    [SerializeField] private GameObject _emitterObject;
    [SerializeField] private ObjectData _fireData;
    [SerializeField] private float _duration = 10;
    [SerializeField] private float _baseArea = 3f;

    [Header("TrickShot Settings")]
    [SerializeField] private float _speed;

    [Header("Raycast settings")]
    [SerializeField] private LayerMask _groundLayer;

    private ReconnaissanceFireAura _currentFireAura;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Vector3 _endPoint = Vector3.positiveInfinity;
    private float _baseDuration;
    private float _baseAnimSpeed;
    private float _baseCastDelay;
    private Coroutine _auraLifeCoroutine;
    private Coroutine _boostWindow;
    private bool _isSkillEnableBoostLogic;
    private WaitForSeconds _waitForElvenBoostDuration;

    #region Const
    private const float AnimSlowdownFactor = 1.8f;
    private const float MaxRaycastDistance = 200f;
    private const float TrickShotDistanceOffset = 1f;
    private const float ElvenBoostDuration = 2f;
    private const float FireAuraBoostedHealth = 65f;
    private const float FireAuraWorshipperBonusDuration = 6f;
    private const float AuraSpawnYOffset = 0.1f;
    private const float ElvenBoostWindowChance = 0.30f;
    private const float AnimationFireMoveMagnitude = 0.0001f;
    #endregion

    #region Talent

    private bool _fireDarkTalent;
    private bool _fireHealthTalent;
    private bool _partialBlindnessTalent;
    private bool _fireWorshipperTalent;
    private bool _isSkillEnableBoostLogicActiveTalent;

    #endregion

    public ReconnaissanceFireAura CurrentFireAura => _currentFireAura;
    public float BaseArea { get => _baseArea; set => _baseArea = value; }

    protected override bool IsCanCast => Vector3.Distance(_targetPoint, transform.position) <= Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ThrowCastDelay");

    protected override void SkillEnableBoostLogic()
    {
        CastDeley = 0;
        _isSkillEnableBoostLogic = true;
    }
    protected override void SkillDisableBoostLogic()
    {
        CastDeley = _baseCastDelay;
        _isSkillEnableBoostLogic = false;
    }

    private void Start()
    {
        _baseAnimSpeed = Hero.Animator.speed;
        _trickShot.speed = _speed;
        _baseDuration = _duration;
        _waitForElvenBoostDuration = new WaitForSeconds(ElvenBoostDuration);
    }

    private void OnEnable()
    {
        ArrowFireProjectile.OnProjectilePathEnd += HandleProjectilePathEnd;
        OnSkillCanceled += HandleSkillCanceled;

        _baseCastDelay = CastDeley;
    }

    private void OnDisable()
    {
        ArrowFireProjectile.OnProjectilePathEnd -= HandleProjectilePathEnd;
        OnSkillCanceled -= HandleSkillCanceled;
    }

    public void FireCastStart()
    {
        AnimStartCastCoroutine();
    }

    public void FireCastEnd()
    {
        AnimCastEnded();
    }

    public void AnimationFireMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.CanMove = false;

        Vector3 direction = _targetPoint - _hero.transform.position;
        bool badDirection = float.IsInfinity(_targetPoint.x) || direction.sqrMagnitude < AnimationFireMoveMagnitude;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(_targetPoint);
    }

    public void TryStartElvenBoostWindow()
    {
        if (!_isSkillEnableBoostLogicActiveTalent) return;
        if (UnityEngine.Random.value > ElvenBoostWindowChance) return;

        if (_boostWindow != null) StopCoroutine(_boostWindow);
        _boostWindow = StartCoroutine(ElvenBoostWindow());
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
        _endPoint = _targetPoint;
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Animator.speed = Hero.Animator.speed/ AnimSlowdownFactor;
        _endPoint = Vector3.positiveInfinity;

        if (_emitterObject) _emitterObject.SetActive(true);
        ReconnaissanceFireHealthTalentEnter();

        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                targetPoint = GetMousePoint();

                if (IsPointInRadius(Radius, targetPoint) && NoObstacles(targetPoint, transform.position, _obstacle))
                {
                    Hero.Move.LookAtPosition(targetPoint);
                }
            }

            UpdateTrickShotTrajectory();
            yield return null;
        }

        if (_emitterObject) _emitterObject.SetActive(false);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_targetPoint == Vector3.positiveInfinity && (_trickShot == null || _fireAura == null)) yield break;

        _trickShot.Shoot();

        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.StopLookAt();
        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.CanMove = true;
    }

    private IEnumerator ElvenBoostWindow()
    {
        EnableSkillBoost();
        yield return _waitForElvenBoostDuration;
        DisableSkillBoost();
    }

    private void HandleProjectilePathEnd(Vector3 position) => CmdSpawnFireAura(_endPoint);

    void UpdateTrickShotTrajectory()
    {
         if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, MaxRaycastDistance, _groundLayer))
        {
            float s = Vector3.Distance(_trickShot.transform.position, hit.point); _trickShot.distance = s + TrickShotDistanceOffset;

            if (Ballistics.Solution(_trickShot.transform.position, _trickShot.speed, hit.point, _trickShot.constantAcceleration, out Quaternion low, out _) > 0) _trickShot.transform.rotation = low;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null) ReconnaissanceFireHealthTalentExit();
        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
        _targetPoint = Vector3.positiveInfinity;
        AnimCastEnded();
        if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
        if (_boostWindow != null) StopCoroutine(_boostWindow);
    }

    [Command]
    private void CmdSetMaxHealth(float maxHealth)
    {
        _fireData.MaxHealth = maxHealth;
    }

    [Command]
    private void CmdSpawnFireAura(Vector3 position)
    {
        if (float.IsInfinity(position.x) || float.IsNaN(position.x)) return;

        if (!_isSkillEnableBoostLogic)
        {
            if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
            if (_currentFireAura != null) NetworkServer.Destroy(_currentFireAura.gameObject);
        }

        position.y += AuraSpawnYOffset;
        var aura = Instantiate(_fireAura, position, Quaternion.identity);
        aura.Init(Hero);
        SceneManager.MoveGameObjectToScene(aura.gameObject, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(aura.gameObject, connectionToClient);

        _currentFireAura = aura;
        _currentFireAura.FireDarkTalent = _fireDarkTalent;
        RpcSetCurrentFireAura(aura);

        float life = _baseDuration + (_fireWorshipperTalent ? FireAuraWorshipperBonusDuration : 0f);
        _auraLifeCoroutine = StartCoroutine(DestroyAuraAfter(life, aura));
    }

    [Server]
    private IEnumerator DestroyAuraAfter(float seconds, ReconnaissanceFireAura aura)
    {
        yield return new WaitForSeconds(seconds);
        if (aura != null) NetworkServer.Destroy(aura.gameObject);
    }

    [ClientRpc]
    private void RpcSetCurrentFireAura(ReconnaissanceFireAura fireAura)
    {
        _currentFireAura = fireAura;
        _currentFireAura.FireDarkTalent = _fireDarkTalent;

        if (_fireWorshipperTalent) _currentFireAura.ApplyFireWorshipperTalentEffect(true);
    }

    protected override void ClearData()
    {
        if (_emitterObject != null) _emitterObject.SetActive(false);
        _targetPoint = Vector3.positiveInfinity;
        AnimCastEnded();
        if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
        if (_boostWindow != null) StopCoroutine(_boostWindow);
    }

    #region ReconnaissanceFireAuraDarknesTalent
    public void ReconnaissanceFireAuraDarknesActive(bool value)
    {
        _fireDarkTalent = value;

        if (_currentFireAura != null) _currentFireAura.FireDarkTalent = _fireDarkTalent;
    }
    #endregion

    #region ReconnaissanceFireHealthTalent
    public void ReconnaissanceFireHealthTalentActive(bool value)
    {
        _fireHealthTalent = value;
    }

    private void ReconnaissanceFireHealthTalentEnter()
    {
        if (_fireHealthTalent)
        {
            CmdSetMaxHealth(FireAuraBoostedHealth);
            _fireData.MaxHealth = FireAuraBoostedHealth;
        }
    }

    private void ReconnaissanceFireHealthTalentExit()
    {
        CmdSetMaxHealth(6);
        _fireData.MaxHealth = 6;
    }
    #endregion

    #region partialBlindnessTalent
    public void partialBlindnessTalentActive(bool value)
    {
        _partialBlindnessTalent = value;
        if (_currentFireAura != null) _currentFireAura.FireDarkTalent = _partialBlindnessTalent;
    }
    #endregion

    #region FireWorshipperTalent
    public void FireWorshipperTalentActive(bool value)
    {
        _fireWorshipperTalent = value;
    }

    #endregion

    #region SkillEnableBoostLogicActiveTalent

    public void SkillEnableBoostLogicActiveTalent(bool value) => _isSkillEnableBoostLogicActiveTalent = value;

    #endregion
}
