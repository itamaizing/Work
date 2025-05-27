using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class ShotIntoSky : Skill
{
    [Header("ShotIntoSky Settings")]
    [SerializeField] private SkillRenderer skillRenderer;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField] private bool silenceTalentActive;
    [SerializeField] private bool tripleShotTalentActive;
    [SerializeField] private bool shotAstralManaActive;
    [SerializeField, Range(0f, 100f)] private float criticalChance = 30f;

    [Header("Arrow Effects Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField, Min(0.01f)] private float fallSpeed = 10f;
    [SerializeField] private float spawnHeight = 10f;

    private GameObject _spawnedArrow;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private bool _tripleShot;
    private const float CriticalMultiplier = 2.4f;

    private const string _endAnimTrigger = "ShotSkyCastDelayEnd";

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotSkyCastDelay");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        Damage = UnityEngine.Random.Range(minDamage, maxDamage + 1);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        _hero.Animator.speed = Hero.Animator.speed / CastDeley;
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

        Hero.Move.LookAtPosition(_targetPoint);
        Hero.Move.CanMove = false;

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_targetPoint);
        callbackDataSaved(targetInfo);
        DrawDamageZone(_targetPoint);
    }

    protected override IEnumerator CastJob()
    {
        Damage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        CmdSpawnArrow(_targetPoint, spawnHeight);
        float fallTime = spawnHeight / fallSpeed;
        yield return new WaitForSeconds(fallTime);

        ApplyDamageToEnemiesInZone();

        CmdDestroyArrow();

        _hero.Animator.speed = 1f;
        _hero.Animator.SetTrigger(Animator.StringToHash(_endAnimTrigger));
        _hero.NetworkAnimator.SetTrigger(Animator.StringToHash(_endAnimTrigger));

        _hero.Animator.ResetTrigger(_endAnimTrigger);

        ClearData();
    }

    private void SetInitialVelocity(GameObject arrow, Vector3 targetPoint, float speed)
    {
        if (arrow.TryGetComponent<Rigidbody>(out var rb))
        {
            Vector3 dir = (targetPoint - arrow.transform.position).normalized;
            rb.velocity = dir * speed;
        }
    }

    #region Arrow Spawn/Destroy (Networking)

    [Command]
    private void CmdSpawnArrow(Vector3 targetPoint, float height)
    {
        Vector3 pos = targetPoint + Vector3.up * height;
        GameObject arrow = Instantiate(arrowPrefab, pos, Quaternion.identity);

        SetInitialVelocity(arrow, targetPoint, fallSpeed);

        NetworkServer.Spawn(arrow, connectionToClient);
        RpcSetupArrow(arrow, targetPoint, height, fallSpeed);
        _spawnedArrow = arrow;
    }

    [Command]
    private void CmdDestroyArrow()
    {
        if (_spawnedArrow != null)
        {
            NetworkServer.Destroy(_spawnedArrow);
            _spawnedArrow = null;
        }
        RpcDestroyArrow();
    }

    [ClientRpc]
    private void RpcSetupArrow(GameObject arrow, Vector3 targetPoint, float height, float speed)
    {
        if (arrow == null) return;

        SetInitialVelocity(arrow, targetPoint, speed);
        StartCoroutine(ArrowFallRoutine(arrow, targetPoint, height, speed));
    }

    [ClientRpc]
    private void RpcDestroyArrow()
    {
        if (_spawnedArrow != null)
        {
            Destroy(_spawnedArrow);
            _spawnedArrow = null;
        }
    }

    private IEnumerator ArrowFallRoutine(GameObject arrow, Vector3 targetPoint, float height, float speed)
    {
        Vector3 start = targetPoint + Vector3.up * height;
        Vector3 end = targetPoint;
        float time = 0f;
        float fallTime = height / speed;

        while (time < fallTime)
        {
            if (arrow == null) yield break;
            float ratio = time / fallTime;
            arrow.transform.position = Vector3.Lerp(start, end, ratio);
            time += Time.deltaTime;
            yield return null;
        }

        if (arrow != null) arrow.transform.position = end;
    }

    #endregion

    #region Damage Logic

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

    private float CalculateDamage(float baseDamage)
    {
        bool isCriticalHit = UnityEngine.Random.Range(0f, 100f) <= criticalChance;
        return isCriticalHit ? baseDamage * CriticalMultiplier : baseDamage;
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
    private void CmdAddWeakeningSilence(CharacterState targetState)
    {
        targetState.AddState(States.WeakeningSilence, 4, 4, Hero.gameObject, this.name);
    }

    #endregion

    #region Misc/Utility

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
        Hero.Move.CanMove = true;
        Hero.Move.StopLookAt();
    }

    public void SetSilenceTalentActive(bool value) => silenceTalentActive = value;
    public void SetTripleShotTalentActive(bool value) => tripleShotTalentActive = value;

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
                        }
                    }
                }

                if (hitCollider.TryGetComponent<ReconnaissanceFireAura>(out ReconnaissanceFireAura aura) && tripleShotTalentActive)
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

    public void ShotsIntoSkyAstralTalentActive(bool value) => shotAstralManaActive = value;

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
