using Mirror;
using System.Collections;
using UnityEngine;
using HeathenEngineering.UnityPhysics;
using HeathenEngineering.UnityPhysics.API;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
    private Vector3 targetPoint = Vector3.positiveInfinity;
    private float _baseDuration;

    public ReconnaissanceFireAura CurrentFireAura => currentFireAura;

    protected override bool IsCanCast => !float.IsPositiveInfinity(targetPoint.x) && IsPointInRadius(Radius, targetPoint);
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ThrowCastDelay");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        trickShot.speed = speed;
        _baseDuration = duration;
    }

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

    protected override IEnumerator PrepareJob()
    {
        Hero.Animator.speed = Hero.Animator.speed/CastDeley;
        OnSkillCanceled += HandleSkillCanceled;

        if (fireWorshipperTalent) duration += 4;

        if (emitterObject != null)
        {
            emitterObject.SetActive(true);
        }

        ReconnaissanceFireHealthTalentEnter();

        while (float.IsPositiveInfinity(targetPoint.x) && !_disactive)
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint) && NoObstacles(clickedPoint, transform.position, _obstacle))
                {
                    targetPoint = clickedPoint;
                    Hero.Move.LookAtPosition(targetPoint);
                }
            }

            UpdateTrickShotTrajectory();
            yield return null;
        }

        if (emitterObject != null)
        {
            emitterObject.SetActive(false);
        }
    }

    protected override IEnumerator CastJob()
    {
        if (trickShot == null || fireAura == null)
        {
            Debug.LogError("TrickShot or FireAuraPrefab is not assigned!");
            yield break;
        }

        trickShot.Shoot();
    }

    private void HandleProjectilePathEnd(Vector3 position)
    {
        CmdSpawnFireAura(position);
        Invoke("CmdDestroyCurrentFireAura", duration);
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

    private void OnEnable()
    {
        ArrowFireProjectile.OnProjectilePathEnd += HandleProjectilePathEnd;
        trickShot.template.endOfPath.AddListener(OnPathEnd);
    }

    private void OnDisable()
    {
        ArrowFireProjectile.OnProjectilePathEnd -= HandleProjectilePathEnd;
        trickShot.template.endOfPath.RemoveListener(OnPathEnd);
    }

    private void OnPathEnd(float3 endPoint)
    {
        Vector3 adjustedPosition = new Vector3(endPoint.x, endPoint.y, endPoint.z);

        CmdSpawnFireAura(adjustedPosition);
    }

    private void HandleSkillCanceled()
    {
        if (_hero != null && _hero.Move != null)
        {
            Hero.Animator.speed = 1;
            Hero.Move.StopLookAt();
            ReconnaissanceFireHealthTalentExit();
            duration = _baseDuration;
        }
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
        targetPoint = Vector3.positiveInfinity;

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
    #endregion
}
