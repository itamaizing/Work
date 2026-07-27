using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowsIntoSkyProjectile : NetworkBehaviour
{
    [SerializeField] private float impactLifeTime = 2;
    [SerializeField] private float nextDamageTime = 1;

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

    private readonly HashSet<Collider> _damagedThisTick = new();

    public GameObject Arrow { get => arrow; set => arrow = value; }
    public GameObject Circle { get => circle; set => circle = value; }

    public virtual void Init(HeroComponent dad, Skill skill, float damage, bool lastStreamTalent, bool shotMagicDebuffActive)
    {
        this.lastStreamTalent = lastStreamTalent;
        this.shotMagicDebuffActive = shotMagicDebuffActive;

        _dad = dad;
        _skill = skill;
        _damage = damage;

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

    private void OnTriggerStay(Collider other)
    {
        if (!isOwned) return; 
        if (!_damagedThisTick.Add(other)) return;

        if (other.gameObject.TryGetComponent<Character>(out var victim))
        {
            ApplyDamage(_damage, DamageType.Magical, victim);
            ApplyStatesAndTalents(victim);
            return;
        }

        
        if (other.gameObject.TryGetComponent<IDamageable>(out var dmgTarget))
            ApplyDamage(_damage, DamageType.Magical, dmgTarget);
    }

    private void ApplyStatesAndTalents(Character character)
    {
        CharacterState characterState = character.CharacterState;
        if (characterState == null) return;

        if (lastStreamTalent && !IsAlly(character)) characterState.CmdAddState(States.InnerDarkness, 13, 0, _character.gameObject, name);

        if (shotMagicDebuffActive)
        {
            CmdRefreshExistingMagicStates(characterState, character);
        }
    }
    
    [Command]
    private void CmdRefreshExistingMagicStates(CharacterState targetState, Character target)
    {
        if (_character == null) return;

        bool isAlly = IsAlly(target);
        BaffDebaff wanted = isAlly ? BaffDebaff.Baff : BaffDebaff.Debaff;
        var statesCopy = new List<AbstractCharacterState>(targetState.CurrentStates);

        foreach (var state in statesCopy)
        {
            if (state.Type != StateType.Magic) continue;
            if (state.BaffDebaff != wanted) continue;
            if (state.BaseDurationValue < 0f) continue;

            targetState.AddState(state.State,state.BaseDurationValue,0,state.PersonWhoMadeBuff.gameObject,name);
        }
    }

    private bool IsAlly(Character target)
    {
        if (target == null) return false;
        if (target == _character) return true;

        if (target.TryGetComponent<UserNetworkSettings>(out var targetSettings) &&
            _character.TryGetComponent<UserNetworkSettings>(out var casterSettings) &&
            targetSettings.TeamIndex != 0 && casterSettings.TeamIndex != 0)
        {
            return targetSettings.TeamIndex == casterSettings.TeamIndex;
        }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        return target.gameObject.layer != enemyLayer;
    }

    private void ApplyDamage(float damage, DamageType damageType, IDamageable target)
    {
        if (target.gameObject.TryGetComponent<Character>(out var victim))
        {
            if(IsAlly(victim))
                return;
        }
        
        Damage _damage = new Damage
        {
            Value = damage,
            Type = damageType,
            PhysicAttackType = AttackRangeType.RangeAttack,
        };

        if (target is Component targetComponent)
        {
            _skill.CmdApplyDamage(_damage, targetComponent.gameObject);
        }
    }
}