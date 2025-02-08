using Mirror;
using System.Collections;
using UnityEngine;

public class Silence : Skill
{
    [SerializeField] private float _duration;
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private bool _canAttackMinions;
    [SerializeField] private bool _reducedCooldown;
    [SerializeField] private AudioClip audioClip;

    private AudioSource audioSource;
    private Vector3 _targetPoint = Vector3.positiveInfinity;

    protected override bool IsCanCast => !_disactive && IsPointInRadius(Radius, _targetPoint);

    protected override int AnimTriggerCastDelay => Animator.StringToHash("SpellCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;

    private void Start()
    {
        _baseCooldownTime = CooldownTime;
        audioSource = GetComponent<AudioSource>();
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_targetPoint.x) && !_disactive)
        {
            if (GetMouseButton)
            {
                Vector3 clickedPoint = GetMousePoint();

                if (IsPointInRadius(Radius, clickedPoint))
                {
                    _targetPoint = clickedPoint;

                    Collider[] hitColliders = Physics.OverlapSphere(_targetPoint, Area, TargetsLayers);
                    int minionCount = 0;

                    foreach (var hitCollider in hitColliders) 
                        if (hitCollider.TryGetComponent<MinionComponent>(out _)) minionCount++;

                    if (minionCount > 0 && _reducedCooldown) _cooldownTime = _cooldownTime - minionCount;

                    yield break;
                }
            }
            yield return null;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_targetPoint != Vector3.positiveInfinity)
        {
            yield return new WaitForSeconds(0.1f);

            SpawnEffectAtTargetPoint();
            ApplyStateToEnemiesInZone();
        }
    }

    private void SpawnEffectAtTargetPoint()
    {
        if (effectPrefab != null) Instantiate(effectPrefab, _targetPoint, Quaternion.identity);
    }

    private void ApplyStateToEnemiesInZone()
    {
        Collider[] hitColliders = Physics.OverlapSphere(_targetPoint, Area, TargetsLayers);

        int minionHitCount = 0;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject != Hero.gameObject)
                ApplyEnemiesZone(hitCollider, ref minionHitCount);
        }
    }

    private void ApplyEnemiesZone(Collider hitCollider, ref int minionHitCount)
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

            if (_canAttackMinions) MinionDamage(minion);
        }
    }

    private void MinionDamage(MinionComponent minion)
    {
        ApplyDamage(Damage, DamageType.Magical, minion);
        RewardMana();
    }

    private void RewardMana()
    {
        if (Hero.TryGetResource(ResourceType.Mana) is Mana manaResource)
        {
            manaResource.CmdAdd(Damage);
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
            //CmdApplyDamage(targetComponent.gameObject, _damage, null);
            CmdApplyDamage(_damage, targetComponent.gameObject);
        }
    }

    public void SetCanAttackMinions(bool value)
    {
        _canAttackMinions = value;
    }

    public void SetReducedCooldown(bool value)
    {
        _reducedCooldown = value;
    }

    //[Command]
    //private void CmdApplyDamage(GameObject targetObject, Damage damage, Skill skill)
    //{
    //    if (targetObject != null && targetObject.TryGetComponent<IDamageable>(out IDamageable target))
    //    {
    //        target.TryTakeDamage(ref damage, skill);
    //    }
    //}

    [Command]
    private void CmdApplySilenceState(CharacterState targetState)
    {
        float adjustedDuration = _duration;
        RpcPlayShotSound();

        if (targetState.CheckForState(States.InnerDarkness))
        {
            int innerDarknessStacks = targetState.CheckStateStacks(States.InnerDarkness);

            float durationMultiplier = 1 + 0.4f * innerDarknessStacks;
            adjustedDuration *= durationMultiplier;
        }

        targetState.AddState(States.Silent, adjustedDuration, 0, Hero.gameObject, this.name);
    }

    [ClientRpc]
    private void RpcPlayShotSound()
    {
        if (audioSource != null && audioClip != null) audioSource.PlayOneShot(audioClip);
    }

    protected override void ClearData()
    {
        _targetPoint = Vector3.positiveInfinity;
    }
}
