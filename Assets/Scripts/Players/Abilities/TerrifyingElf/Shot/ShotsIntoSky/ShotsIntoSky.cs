using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShotsIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private bool tripleShotTalentActive;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private float _dropDelayTime = 1f;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;

    [Header("Arrows Effects Settings")]
    [SerializeField] private ArrowsIntoSkyProjectile impactPrefab;
    [SerializeField] private ParticleSystem arrowsIntoSkyEffect;

    private readonly SyncList<uint> _arrowsIntoSkyProjectileIds = new SyncList<uint>();
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _secondShotPlanned;
    private bool _tripleShootPlanned;
    private const float _extraShotDelay = 1f;

    private float _impactLifeTime = 2;
    private float _baseCastDelay;
    private Coroutine _boostWindow;
    private WaitForSeconds _boostDuration = new WaitForSeconds(2f);

    #region Talent
    
    private bool shotMagicDebuffActive;

    public void ShotsIntoSkyMagicDebuffTalentActive(bool value) => shotMagicDebuffActive = value;

    #endregion

    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotsSkyCastDelay");
    protected override int AnimTriggerCast => 0;


    protected override bool IsCanCast => Targeting.IsPointInRadius(AreaInfo.Radius, _targetPoint);
    
    private void OnDestroy() => Canceled -= HandleSkillCanceled;

    private void OnEnable()
    {
        _baseCastDelay = CastDeley;
        Canceled += HandleSkillCanceled;
    }

    protected override void SkillEnableBoostLogic() => CastDeley = 0;
    protected override void SkillDisableBoostLogic() => CastDeley = _baseCastDelay;

    public void ShotsAnimationMove()
    {
        if (_hero == null || _hero.Move == null) return;

        _hero.Move.StopMoveAndAnimationMove();
        _hero.Move.SetCanMove(false);

        Vector3 direction = _targetPoint - _hero.transform.position;
        bool badDirection = float.IsInfinity(_targetPoint.x) || direction.sqrMagnitude < 0.0001f;

        if (badDirection)
        {
            _hero.Move.StopLookAt();
            return;
        }

        _hero.Move.LookAtPosition(_targetPoint);
    }

    public void ArrowsIntoSkyEffectPlay() => arrowsIntoSkyEffect.Play();

    public void TryStartBoost()
    {
        if (_boostWindow != null) StopCoroutine(_boostWindow);

        _boostWindow = StartCoroutine(BoostWindow());
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.SetCanMove(true);
            Hero.Animator.speed = 1;
            Hero.Move.StopLookAt();

            if (isServer) ServerDestroyPendingImpacts();
            else CmdDestroyPendingImpacts();
        }
    }

    private IEnumerator BoostWindow()
    {
        EnableSkillBoost();
        yield return _boostDuration;
        DisableSkillBoost();
    }
    
    protected override void PlayPrepareAnim()
    {
        if (CastDeley > 0f)
            Hero.Animator.speed = Hero.Animator.speed / CastDeley;
        
        base.PlayPrepareAnim();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x) && !_disactive)
        {
            if (GetMouseButton)
            {
                if (TryGetGroundPoint(out Vector3 ground))
                {
                    targetPoint = ground;
                    
                    if (Targeting.IsPointInRadius(AreaInfo.Radius, targetPoint))
                    {
                        Hero.Move.LookAtPosition(targetPoint);
                    }
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
        if (float.IsInfinity(_targetPoint.x)) yield break;

        Hero.Move.StopLookAt();
        Hero.Move.SetCanMove(true);

        int projectileCount = 0;

        CmdSpawnImpact(_targetPoint, Damage, false);
        projectileCount++;

        if (tripleShotTalentActive && reconnaissanceFire != null && reconnaissanceFire.CurrentFireAura != null)
        {
            Vector3 auraCenter = reconnaissanceFire.CurrentFireAura.transform.position;
            float combinedRadius = AreaInfo.Area + reconnaissanceFire.AreaInfo.Area;
            float distantion = Vector3.Distance(_targetPoint, auraCenter);

            if (distantion <= combinedRadius / 2)
            {
                if (reconnaissanceFire.CurrentFireAura.StateDark)
                {
                    CmdSpawnImpact(_targetPoint, Damage / 2, false);
                    projectileCount++;

                    CmdSpawnImpact(_targetPoint, Damage / 4, true);
                    projectileCount++;
                }
                else
                {
                    CmdSpawnImpact(_targetPoint, Damage / 2, true);
                    projectileCount++;
                }
            }
        }
        
        CmdSpawnImpact(_targetPoint, Damage, false);
        projectileCount++;
        
        CmdExecuteAllWaves();

        _hero.Animator.speed = 1f;
        ClearData();
    }

    private bool TryGetGroundPoint(out Vector3 groundPoint)
    {
        groundPoint = Vector3.zero;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hits = Physics.RaycastAll(ray, 100f, ~0).OrderBy(hit => hit.distance);


        foreach (var hit in hits)
        {
            if (hit.collider.GetComponent<Character>() != null) continue;
            if ((groundLayer.value & (1 << hit.collider.gameObject.layer)) == 0) continue;

            groundPoint = hit.point;
            return true;
        }

        return false;
    }


    [Command]
    private void CmdSpawnImpact(Vector3 position, float damage, bool lastStreamTalent)
    {
        if (!impactPrefab) return;

        ArrowsIntoSkyProjectile impact = Instantiate(impactPrefab, position, Quaternion.identity);
        impact.Init(playerLinks, this, damage, lastStreamTalent, shotMagicDebuffActive);
        
        NetworkServer.Spawn(impact.gameObject, connectionToClient);

        _arrowsIntoSkyProjectileIds.Add(impact.GetComponent<NetworkIdentity>().netId);

        RpcInit(impact.gameObject, damage, lastStreamTalent);
    }

    [Command]
    private void CmdExecuteAllWaves()
    {
        StartCoroutine(ServerExecuteAllWavesRoutine());
    }

    [Server]
    private IEnumerator ServerExecuteAllWavesRoutine()
    {
        yield return new WaitForSeconds(_dropDelayTime);

        CleanupProjectileList();

        while (_arrowsIntoSkyProjectileIds.Count > 0)
        {
            uint id = _arrowsIntoSkyProjectileIds[0];
            _arrowsIntoSkyProjectileIds.RemoveAt(0);

            if (NetworkServer.spawned.TryGetValue(id, out var netIdentity) && netIdentity != null)
            {
                var projectile = netIdentity.GetComponent<ArrowsIntoSkyProjectile>();
                if (projectile != null)
                {
                    projectile.Activate();
                    RpcActivate(projectile);
                }
            }

            if (_arrowsIntoSkyProjectileIds.Count > 0)
            {
                yield return new WaitForSeconds(_extraShotDelay);
            }
        }
    }

    [Command] private void CmdDestroyPendingImpacts() => ServerDestroyPendingImpacts();

    [ClientRpc]
    protected void RpcInit(GameObject gameObject, float damage, bool lastStreamTalent)
    {
        if (gameObject == null) return;

        ArrowsIntoSkyProjectile impact = gameObject.GetComponent<ArrowsIntoSkyProjectile>();
        if (impact != null) impact.Init(playerLinks, this, damage, lastStreamTalent, shotMagicDebuffActive);
    }

    [ClientRpc] private void RpcActivate(ArrowsIntoSkyProjectile projectile) => projectile.Activate();

    [Server]
    private void CleanupProjectileList()
    {
        for (int i = _arrowsIntoSkyProjectileIds.Count - 1; i >= 0; i--)
            if (!NetworkServer.spawned.TryGetValue(_arrowsIntoSkyProjectileIds[i], out NetworkIdentity networkIdentity) || networkIdentity == null) _arrowsIntoSkyProjectileIds.RemoveAt(i);
    }

    [Server]
    private void ServerDestroyPendingImpacts(int count = 1)
    {
        while (count-- > 0 && _arrowsIntoSkyProjectileIds.Count > 0)
        {
            uint id = _arrowsIntoSkyProjectileIds[0];
            _arrowsIntoSkyProjectileIds.RemoveAt(0);

            if (NetworkServer.spawned.TryGetValue(id, out NetworkIdentity networkIdentity) && networkIdentity != null)
                NetworkServer.Destroy(networkIdentity.gameObject);
        }
    }

    [Server]
    private IEnumerator ActivateAfterDelay(uint projectileNetId)
    {
        yield return new WaitForSeconds(_dropDelayTime);

        if (!NetworkServer.spawned.TryGetValue(projectileNetId, out var netIdentity) || netIdentity == null)
            yield break;

        var projectile = netIdentity.GetComponent<ArrowsIntoSkyProjectile>();
        if (projectile == null) yield break;

        projectile.Activate();
        RpcActivate(projectile);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        _hero.Move.StopLookAt();
        _hero.Move.SetCanMove(true);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo != null && targetInfo.Points.Count > 0)
        {
            _targetPoint = targetInfo.Points[0];
        }
    }

    #region ReconnaissanceFireArrowIntoSkyTalent
    public void SetTripleShotTalentActive(bool value)
    {
        tripleShotTalentActive = value;
    }
    #endregion
}
