using System.Collections;
using Mirror;
using UnityEngine;

public class ShotIntoSky : Skill
{
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField] private bool silenceTalentActive;
    [SerializeField] private bool tripleShotTalentActive;
    [SerializeField] private bool shotAstralManaActive;
    [SerializeField, Range(0f, 100f)] private float criticalChance = 30f;
    [SerializeField, Range(0f, 100f)] private float stunChance = 30f;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _tripleShot;
    private const float CriticalMultiplier = 2.4f;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotCastDelayAnimTrigger");

    protected override int AnimTriggerCast => 0;

    protected override IEnumerator PrepareJob()
    {
        _hero.Animator.speed = CastDeley;

        while (float.IsPositiveInfinity(_targetPoint.x) && !Disactive)
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
    }

    protected override IEnumerator CastJob()
    {
        DrawDamageZone(_targetPoint);
        Damage = Random.Range(minDamage, maxDamage + 1);

        yield return new WaitForSeconds(0.1f);

        ApplyDamageToEnemiesInZone();
        _hero.Animator.speed = 1f;
    }

    #region ApplyDamageToEnemiesInZone
    private void ApplyDamageToEnemiesInZone()
    {
        CircleArea damageZone = skillRenderer.TempDamageZone;

        if (damageZone != null)
        {
            Collider[] hitColliders = Physics.OverlapSphere(damageZone.transform.position, Area, TargetsLayers);
            Collider[] objectColliders = Physics.OverlapSphere(damageZone.transform.position, Area);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<IDamageable>(out IDamageable target) && hitCollider != Hero.gameObject)
                {
                    float finalDamage = CalculateDamage(Damage);
                    ApplyDamage(finalDamage, DamageType.Physical, target);

                    if (hitCollider.TryGetComponent<Character>(out Character character))
                    {
                        var targetState = character.CharacterState;
                        if (targetState != null)
                        {
                            if (shotAstralManaActive && targetState.CheckForState(States.Astral)) RestoreMana();
                            if (targetState.CheckForState(States.Silent) && silenceTalentActive) CmdAddWeakeningSilence(targetState);
                            if (Random.Range(0f, 100f) <= stunChance) CmdAddState(targetState);
                        }
                    }
                }
            }

            foreach (var objectCollider in objectColliders)
            {
                if (objectCollider.TryGetComponent<ReconnaissanceFireAura>(out ReconnaissanceFireAura aura) && tripleShotTalentActive)
                {
                    if (FindObjectOfType<ReconnaissanceFireArrowIntoSkyTalent>() != null && !_tripleShot)
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

    private float CalculateDamage(float baseDamage)
    {
        bool isCriticalHit = Random.Range(0f, 100f) <= criticalChance;

        if (isCriticalHit)
        {
            return baseDamage * CriticalMultiplier;
        }

        return baseDamage;
    }

    private void ApplyDamage(float damage, DamageType damageType, IDamageable target)
    {
        Damage _damage = new Damage
        {
            Value = damage,
            Type = damageType,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        if (target is Character targetComponent)
        {
            CmdApplyDamage(_damage, targetComponent.gameObject);
        }
    }

    [Command]
    private void CmdAddState(CharacterState targetState)
    {
        targetState.AddState(States.Stun, 2, 0, Hero.gameObject, this.name);
    }

    [Command]
    private void CmdAddWeakeningSilence(CharacterState targetState)
    {
        targetState.AddState(States.WeakeningSilence, 4, 4, Hero.gameObject, this.name);
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
            Collider[] hitColliders = Physics.OverlapSphere(damageZone.transform.position, Area, TargetsLayers);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<IDamageable>(out IDamageable target) && hitCollider != Hero.gameObject)
                {
                    float finalDamage = CalculateDamage(damageValue);
                    ApplyDamage(finalDamage, DamageType.Physical, target);

                    if (hitCollider.TryGetComponent<Character>(out Character character))
                    {
                        var targetState = character.CharacterState;
                        if (targetState != null)
                        {
                            if (shotAstralManaActive && targetState.CheckForState(States.Astral)) RestoreMana();
                            if (targetState.CheckForState(States.Silent) && silenceTalentActive) CmdAddWeakeningSilence(targetState);
                            if (Random.Range(0f, 100f) <= stunChance) CmdAddState(targetState);
                        }
                    }
                }

                if (hitCollider.TryGetComponent<ReconnaissanceFireAura>(out ReconnaissanceFireAura aura) && tripleShotTalentActive)
                {
                    if (FindObjectOfType<ReconnaissanceFireArrowIntoSkyTalent>() != null && !_tripleShot)
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
        }
    }
    #endregion
}