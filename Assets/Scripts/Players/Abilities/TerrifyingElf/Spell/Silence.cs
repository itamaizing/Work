using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Silence : Skill
{
    [SerializeField] private GameObject _effectPrefab;
    [SerializeField] private bool _reducedCooldown;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private int _maxAdditionalManaUsage = 8;
    [SerializeField] private Ghost _ghost;
    [SerializeField] private float _damageMinoin = 60;
    [SerializeField] private SkillQueue _skillQueue;

    #region const
    private const float SilenceAreaRadiusOffset = 1.5f;
    private const float GhostCooldownPerMinion = 4f;
    private const float DurationPerMana = 0.5f;
    private const int MinManaReserve = 1;
    private const int ManaThreshold = 1;
    private const float GhostHealthCheckDelay = 0.1f;
    private const float BaseDarknessMultiplier = 1.4f;
    private const float StackMultiplierBonus = 0.1f;
    #endregion

    private AudioSource _audioSource;
    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private float _finalDuration;
    private WaitForSeconds _waitForGhostHealthCheckDelay;

    private bool _effectsDarknessTalent;
    private bool _canAttackMinions;
    private bool _isSilenceEffectsOnMinionMagic;
    private bool _isSilenceEffectGhostCast;
    private bool _isSilenceAddAllCharacterWithDeabaffElf;
    private bool _weakeningSilenceTalentActive;
    public bool IsSilenceAddAllCharacterWithDeabaffElf { get => _isSilenceAddAllCharacterWithDeabaffElf; }
    
    public void WeakeningSilenceTalentActive(bool value) => _weakeningSilenceTalentActive = value;

    private void OnEnable()
    {
        if (_skillQueue != null) _skillQueue.Cancell += HandleSkillDeleted;
    }

    private void OnDisable()
    {
        if (_skillQueue != null) _skillQueue.Cancell -= HandleSkillDeleted;
    }

    private void HandleSkillDeleted(Skill skill)
    {
        if (skill == this) Renderer.HideAOEIndicator(isCommand: false);
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

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellSilence");
    protected override int AnimTriggerCast => 0;
    private void Start()
    {
        _baseCooldownTime = CooldownTime;
        _audioSource = GetComponent<AudioSource>();
        _waitForGhostHealthCheckDelay = new WaitForSeconds(GhostHealthCheckDelay);
    }
    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _targetPoint = targetInfo.Points[0];
    }
    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x))
        {
            if (GetMouseButton)
            {
                targetPoint = Targeting.GetMousePoint();

                if (Targeting.IsPointInRadius(AreaInfo.Radius, targetPoint))
                {
                    Renderer.ShowAOEIndicator(targetPoint, isCommand: false);
                    break;
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
        if (_targetPoint == Vector3.positiveInfinity) yield return null;

        CalculateFinalDurationAndSpendMana();

        CmdSpawnEffectAtTargetPoint(_targetPoint);
        ApplyStateToEnemiesInZone(_targetPoint);
        Renderer.HideAOEIndicator(isCommand: false);
        yield return null;
    }

    private void ApplyStateToEnemiesInZone(Vector3 target)
    {
        Collider[] hitColliders = Physics.OverlapSphere(target, AreaInfo.Area - SilenceAreaRadiusOffset, Targeting.Layer);

        int minionHitCount = 0;
        int ghostAuraMinionHitCount = 0;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject != Hero.gameObject)
            {
                ApplyEnemiesZone(hitCollider, ref minionHitCount, ref ghostAuraMinionHitCount);
            }    
        }

        if (minionHitCount > 0 && _isSilenceEffectsOnMinionMagic) DecreaseSetCooldown(GhostCooldownPerMinion * minionHitCount);
        if (ghostAuraMinionHitCount >= 2 && _isSilenceEffectGhostCast) CmdTriggerGhostFreeWindow();
    }

    [Command]
    private void CalculateFinalDurationAndSpendMana()
    {
        _finalDuration = 0;

        var manaRes = Hero.TryGetResource(ResourceType.Mana);
        if (manaRes != null)
        {
            int availableMana = Mathf.Min((int)manaRes.CurrentValue - MinManaReserve, _maxAdditionalManaUsage);

            if (availableMana > ManaThreshold)
            {
                manaRes.TryUse(availableMana);
                _finalDuration += DurationPerMana * availableMana;
            }
        }
    }

    private void ApplyEnemiesZone(Collider hitCollider, ref int minionHitCount, ref int ghostAuraMinionHitCount)
    {
        if (hitCollider.TryGetComponent<HeroComponent>(out HeroComponent enemy))
        {
            var targetState = enemy.GetComponent<CharacterState>();
            if (targetState != null)
            {
                CmdApplySilenceState(targetState);
            }
        }

        if (hitCollider.TryGetComponent<MinionComponent>(out MinionComponent minion))
        {
            var targetState = minion.GetComponent<CharacterState>();

            if (targetState != null)
            {
                CmdApplySilenceState(targetState);
                minionHitCount++;
            }

            if (minion.TryGetComponent<GhostAura>(out GhostAura ghostAura)) ghostAuraMinionHitCount++;

            if (_canAttackMinions) MinionDamage(minion);
        }
    }

    private void MinionDamage(MinionComponent minion)
    {
        ApplyDamage(_damageMinoin, DamageType.Magical, minion);
        RewardMana();
    }

    private void RewardMana()
    {
        if (Hero.TryGetResource(ResourceType.Mana) is Mana manaResource)
        {
            manaResource.CmdAdd(_damageMinoin);
            Debug.Log("Restored mana for hitting a magical creature.");
        }
    }

    private void ApplyDamage(float damage, DamageType damageType, MinionComponent target)
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
            CmdReduceGhostCharge(target);
            StartCoroutine(IGhostHealthCheck(target));
        }
    }

    private IEnumerator IGhostHealthCheck(MinionComponent target)
    {
        yield return _waitForGhostHealthCheckDelay;
        if (target.TryGetComponent<GhostAura>(out var ghostAura))
        {
            if (ghostAura.TryGetComponent<Health>(out var health))
            {
                if (health.CurrentValue <= 0) _ghost.ResetCurrentChargeCooldown(0);
            }
        }

    }
    [Server] private void ServerGhostHealthCheck(MinionComponent target) => StartCoroutine(IGhostHealthCheck(target));

    #region Talents

    public void SetCanAttackMinions(bool value) => _canAttackMinions = value;
    public void SetReducedCooldown(bool value) => _reducedCooldown = value;
    public void EffectsInnerDarknessTalentActive(bool value) => _effectsDarknessTalent = value;
    public void SilenceEffectsOnMinionMagic(bool value) => _isSilenceEffectsOnMinionMagic = value;
    public void SilenceEffectGhostCast(bool value) => _isSilenceEffectGhostCast = value;
    public void SilenceAddAllCharacterWithDeabaffElf(bool value) => _isSilenceAddAllCharacterWithDeabaffElf = value;

    #endregion
    [Command] private void CmdTriggerGhostFreeWindow() => RpcTriggerGhostFreeWindow();
    [Command] private void CmdReduceGhostCharge(MinionComponent target) => ServerGhostHealthCheck(target);

    [Command]
    private void CmdSpawnEffectAtTargetPoint(Vector3 point)
    {
        RpcSpawnEffect(point);
        RpcSpawnEffect(point);
    }

    [ClientRpc]
    private void RpcSpawnEffect(Vector3 point)
    {
        if (_effectPrefab != null) Instantiate(_effectPrefab, point, Quaternion.identity);
    }

    [Command]
    private void CmdApplySilenceState(CharacterState targetState)
    {
        RpcPlayShotSound();

        float duration = _finalDuration;
        bool hasInnerDarkness = targetState.CheckForState(States.InnerDarkness);

        if (_effectsDarknessTalent && hasInnerDarkness)
        {
            int stacks = targetState.CheckStateStacks(States.InnerDarkness);
            float durationMultiplier = BaseDarknessMultiplier + StackMultiplierBonus * (stacks - 1);
            duration += durationMultiplier;
        }

        targetState.AddState(States.Silent, duration, 0, Hero.gameObject, this.name);

        if (_weakeningSilenceTalentActive && hasInnerDarkness) targetState.AddState(States.WeakeningSilence, 4f, 4f, Hero.gameObject, this.name);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (_audioSource != null && _audioClip != null) _audioSource.PlayOneShot(_audioClip);
    }

    [ClientRpc]
    private void RpcTriggerGhostFreeWindow()
    {
        if (_ghost != null) _ghost.TryStartGhostBoostWindow();
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }
}
