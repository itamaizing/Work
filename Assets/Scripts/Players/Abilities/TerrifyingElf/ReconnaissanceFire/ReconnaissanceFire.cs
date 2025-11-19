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
    [SerializeField] private TrickShot trickShot;
    [SerializeField] private ReconnaissanceFireAura fireAura;
    [SerializeField] private GameObject emitterObject;
    [SerializeField] private ObjectData fireData;
    [SerializeField] private float duration = 10;
    [SerializeField] private float baseArea = 3f;

    [Header("TrickShot Settings")]
    [SerializeField] private float speed;

    [Header("Raycast settings")]
    [SerializeField] private LayerMask groundLayer;

    private ReconnaissanceFireAura currentFireAura;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Vector3 _endPoint = Vector3.positiveInfinity;
    private float _baseDuration;
    private float _baseAnimSpeed;
    private float _baseCastDelay;
    private Coroutine _auraLifeCoroutine;
    private Coroutine _boostWindow;
    private bool isSkillEnableBoostLogic;

    #region Talent

    private bool fireDarkTalent;
    private bool fireHealthTalent;
    private bool partialBlindnessTalent;
    private bool fireWorshipperTalent;
    private bool isSkillEnableBoostLogicActiveTalent;

    #endregion

    public ReconnaissanceFireAura CurrentFireAura => currentFireAura;
    public float BaseArea { get => baseArea; set => baseArea = value; }

    protected override bool IsCanCast => Vector3.Distance(_targetPoint, transform.position) <= Radius;
    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => Animator.StringToHash("ThrowCastDelay");

    protected override void SkillEnableBoostLogic()
    {
        CastDeley = 0;
        isSkillEnableBoostLogic = true;
        Debug.Log("SkillEnableBoostLogic");
    }
    protected override void SkillDisableBoostLogic()
    {
        CastDeley = _baseCastDelay;
        isSkillEnableBoostLogic = false;
    }

    private void Start()
    {
        _baseAnimSpeed = Hero.Animator.speed;
        trickShot.speed = speed;
        _baseDuration = duration;
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
        bool badDirection = float.IsInfinity(_targetPoint.x) || direction.sqrMagnitude < 0.0001f;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(_targetPoint);
    }

    public void TryStartElvenBoostWindow()
    {
        if (!isSkillEnableBoostLogicActiveTalent) return;
        if (_boostWindow != null) return;
        if (UnityEngine.Random.value > 0.30f) return;

        _boostWindow = StartCoroutine(ElvenBoostWindow());
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
        _endPoint = _targetPoint;
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Animator.speed = Hero.Animator.speed/ 1.8f;
        _endPoint = Vector3.positiveInfinity;

        if (emitterObject) emitterObject.SetActive(true);
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

        if (emitterObject) emitterObject.SetActive(false);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_targetPoint == Vector3.positiveInfinity && (trickShot == null || fireAura == null)) yield break;

        trickShot.Shoot();

        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.StopLookAt();
        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.CanMove = true;
    }

    private IEnumerator ElvenBoostWindow()
    {
        EnableSkillBoost();
        yield return new WaitForSeconds(2f);
        DisableSkillBoost();
        _boostWindow = null;
    }

    private void HandleProjectilePathEnd(Vector3 position) => CmdSpawnFireAura(_endPoint);

    void UpdateTrickShotTrajectory()
    {
         if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 200f, groundLayer))
        {
            float s = Vector3.Distance(trickShot.transform.position, hit.point); trickShot.distance = s + 1f;

            if (Ballistics.Solution(trickShot.transform.position, trickShot.speed, hit.point, trickShot.constantAcceleration, out Quaternion low, out _) > 0) trickShot.transform.rotation = low;
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null) ReconnaissanceFireHealthTalentExit();
        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
        _targetPoint = Vector3.positiveInfinity;
    }

    [Command]
    private void CmdSetMaxHealth(float maxHealth)
    {
        fireData.MaxHealth = maxHealth;
    }

    [Command]
    private void CmdSpawnFireAura(Vector3 position)
    {
        if (float.IsInfinity(position.x) || float.IsNaN(position.x)) return;

        if (!isSkillEnableBoostLogic)
        {
            if (_auraLifeCoroutine != null) StopCoroutine(_auraLifeCoroutine);
            if (currentFireAura != null) NetworkServer.Destroy(currentFireAura.gameObject);
        }

        position.y += 0.1f;
        var aura = Instantiate(fireAura, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(aura.gameObject, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(aura.gameObject, connectionToClient);

        currentFireAura = aura;
        currentFireAura.FireDarkTalent = fireDarkTalent;
        RpcSetCurrentFireAura(aura);

        float life = _baseDuration + (fireWorshipperTalent ? 6f : 0f);
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
        currentFireAura = fireAura;
        currentFireAura.FireDarkTalent = fireDarkTalent;

        if (fireWorshipperTalent) currentFireAura.ApplyFireWorshipperTalentEffect(true);
    }

    protected override void ClearData()
    {
        if (emitterObject != null) emitterObject.SetActive(false);
        _targetPoint = Vector3.positiveInfinity;
    }

    #region ReconnaissanceFireAuraDarknesTalent
    public void ReconnaissanceFireAuraDarknesActive(bool value)
    {
        fireDarkTalent = value;

        if (currentFireAura != null) currentFireAura.FireDarkTalent = fireDarkTalent;
    }
    #endregion

    #region ReconnaissanceFireHealthTalent
    public void ReconnaissanceFireHealthTalentActive(bool value)
    {
        fireHealthTalent = value;
    }

    private void ReconnaissanceFireHealthTalentEnter()
    {
        if (fireHealthTalent)
        {
            CmdSetMaxHealth(65);
            fireData.MaxHealth = 65;
        }
    }

    private void ReconnaissanceFireHealthTalentExit()
    {
        CmdSetMaxHealth(6);
        fireData.MaxHealth = 6;
    }
    #endregion

    #region partialBlindnessTalent
    public void partialBlindnessTalentActive(bool value)
    {
        partialBlindnessTalent = value;
        if (currentFireAura != null) currentFireAura.FireDarkTalent = partialBlindnessTalent;
    }
    #endregion

    #region FireWorshipperTalent
    public void FireWorshipperTalentActive(bool value)
    {
        fireWorshipperTalent = value;
    }

    #endregion

    #region SkillEnableBoostLogicActiveTalent

    public void SkillEnableBoostLogicActiveTalent(bool value) => isSkillEnableBoostLogicActiveTalent = value;

    #endregion
}
