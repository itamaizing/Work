using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class ShotsIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private bool silenceTalentActive;
    [SerializeField] private bool tripleShotTalentActive;
    [SerializeField] private bool shotAstralManaActive;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private HeroComponent playerLinks;

    [Header("Arrows Effects Settings")]
    [SerializeField] private ArrowsIntoSkyProjectile impactPrefab;

    private readonly SyncList<uint> _arrowsIntoSkyProjectileIds = new SyncList<uint>();
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _tripleShot;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotSkyCastDelay");
    protected override int AnimTriggerCast => 0;

    private void OnDestroy()
    {
        //Canceled -= HandleManualCancel;
    }

    private void OnEnable()
    {
        //Canceled += HandleManualCancel;
    }


    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _hero.Animator.speed = CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton && IsCanCast)
            {
                if (TryGetGroundPoint(out Vector3 ground) && IsPointInRadius(Radius, ground)) _targetPoint = ground;
            }
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
    }

    #region ApplyDamageToEnemiesInZone
    [Server]
    private void ApplyDamageToEnemiesInZone(SphereCollider damageZone)
    {
        if (damageZone == null) return;

        float radius = damageZone.radius * damageZone.transform.lossyScale.x;

        Collider[] hits = Physics.OverlapSphere(damageZone.transform.position, radius, TargetsLayers);

        foreach (var hit in hits)
        {
            if (hit.gameObject == Hero.gameObject) continue;

            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                ApplyDamage(Damage, DamageType.Magical, target);

                if (hit.TryGetComponent<Character>(out var character))
                {
                    var state = character.CharacterState;
                    if (state == null) continue;

                    CmdAddState(state);

                    if (shotAstralManaActive && state.CheckForState(States.Astral))
                        RestoreMana();

                    if (silenceTalentActive &&
                        state.CheckForState(States.Silent))
                        CmdAddWeakeningSilence(state);
                }
            }
        }
    }
    #endregion

    private void ApplyDamage(float damage, DamageType damageType, IDamageable target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = damageType,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        if (target is Component targetComponent)
        {
            CmdApplyDamage(_damage, targetComponent.gameObject);
            //CmdApplyDamage(targetComponent.gameObject, _damage, null);
        }
    }

    //private void HandleManualCancel()
    //{
    //    if (_arrowsIntoSkyProjectileIds.Count == 0) return;

    //    if (isServer) CancelPendingProjectile();
    //    else CmdCancelPendingProjectile();
    //}

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

        ArrowsIntoSkyProjectile impact = Instantiate(impactPrefab, position, Quaternion.identity);
        impact.Init(playerLinks, this);
        NetworkServer.Spawn(impact.gameObject);

        _arrowsIntoSkyProjectileIds.Add(impact.GetComponent<NetworkIdentity>().netId);

        RpcInit(impact.gameObject);
    }

    [Command]
    private void CmdAddState(CharacterState targetState)
    {
        targetState.AddState(States.Irradiation, 9, 0, Hero.gameObject, this.name);
    }

    [Command]
    private void CmdAddWeakeningSilence(CharacterState targetState)
    {
        targetState.AddState(States.WeakeningSilence, 4, 4, Hero.gameObject, this.name);
    }

    [Command]
    private void CmdExecuteCast()
    {
        CleanupProjectileList();

        if (_arrowsIntoSkyProjectileIds.Count == 0) return;

        uint id = _arrowsIntoSkyProjectileIds[0];
        _arrowsIntoSkyProjectileIds.RemoveAt(0);

        if (!NetworkServer.spawned.TryGetValue(id, out var networkIdentity)) return;

        var projectile = networkIdentity.GetComponent<ArrowsIntoSkyProjectile>();
        projectile.Activate();
        RpcActivate(projectile);

        ApplyDamageToEnemiesInZone(projectile.DamageCollider);
    }

    //[Command]
    //private void CmdCancelPendingProjectile()
    //{
    //    CancelPendingProjectile();
    //}

    //[Server]
    //private void CancelPendingProjectile()
    //{
    //    uint id = _arrowsIntoSkyProjectileIds[0];
    //    _arrowsIntoSkyProjectileIds.RemoveAt(0);

    //    if (NetworkServer.spawned.TryGetValue(id, out var ni) && ni != null)
    //        NetworkServer.Destroy(ni.gameObject);
    //}

    [ClientRpc]
    protected void RpcInit(GameObject gameObject)
    {
        if (gameObject == null) return;

        ArrowsIntoSkyProjectile impact = gameObject.GetComponent<ArrowsIntoSkyProjectile>();
        if (impact != null) impact.Init(playerLinks, this);
    }

    [ClientRpc]
    private void RpcActivate(ArrowsIntoSkyProjectile projectile)
    {
        projectile.Activate();
    }

    //[Command]
    //private void CmdApplyDamage(GameObject targetObject, Damage damage, Skill skill)
    //{
    //    if (targetObject != null && targetObject.TryGetComponent<IDamageable>(out IDamageable target))
    //    {
    //        target.TryTakeDamage(ref damage, skill);
    //    }
    //}

    [Server]
    private void CleanupProjectileList()
    {
        for (int i = _arrowsIntoSkyProjectileIds.Count - 1; i >= 0; i--)
            if (!NetworkServer.spawned.TryGetValue(_arrowsIntoSkyProjectileIds[i], out var ni) || ni == null) _arrowsIntoSkyProjectileIds.RemoveAt(i);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }

    #region silenceTalent
    public void SetSilenceTalentActive(bool value)
    {
        silenceTalentActive = value;
    }
    #endregion

    #region ReconnaissanceFireArrowIntoSkyTalent
    public void SetTripleShotTalentActive(bool value)
    {
        tripleShotTalentActive = value;
    }

    private void ApplyAdditionalDamage(float damageValue)
    {
        CircleArea damageZone = skillRenderer.TempDamageZone;

        if (damageZone != null)
        {
            Collider[] enemyColliders = Physics.OverlapSphere(damageZone.transform.position, Area, TargetsLayers);
            Collider[] objectColliders = Physics.OverlapSphere(damageZone.transform.position, Area);

            foreach (var enemyCollider in enemyColliders)
            {
                if (enemyCollider.TryGetComponent<IDamageable>(out IDamageable target) && enemyCollider != Hero.gameObject)
                {
                    ApplyDamage(damageValue, DamageType.Magical, target);

                    if (enemyCollider.TryGetComponent<Character>(out Character character))
                    {
                        var targetState = character.CharacterState;

                        if (targetState != null)
                        {
                            CmdAddState(targetState);

                            if (shotAstralManaActive && targetState.CheckForState(States.Astral)) RestoreMana();
                            if (targetState.CheckForState(States.Silent) && silenceTalentActive) CmdAddWeakeningSilence(targetState);
                        }
                    }
                }
            }

            foreach (var objectCollider in objectColliders)
            {
                if (objectCollider.TryGetComponent<ReconnaissanceFireAura>(out ReconnaissanceFireAura aura) && tripleShotTalentActive)
                {
                    if (FindObjectOfType<NatureTalent_6>() != null && !_tripleShot)
                    {
                        StartCoroutine(SpawnAdditionalDamageZones(aura));
                    }
                }
            }
        }
    }

    private IEnumerator SpawnAdditionalDamageZones(ReconnaissanceFireAura aura)
    {
        yield return new WaitForSeconds(1f);
        ApplyAdditionalDamage(Damage / 2);

        if (aura.StateDark)
        {
            yield return new WaitForSeconds(1f);
            ApplyAdditionalDamage(Damage / 4);
            _tripleShot = false;
            StopDamageZone();
            yield break;
        }

        _tripleShot = false;
        StopDamageZone();
        yield break;
    }
    #endregion

    #region ShotsIntoSkyAstralTalent
    public void ShotsIntoSkyAstralTalentActive(bool value)
    {
        shotAstralManaActive = value;
    }

    private void RestoreMana()
    {
        if (Hero.TryGetResource(ResourceType.Mana) is Mana manaResource)
        {
            float manaToRestore = manaResource.MaxValue * 0.03f;
            manaResource.Add(manaToRestore);
            Hero.CharacterState.CmdAddState(States.ManaRegen, 1, 0, Hero.gameObject, this.name);
        }
    }
    #endregion
}