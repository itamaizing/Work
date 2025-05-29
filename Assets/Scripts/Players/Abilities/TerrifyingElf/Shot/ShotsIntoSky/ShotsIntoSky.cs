using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class ShotsIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private bool silenceTalentActive;
    [SerializeField] private bool tripleShotTalentActive;
    [SerializeField] private bool shotAstralManaActive;

    [Header("Arrows Effects Settings")]
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private float impactLifeTime = 2;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _tripleShot;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _hero.Animator.speed = CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton && IsCanCast)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint))
                {
                    _targetPoint = clickedPoint;
                }
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        yield return new WaitForSeconds(0.6f);

        ApplyDamageToEnemiesInZone();
        _hero.Animator.speed = 1f;
    }

    #region ApplyDamageToEnemiesInZone
    private void ApplyDamageToEnemiesInZone()
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