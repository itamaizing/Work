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

    [Header("Arrows Effects Settings")]
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private float impactLifeTime = 2;

    private const int ZONE_BUFFER_SIZE = 20;

    /// <remarks>
    ///   _head  Ц индекс самой старой (будет использована первой при CastJob).  
    ///   _tail  Ц куда писать новую.  
    ///   _count Ц сколько реально €чеек зан€то.
    /// </remarks>
    [SerializeField] private CircleArea[] _zones = new CircleArea[ZONE_BUFFER_SIZE];
    private int _head;
    private int _tail;
    private int _count;


    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _tripleShot;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotSkyCastDelay");
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _hero.Animator.speed = CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton && IsCanCast) if (TryGetGroundPoint(out Vector3 ground) && IsPointInRadius(Radius, ground)) _targetPoint = ground;
            yield return null;
        }

        DrawDamageZone(_targetPoint);
        var damageZone = DrawDamageZone(_targetPoint);
        AddZone(damageZone);

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    private void RemoveNewestZone()
    {
        if (_count == 0) return;

        int newestIdx = (_tail - 1 + ZONE_BUFFER_SIZE) % ZONE_BUFFER_SIZE;
        if (IsZoneValid(_zones[newestIdx])) Destroy(_zones[newestIdx].gameObject);

        _zones[newestIdx] = null;
        _tail = newestIdx;
        _count--;
    }

    private static bool IsZoneValid(CircleArea damageZone) => damageZone != null;

    protected override IEnumerator CastJob()
    {
        CmdSpawnImpact(_targetPoint);
        yield return new WaitForSeconds(0.6f);

        var damageZone = ConsumeOldestZone();
        if (damageZone) ApplyDamageToEnemiesInZone(damageZone);
    }

    #region ApplyDamageToEnemiesInZone
    private void ApplyDamageToEnemiesInZone(CircleArea damageZone)
    {
        if (damageZone != null)
        {
            Collider[] enemyColliders = Physics.OverlapSphere(damageZone.transform.position, Area, TargetsLayers);
            Collider[] objectColliders = Physics.OverlapSphere(damageZone.transform.position, Area);

            foreach (var enemyCollider in enemyColliders)
            {
                if (enemyCollider.TryGetComponent<IDamageable>(out IDamageable target) && enemyCollider != Hero.gameObject)
                {
                    ApplyDamage(Damage, DamageType.Magical, target);

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
                        _tripleShot = true;
                        StartCoroutine(SpawnAdditionalDamageZones(aura));
                    }
                }
            }

            if (!_tripleShot) StopDamageZone();
        }
    }
    #endregion

    private void AddZone(CircleArea zone)
    {
        if (!zone) return;

        if (_count == ZONE_BUFFER_SIZE)
        {
            if (_zones[_head]) Destroy(_zones[_head].gameObject);
            _head = (_head + 1) % ZONE_BUFFER_SIZE;
            _count--;
        }

        _zones[_tail] = zone;
        _tail = (_tail + 1) % ZONE_BUFFER_SIZE;
        _count++;
    }

    private CircleArea ConsumeOldestZone()
    {
        if (_count == 0) return null;

        var zone = _zones[_head];
        _zones[_head] = null;
        _head = (_head + 1) % ZONE_BUFFER_SIZE;
        _count--;

        if (zone) Destroy(zone.gameObject);

        return zone;
    }

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

        GameObject impact = Instantiate(impactPrefab, position, Quaternion.identity);
        NetworkServer.Spawn(impact);

        RpcScheduleDestroy(impact, impactLifeTime);
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

    [ClientRpc]
    private void RpcScheduleDestroy(GameObject impact, float lifeTime)
    {
        if (impact == null) return;
        Destroy(impact, lifeTime);
    }

    //[Command]
    //private void CmdApplyDamage(GameObject targetObject, Damage damage, Skill skill)
    //{
    //    if (targetObject != null && targetObject.TryGetComponent<IDamageable>(out IDamageable target))
    //    {
    //        target.TryTakeDamage(ref damage, skill);
    //    }
    //}

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        RemoveNewestZone();
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