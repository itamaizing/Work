using Mirror;
using System.Linq;
using UnityEngine;

public class ArrowProjectile : Projectiles
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _lifeTime = 5f;
    [SerializeField] private bool _arrowDark;
    [SerializeField] private float duration;
    [SerializeField] private DamageType damageTypePhysics;

    private float magDamage;
    private float _damage;

    public bool ArrowDark { get => _arrowDark; set => _arrowDark = value; }

    public void StartFly(Vector3 direction)
    {
        if (_rb != null) _rb.velocity = direction * _speed;

        Destroy(gameObject, _lifeTime);
    }

    public void Init(HeroComponent dad, float energy, bool lastHit, Skill skill, float damage)
    {
        base.Init(dad, energy, lastHit, skill);
        _damage = damage;
    }

    [Server]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == _dad?.gameObject) return;
        if (!other.TryGetComponent<IDamageable>(out _)) return;

        if (((1 << other.gameObject.layer) & _skill.TargetsLayers.value) == 0) return;

        if (other.TryGetComponent<ObjectHealth>(out ObjectHealth objectHealth) &&
            objectHealth.ResistMagicDamage >= 100 && _arrowDark)
            return;

        ApplyEnemy(other);
        Destroy(gameObject);
    }


    //private void TargetApply(Collider other)
    //{
    //    //if (other.TryGetComponent<IDamageable>(out IDamageable target))
    //    //{
    //    //    //if (other.TryGetComponent<UserNetworkSettings>(out UserNetworkSettings userNetworkSettings))
    //    //    //{
    //    //    //    if (userNetworkSettings.TeamIndex != _dad.NetworkSettings.TeamIndex)
    //    //    //    {
    //    //    //        ApplyEnemy(other, target);
    //    //    //    }
    //    //    //}

    //    //    if (other.gameObject != _dad.gameObject && ((1 << other.gameObject.layer) & _skill.TargetsLayers.value) != 0) ApplyEnemy(other);
    //    //}
    //}

    #region ApplyEnemy
    private void ApplyEnemy(Collider collider)
    {
        bool inAstral = _dad != null && _dad.CharacterState.CheckForState(States.Astral);

        if (_arrowDark)
        {
            if (!inAstral)
            {
                ApplyDamage(_damage, damageTypePhysics, collider.gameObject);
                if (TryApplyDamage(damageTypePhysics, _skill.AttackRangeType, collider.gameObject)) return;
            }

            float availableMana = 0f;
            if (_dad != null) availableMana = _dad.Resources.Where(resourse => resourse.Type == ResourceType.Mana).Sum(resourse => resourse.CurrentValue);

            float bonusMagDamage = Mathf.Min(6f, Mathf.Floor(availableMana));
            float totalMagDamage = magDamage + bonusMagDamage;

            if (inAstral) totalMagDamage *= 1.5f;

            ApplyDamage(totalMagDamage, _skill.DamageType, collider.gameObject);

            if (_dad != null && bonusMagDamage > 0)
            {
                float manaToUse = bonusMagDamage;
                foreach (var manaResource in _dad.Resources.Where(resourse => resourse.Type == ResourceType.Mana))
                {
                    if (manaToUse <= 0) break;

                    float amountToUse = Mathf.Min(manaResource.CurrentValue, manaToUse);
                    if (isServer) manaResource.CurrentValue -= amountToUse;
                    manaToUse -= amountToUse;
                }
            }

            if (collider.TryGetComponent<Character>(out Character character)) character.CharacterState.AddState(States.InnerDarkness, duration, 0, _skill.Hero.gameObject, _skill.name);
        }

        else ApplyDamage(_damage, damageTypePhysics, collider.gameObject);
    }
    #endregion

    private void ApplyDamage(float value, DamageType type, GameObject target)
    {
        var damage = new Damage { Value = value, Type = type };
        _skill.ApplyDamage(damage, target);
    }

    private bool TryApplyDamage(DamageType damageType, AttackRangeType attackRangeType, GameObject target)
    {
        if (target.TryGetComponent<Health>(out Health health)) return health.TryEvade(damageType, attackRangeType);

        return false;
    }
}
