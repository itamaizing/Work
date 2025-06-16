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
    [SerializeField] private bool fireDarkTalent;
    [SerializeField] private bool fireHealthTalent;
    [SerializeField] private bool partialBlindnessTalent;
    [SerializeField] private bool fireWorshipperTalent;
    [SerializeField] private ObjectData fireData;
    [SerializeField] private float duration = 10;

    [Header("TrickShot Settings")]
    [SerializeField] private List<Vector3> globalConstants = new(new Vector3[] { new(0, -9.81f, 0) });
    [SerializeField] private List<Vector3> localConstants = new();
    [SerializeField] private float speed;

    private ReconnaissanceFireAura currentFireAura;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private float _baseDuration;
    private float _baseAnimSpeed;

    public ReconnaissanceFireAura CurrentFireAura => currentFireAura;

    protected override bool IsCanCast => !float.IsPositiveInfinity(_targetPoint.x) && IsPointInRadius(Radius, _targetPoint);
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ThrowCastDelay");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        _baseAnimSpeed = Hero.Animator.speed;
        trickShot.speed = speed;
        _baseDuration = duration;
    }

    private void OnEnable() => ArrowFireProjectile.OnProjectilePathEnd += HandleProjectilePathEnd;
    private void OnDisable() => ArrowFireProjectile.OnProjectilePathEnd -= HandleProjectilePathEnd;

    private void OnDestroy()
    {
        OnSkillCanceled -= HandleSkillCanceled;
    }

    [Command]
    private void CmdDestroyCurrentFireAura()
    {
        if (currentFireAura != null) NetworkServer.Destroy(currentFireAura.gameObject);

        currentFireAura = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Animator.speed = Hero.Animator.speed/CastDeley;
        OnSkillCanceled += HandleSkillCanceled;

        if (emitterObject) emitterObject.SetActive(true);
        ReconnaissanceFireHealthTalentEnter();

        while (float.IsPositiveInfinity(_targetPoint.x))
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint) && NoObstacles(clickedPoint, transform.position, _obstacle))
                {
                    _targetPoint = clickedPoint;
                    Hero.Move.LookAtPosition(_targetPoint);
                }
            }

            UpdateTrickShotTrajectory();
            yield return null;
        }

        if (emitterObject) emitterObject.SetActive(false);
    }

    protected override IEnumerator CastJob()
    {
        if (trickShot == null || fireAura == null) yield break;

        trickShot.Shoot();

        Hero.Animator.speed = _baseAnimSpeed;
        Hero.Move.StopLookAt();
    }

    private void HandleProjectilePathEnd(Vector3 position)
    {
        float auraDuration = _baseDuration + (fireWorshipperTalent ? 4f : 0f);

        CmdSpawnFireAura(_targetPoint);
        Invoke("CmdDestroyCurrentFireAura", auraDuration);
        _targetPoint = Vector3.positiveInfinity;
    }

    private void UpdateTrickShotTrajectory()
    {
        Vector3 accelerationSum = Vector3.zero;
        foreach (var constant in globalConstants)
        {
            accelerationSum += constant;
        }
        foreach (var constant in localConstants)
        {
            accelerationSum += trickShot.transform.rotation * constant;
        }

        trickShot.constantAcceleration = accelerationSum;

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            int solutions = Ballistics.Solution(trickShot.transform.position, trickShot.speed, hit.point, trickShot.constantAcceleration, out Quaternion low, out Quaternion _);
            if (solutions > 0)
            {
                trickShot.transform.rotation = low;
            }
        }
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null) ReconnaissanceFireHealthTalentExit();
        Hero.Animator.speed = _baseAnimSpeed;
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
        if (currentFireAura != null) NetworkServer.Destroy(currentFireAura.gameObject);

        position.y += 0.1f;

        ReconnaissanceFireAura fireAura = Instantiate(this.fireAura, position, Quaternion.identity);

        NetworkServer.Spawn(fireAura.gameObject);
        SceneManager.MoveGameObjectToScene(fireAura.gameObject, Hero.NetworkSettings.MyRoom);
        currentFireAura = fireAura;
        currentFireAura.FireDarkTalent = fireDarkTalent;
        RpcSetCurrentFireAura(fireAura);
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

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }
    #endregion
}
