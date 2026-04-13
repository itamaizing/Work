using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShotIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private HeroComponent playerLinks;
    [SerializeField] private float _dropDelayTime = 3f;
    [SerializeField] private ReconnaissanceFire reconnaissanceFire;
    [SerializeField] private TerrifyingElfAura _terrifyingElfAura;
    
    [Header("Arrow Effects Settings")]
    [SerializeField] private ArrowIntoSkyProjectile impactPrefab;
    [SerializeField] private ParticleSystem arrowIntoSkyEffect;

    private readonly SyncList<uint> _arrowIntoSkyProjectileIds = new SyncList<uint>();
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _secondShotPlanned;
    private bool _tripleShootPlanned;
    private float _baseRadius;
    private const float _extraShotDelay = 1f;

    private float _baseCastDelay;
    private Coroutine _boostWindow;
    private WaitForSeconds _boostDuration = new WaitForSeconds(2f);

    #region Talent
    private bool tripleShotTalentActive;
    private bool shotMagicDebuffActive;
    private bool _isShotRadiusUpgradeActive;
    private bool _isSkillEnableBoostLogicActiveTalent;

    public void ShotsIntoSkyMagicDebuffTalentActive(bool value) => shotMagicDebuffActive = value;
    public void SetTripleShotTalentActive(bool value) => tripleShotTalentActive = value;
    public void ShotRadiusUpgradeActive(bool value)
    {
        _isShotRadiusUpgradeActive = value;

        if (_isShotRadiusUpgradeActive) AreaInfo.Radius *= 3;
        else AreaInfo.Radius = _baseRadius;
    }

    public void SkillEnableBoostLogicActiveTalent(bool value) => _isSkillEnableBoostLogicActiveTalent = value;
    #endregion

    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotSkyCastDelay");
    protected override int AnimTriggerCast => 0;

    private void OnDestroy() => Canceled -= HandleSkillCanceled;

    private void OnEnable()
    {
        _baseRadius = AreaInfo.Radius;
        _baseCastDelay = CastDeley;
        Canceled += HandleSkillCanceled;
    }

        protected override bool IsCanCast
    {
        get
        {
            if (_disactive) return false;

            if (TargetInfoQueue.Count > 0 && TargetInfoQueue.TryPeek(out var target) && target != null && target.Points.Count > 0)
            {
                var point = target.Points[0];
                if (float.IsInfinity(point.x)) return false;
                return Targeting.IsPointInRadius(AreaInfo.Radius, point);
            }

            return Targeting.IsPointInRadius(AreaInfo.Radius, _targetPoint);
        }
    }

    protected override void SkillEnableBoostLogic() => CastDeley = 0;
    protected override void SkillDisableBoostLogic() => CastDeley = _baseCastDelay;

    public void TryStartBoost()
    {
        if (!_isSkillEnableBoostLogicActiveTalent) return;
        if (_boostWindow != null) StopCoroutine(_boostWindow);

        _boostWindow = StartCoroutine(BoostWindow());
    }

    public void ShotAnimationMove()
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

    public void ArrowIntoSkyEffectPlay() => arrowIntoSkyEffect.Play();

    public void ForceCooldownEnd()
    {
        if (_cooldownJob != null)
            StopCoroutine(_cooldownJob);

        RemainingCooldownTime = 0f;
        RaiseCooldownEnded();
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

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Animator.speed = Hero.Animator.speed / CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton) if (TryGetGroundPoint(out Vector3 ground) && Targeting.IsPointInRadius(AreaInfo.Radius, ground)) _targetPoint = ground;
            yield return null;
        }

        CmdSpawnImpact(_targetPoint, Damage, false);

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
                    _secondShotPlanned = true;

                    CmdSpawnImpact(_targetPoint, Damage / 4, true);
                    _tripleShootPlanned = true;
                }

                else
                {
                    CmdSpawnImpact(_targetPoint, Damage / 2, true);
                    _secondShotPlanned = true;
                }
            }
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        CmdExecuteCast();

        if (_secondShotPlanned)
        {
            yield return new WaitForSeconds(_extraShotDelay);
            CmdExecuteCast();
            _secondShotPlanned = false;

            if (_tripleShootPlanned)
            {
                yield return new WaitForSeconds(_extraShotDelay);
                CmdExecuteCast();

                _tripleShootPlanned = false;
            }
        }

        yield return null;
        _hero.Animator.speed = 1f;
        ClearData();
    }

    private IEnumerator BoostWindow()
    {
        EnableSkillBoost();
        yield return _boostDuration;
        DisableSkillBoost();
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

        ArrowIntoSkyProjectile impact = Instantiate(impactPrefab, position, Quaternion.identity);
        impact.Init(playerLinks, this, damage, lastStreamTalent, shotMagicDebuffActive, _terrifyingElfAura.IsElvenSkillPhysDamageHealthChance);
        NetworkServer.Spawn(impact.gameObject);

        _arrowIntoSkyProjectileIds.Add(impact.GetComponent<NetworkIdentity>().netId);

        RpcInit(impact.gameObject, damage, lastStreamTalent);
    }

    [Command]
    private void CmdExecuteCast()
    {
        CleanupProjectileList();

        if (_arrowIntoSkyProjectileIds.Count == 0) return;

        uint id = _arrowIntoSkyProjectileIds[0];
        _arrowIntoSkyProjectileIds.RemoveAt(0);

        StartCoroutine(ActivateAfterDelay(id));
    }

    [Command] private void CmdDestroyPendingImpacts() => ServerDestroyPendingImpacts();

    [ClientRpc]
    protected void RpcInit(GameObject gameObject, float damage, bool lastStreamTalent)
    {
        if (gameObject == null) return;

        ArrowIntoSkyProjectile impact = gameObject.GetComponent<ArrowIntoSkyProjectile>();
        if (impact != null) impact.Init(playerLinks, this, damage, lastStreamTalent, shotMagicDebuffActive, _terrifyingElfAura.IsElvenSkillPhysDamageHealthChance);
    }
 
    [ClientRpc] private void RpcActivate(ArrowIntoSkyProjectile projectile) => projectile.Activate();

    [Server]
    private void CleanupProjectileList()
    {
        for (int i = _arrowIntoSkyProjectileIds.Count - 1; i >= 0; i--)
            if (!NetworkServer.spawned.TryGetValue(_arrowIntoSkyProjectileIds[i], out var ni) || ni == null) _arrowIntoSkyProjectileIds.RemoveAt(i);
    }

    [Server]
    private void ServerDestroyPendingImpacts(int count = 1)
    {
        while (count-- > 0 && _arrowIntoSkyProjectileIds.Count > 0)
        {
            uint id = _arrowIntoSkyProjectileIds[0];
            _arrowIntoSkyProjectileIds.RemoveAt(0);

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

        var projectile = netIdentity.GetComponent<ArrowIntoSkyProjectile>();
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

    public override void LoadTargetData(TargetInfo targetInfo) => _targetPoint = targetInfo.Points[0];
}
