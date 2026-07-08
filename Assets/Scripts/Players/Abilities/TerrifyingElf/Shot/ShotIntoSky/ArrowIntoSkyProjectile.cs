using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowIntoSkyProjectile : NetworkBehaviour
{
    [SerializeField] private float impactLifeTime = 1;
    [SerializeField] private float nextDamageTime = 0.5f;
    [SerializeField] private float minDamage;
    [SerializeField] private float maxDamage;
    [SerializeField, Range(0f, 100f)] private float criticalChance = 30f;
    [SerializeField] private float criticalMultiplier = 2.4f;

    [SerializeField] private GameObject arrow;
    [SerializeField] private GameObject circle;
    [SerializeField] private SphereCollider sphereCollider;

    [SerializeField] private bool lastStreamTalent;
    [SerializeField] private bool shotMagicDebuffActive;

    [SerializeField] private bool isDamage;

    private HeroComponent _dad;
    private Skill _skill;
    private Character _character;
    private float _damage;

    private bool _isElvenSkillCrit;

    private readonly HashSet<Collider> _damagedThisTick = new();

    public GameObject Arrow { get => arrow; set => arrow = value; }
    public GameObject Circle { get => circle; set => circle = value; }

    public virtual void Init(HeroComponent dad, Skill skill, float damage, bool lastStreamTalent, bool shotMagicDebuffActive, bool isElvenSkillCrit)
    {
        this.lastStreamTalent = lastStreamTalent;
        this.shotMagicDebuffActive = shotMagicDebuffActive;
        _dad = dad;
        _skill = skill;
        _damage = damage;
        _isElvenSkillCrit = isElvenSkillCrit;

        if (_dad != null && _dad.TryGetComponent<Character>(out Character character)) _character = character;
    }

    public void Activate()
    {
        Arrow.SetActive(true);
        circle.SetActive(true);
        Invoke("ActiveCollider", nextDamageTime);       
        Destroy(gameObject, impactLifeTime);
    }

    private void ActiveCollider() => sphereCollider.enabled = true;

    [Server]
    private void OnTriggerStay(Collider other)
    {
        if (((1 << other.gameObject.layer) & _skill.Targeting.Layer.value) == 0) return;
        if (!_damagedThisTick.Add(other)) return;

        ApplyDamageEnemy(other);
    }

    #region ApplyAdditionalDamage
    //private void ApplyAdditionalDamage(float damageValue)
    //{
    //    foreach (var enemyCollider in enemyColliders)
    //    {
    //        if (enemyCollider.TryGetComponent<IDamageable>(out IDamageable target) && enemyCollider != _character.gameObject)
    //        {
    //            ApplyDamage(damageValue, Info.DamageType.Magical, target);

    //            if (enemyCollider.TryGetComponent<Character>(out Character character))
    //            {
    //                var targetState = character.CharacterState;

    //                if (targetState != null)
    //                {
    //                    targetState.AddState(States.Irradiation, 9, 0, _character.gameObject, this.name);

    //                    if (shotAstralManaActive && targetState.CheckForState(States.Astral)) RestoreMana();
    //                    if (targetState.CheckForState(States.Silent) && silenceTalentActive) targetState.AddState(States.WeakeningSilence, 4, 4, _character.gameObject, this.name);
    //                }
    //            }
    //        }
    //    }

    //    //foreach (var objectCollider in objectColliders)
    //    //{
    //    //    if (objectCollider.TryGetComponent<ReconnaissanceFireAura>(out ReconnaissanceFireAura aura) && tripleShotTalentActive)
    //    //        if (FindObjectOfType<NatureTalent_6>() != null && !_tripleShot) StartCoroutine(SpawnAdditionalDamageZones(aura));
    //    //}
    //}
    #endregion

    #region ApplyDamageToEnemiesInZone
    //private void ApplyDamageToEnemiesInZone(Collider collider)
    //{
    //    foreach (var enemyCollider in enemyColliders)
    //    {
    //        if (enemyCollider.TryGetComponent<IDamageable>(out IDamageable target) && enemyCollider != _character.gameObject)
    //        {
    //            ApplyDamage(_damage, Info.DamageType.Magical, target);

    //            if (enemyCollider.TryGetComponent<Character>(out Character character))
    //            {
    //                var targetState = character.CharacterState;

    //                if (targetState != null)
    //                {
    //                    targetState.AddState(States.Irradiation, 9, 0, _character.gameObject, this.name);

    //                    if (shotAstralManaActive && targetState.CheckForState(States.Astral)) RestoreMana();

    //                    if (targetState.CheckForState(States.Silent) && silenceTalentActive) targetState.AddState(States.WeakeningSilence, 4, 4, _character.gameObject, this.name);
    //                }
    //            }
    //        }
    //    }

    //    //foreach (var objectCollider in objectColliders)
    //    //{
    //    //    if (objectCollider.TryGetComponent<ReconnaissanceFireAura>(out ReconnaissanceFireAura aura) && tripleShotTalentActive)
    //    //    {
    //    //        if (FindObjectOfType<NatureTalent_6>() != null && !_tripleShot)
    //    //        {
    //    //            _tripleShot = true;
    //    //            StartCoroutine(SpawnAdditionalDamageZones(aura));
    //    //        }
    //    //    }
    //    //}

    //    //if (!_tripleShot) HideAOEIndicator();
    //}
    #endregion

    private void ApplyDamageEnemy(Collider other)
    {
        if (!other.TryGetComponent<IDamageable>(out var damageTarget)) return;

        float damageToDeal = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        float distanceMultiplier = 1f;
        if (_character != null)
        {
            float distance = Vector3.Distance(_character.transform.position, other.transform.position);
            distanceMultiplier = 1f + (distance * 0.05f);
        }
        damageToDeal *= distanceMultiplier;

        if (Random.value * 100f < criticalChance)
        {
            float critMultiplier = Random.Range(1.6f, 2.4f);
            damageToDeal *= critMultiplier;
        }

        if (other.TryGetComponent<Character>(out var targetCharacter)) 
        {
            damageToDeal = ApplyElvenCritModifier(damageToDeal, targetCharacter);
        }

        ApplyDamage(damageToDeal, DamageType.Physical, damageTarget);

        _skill.Damage = damageToDeal;
    }

    private void ApplyStatesAndTalents(Character character)
    {
        CharacterState characterState = character.CharacterState;
        if (characterState == null) return;

        //characterState.AddState(States.Irradiation, 9, 0, _character.gameObject, name);

        if (lastStreamTalent) characterState.AddState(States.InnerDarkness, 13, 0, _character.gameObject, name);
        if (shotMagicDebuffActive && characterState.HasMagicDebuff()) RestoreMana();

        //if (characterState.CheckForState(States.Silent)) characterState.AddState(States.WeakeningSilence, 4, 4, _character.gameObject, name);
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
            _skill.ApplyDamage(_damage, targetComponent.gameObject);
            //CmdApplyDamage(targetComponent.gameObject, _damage, null);
        }
    }

    private float ApplyElvenCritModifier(float damage, Character target)
    {
        if (!_isElvenSkillCrit) return damage;
        if (_dad == null || target == null) return damage;

        if (!_dad.CharacterState.CheckForState(States.ElvenSkill))
            return damage;

        if (target.Health == null) return damage;

        float hpPercent = target.Health.CurrentValue / target.Health.MaxValue;
        if (hpPercent <= 0.7f) return damage;

        damage *= 1.3f;

        if (UnityEngine.Random.Range(0f, 100f) <= 30f)
        {
            float critMultiplier = UnityEngine.Random.Range(2.4f, 3.2f);
            damage *= critMultiplier;

            Debug.Log($"[ElvenCrit AoE] CRIT x{critMultiplier}");
        }

        return damage;
    }

    private void RestoreMana()
    {
        if (_character.TryGetResource(ResourceType.Mana) is Mana manaResource)
        {
            float manaToRestore = manaResource.MaxValue * 0.03f;
            manaResource.Add(manaToRestore);
            _character.CharacterState.AddState(States.ManaRegen, 1, 0, _character.gameObject, this.name);
        }
    }

    //private IEnumerator SpawnAdditionalDamageZones(ReconnaissanceFireAura aura)
    //{
    //    yield return new WaitForSeconds(1f);
    //    ApplyAdditionalDamage(Damage / 2);

    //    if (aura.StateDark)
    //    {
    //        yield return new WaitForSeconds(1f);
    //        ApplyAdditionalDamage(Damage / 4);
    //        _tripleShot = false;
    //        HideAOEIndicator();
    //        yield break;
    //    }

    //    _tripleShot = false;
    //    HideAOEIndicator();
    //    yield break;
    //}
}
