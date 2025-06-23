using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ShotIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private bool silenceTalentActive;
    [SerializeField] private bool tripleShotTalentActive;
    [SerializeField] private bool shotAstralManaActive;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private HeroComponent playerLinks;

    [Header("Arrows Effects Settings")]
    [SerializeField] private ArrowIntoSkyProjectile impactPrefab;

    private readonly SyncList<uint> _arrowIntoSkyProjectileIds = new SyncList<uint>();
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _tripleShot;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotSkyCastDelay");
    protected override int AnimTriggerCast => 0;

    private void OnDestroy() => Canceled -= HandleSkillCanceled;
    private void OnEnable() => Canceled += HandleSkillCanceled;

    protected override bool IsCanCast => !_disactive && IsPointInRadius(Radius, _targetPoint);


    public void ShotAnimationMove()
    {
        _hero.Move.StopMoveAnimation();
        _hero.Move.CanMove = false;
        _hero.Move.LookAtPosition(_targetPoint);
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null)
        {
            Hero.Move.CanMove = true;
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
            if (GetMouseButton) if (TryGetGroundPoint(out Vector3 ground) && IsPointInRadius(Radius, ground)) _targetPoint = ground;
            yield return null;
        }

        CmdSpawnImpact(_targetPoint);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        CmdExecuteCast();
        yield return null;
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
    private void CmdSpawnImpact(Vector3 position)
    {
        if (!impactPrefab) return;

        ArrowIntoSkyProjectile impact = Instantiate(impactPrefab, position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(impact.gameObject, _hero.NetworkSettings.MyRoom);
        impact.Init(playerLinks, this, silenceTalentActive, tripleShotTalentActive, shotAstralManaActive);
        NetworkServer.Spawn(impact.gameObject);

        _arrowIntoSkyProjectileIds.Add(impact.GetComponent<NetworkIdentity>().netId);

        RpcInit(impact.gameObject);
    }

    [Command]
    private void CmdExecuteCast()
    {
        CleanupProjectileList();

        if (_arrowIntoSkyProjectileIds.Count == 0) return;

        uint id = _arrowIntoSkyProjectileIds[0];
        _arrowIntoSkyProjectileIds.RemoveAt(0);

        if (!NetworkServer.spawned.TryGetValue(id, out var networkIdentity)) return;

        var projectile = networkIdentity.GetComponent<ArrowIntoSkyProjectile>();
        projectile.Activate();
        RpcActivate(projectile);
    }

    [Command] private void CmdDestroyPendingImpacts() => ServerDestroyPendingImpacts();

    [ClientRpc]
    protected void RpcInit(GameObject gameObject)
    {
        if (gameObject == null) return;

        ArrowIntoSkyProjectile impact = gameObject.GetComponent<ArrowIntoSkyProjectile>();
        if (impact != null) impact.Init(playerLinks, this, silenceTalentActive, tripleShotTalentActive, shotAstralManaActive);
    }
 
    [ClientRpc] private void RpcActivate(ArrowIntoSkyProjectile projectile) => projectile.Activate();
    [ClientRpc] public void TargetResetCooldown(float reset) => ReductionSetCooldown(reset);

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

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        _hero.Move.StopLookAt();
        _hero.Move.CanMove = true;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }

    #region ReconnaissanceFireArrowIntoSkyTalent
    public void SetTripleShotTalentActive(bool value)
    {
        tripleShotTalentActive = value;
    }
    #endregion

    #region silenceTalent
    public void SetSilenceTalentActive(bool value)
    {
        silenceTalentActive = value;
    }
    #endregion

    #region ShotsIntoSkyAstralTalent
    public void ShotsIntoSkyAstralTalentActive(bool value)
    {
        shotAstralManaActive = value;
    }
    #endregion
}
