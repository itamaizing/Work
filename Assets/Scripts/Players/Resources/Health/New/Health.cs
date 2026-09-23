using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Health : Resource, IDamageable, IHealable
{
    [SerializeField] private Animator _animator;
    [SerializeField] private NetworkAnimator _netAnimator;
    [SerializeField] private Bar bar;

    private List<IDamageable> _shields = new List<IDamageable>();
    [SyncVar] private float _sumDamageTaken = 0;
    private float _totalMaxAbsorption = 0;
    private bool _isDot = false;

    public Bar barCharacter { get => bar; }
    //public float SumDamageTaken { get { Debug.Log("Sum dmg " + _sumDamageTaken); return _sumDamageTaken; }} //=> _sumDamageTaken; }
    public float SumDamageTaken { get => _sumDamageTaken; }
    public float TotalMaxAbsorption { get => _totalMaxAbsorption; set => _totalMaxAbsorption = value; }
    public List<IDamageable> Shields { get => _shields; }
    
    private Character _character;
    private Character CharacterRef => _character ??= GetComponent<Character>();

    private float ResistancePhysical => CharacterRef?.AttributeSystem[CharacterAttributeName.ResistancePhysical]?.GetValue() ?? 0f;
    private float ResistanceMagical => CharacterRef?.AttributeSystem[CharacterAttributeName.ResistanceMagical]?.GetValue() ?? 0f;
    private float EvasionMelee => CharacterRef?.AttributeSystem[CharacterAttributeName.EvasionPhysicalMelee]?.GetValue() ?? 0f;
    private float EvasionRange => CharacterRef?.AttributeSystem[CharacterAttributeName.EvasionPhysicalRange]?.GetValue() ?? 0f;
    private float EvasionMagical => CharacterRef?.AttributeSystem[CharacterAttributeName.EvasionMagical]?.GetValue() ?? 0f;
    
    public event Action<Skill> Evaded;
    public event Action Block;
    public event Action<float , Skill , string> HealTaked;
    public event Action<float, Skill, string> HealTakedServer;
    public event Action<Damage, Skill> DamageTaken;
    public event Action Died;
    public event Action<float, float> OnShieldValuesChanged;
    public event Action<float> OnShieldAdd;
    public event Action ShieldDeactivated;

    public event Action<float, DamageType, Skill> ShieldDamageTaken;
    public event Action<Damage, Skill> OnBeforeTakeDamage; //Test
    
    public delegate bool TryResistDelegate(Damage damage, Skill skill);
    public event TryResistDelegate OnTryResist;

    public delegate void BeforeDamageDelegate(ref Damage damage, Skill skill);
    public event BeforeDamageDelegate OnBeforeDamage;
    public delegate void BeforeHealDelegate(ref Heal heal, Skill skill);
    public event BeforeHealDelegate OnBeforeHeal;

    public bool IsDot { get => _isDot; set => _isDot = value; }

    /* public override void Initialize(float health, float hpRegen, float hpRegenDelay, CharacterData data, Attribute attribute)
     {
         base.Initialize(health, hpRegen, hpRegenDelay, data, attribute);

         _defPhysDamage = data.GetAttributeValue(AttributeNames_old.PhysicResist);
         _defMagDamage = data.GetAttributeValue(AttributeNames_old.MagicResist);
         _resistMagDamage = data.GetAttributeValue(AttributeNames_old.MagicEvade);
         _evadeMeleeDamage = data.GetAttributeValue(AttributeNames_old.MeleeEvade);
         _evadeRangeDamage = data.GetAttributeValue(AttributeNames_old.RangeEvade);
     }*/
    public override void Initialize(Attribute maxValue, Attribute regenValue, CharacterData data)
    {
        //Debug.Log("Init hp " + maxValue.GetValue());

        base.Initialize(maxValue, regenValue, data);

        //_defPhysDamage = data.GetAttributeValue(AttributeNames_old.PhysicResist);
        //_defMagDamage = data.GetAttributeValue(AttributeNames_old.MagicResist);
        //_resistMagDamage = data.GetAttributeValue(AttributeNames_old.MagicEvade);
        //_evadeMeleeDamage = data.GetAttributeValue(AttributeNames_old.MeleeEvade);
        //_evadeRangeDamage = data.GetAttributeValue(AttributeNames_old.RangeEvade);
    }

    public bool TryTakeDamage(ref Damage damage, Skill skill)
    {
        OnBeforeTakeDamage?.Invoke(damage, skill);
        OnBeforeDamage?.Invoke(ref damage, skill);

        if (TryEvade(damage.Type, damage.PhysicAttackType))
        {
            Evaded?.Invoke(skill);
            ClientRpcEvade();
            return false;
        }

        if (OnTryResist != null)
        {
            foreach (TryResistDelegate resist in OnTryResist.GetInvocationList())
            {
                if (resist.Invoke(damage, skill))
                    return false;
            }
        }

        Defence(ref damage);

        if (skill != null && skill.Hero != null)
        {
            foreach (var state in skill.Hero.CharacterState.CurrentStates)
                if (state is IDamageGivenModifier modifier) damage.Value = modifier.ModifyOutgoingDamage(damage);

            foreach (var ability in skill.Hero.Abilities.Abilities)
                if (ability is IDamageGivenModifier modifier) damage.Value = modifier.ModifyOutgoingDamage(damage);
        }

        float preShieldValue = damage.Value;
        UseShields(ref damage, skill);
        damage.FullyAbsorbed = damage.Value == 0 && preShieldValue > 0;

        ClientRpcDamage(damage, skill);
        _sumDamageTaken += damage.Value;

        if (damage.Value == 0)
            return true;

        if (!TryUse(damage.Value))
        {
            if (isServer)
            {
                Died?.Invoke();
                ClientRpcDied();
            }
            return true;
        }

        return true;
    }

    [Command(requiresAuthority = false)]
    public void CmdTryTakeDamage(Damage damage, GameObject skillCanBeNull)
    {
        TryTakeDamage(ref damage, null);
    }

    public void Heal(ref Heal heal, string sourceName, Skill skill = null)
    {
        OnBeforeHeal?.Invoke(ref heal,skill);
        heal.Value = ApplyIncomingModifiers(heal.Value);
        HealTakedServer?.Invoke(heal.Value, skill, sourceName);
        ClientRpcHealTaked(heal.Value, skill, sourceName);
        Add(heal.Value);
    }

    public void InvokeEvade(Skill skill = null) => Evaded?.Invoke(skill);

    public void InvokeBlock() => Block?.Invoke();

    public bool TryEvade(DamageType damageType, AttackRangeType attackRangeType)
    {
        switch (damageType)
        {
            case DamageType.Magical:
                return UnityEngine.Random.Range(0, 100) <= EvasionMagical;

            case DamageType.Physical:
                return attackRangeType switch
                {
                    AttackRangeType.MeleeAttack => UnityEngine.Random.Range(0, 100) <= EvasionMelee,
                    AttackRangeType.RangeAttack => UnityEngine.Random.Range(0, 100) <= EvasionRange,
                    _ => false
                };

            default:
                return false;
        }
    }

    protected void UseShields(ref Damage damage, Skill skill)
    {
        if (_shields.Count == 0) return;
        
        for (int i = _shields.Count - 1; i >= 0; i--)
        {
            var shield = _shields[i];

            if (shield == null)
            {
                _shields.RemoveAt(i);
                continue;
            }
            shield.TryTakeDamage(ref damage, skill);

            if (damage.Value == 0)
                break;
        }

        _shields.RemoveAll(shield => shield == null);
    }

    private void Defence(ref Damage damage)
    {
        if (damage.Type == DamageType.Physical)
            damage.Value *= 1 - (ResistancePhysical / 100.0f);
        else if (damage.Type == DamageType.Magical)
            damage.Value *= 1 - (ResistanceMagical / 100.0f);
    }

    public void UpdateShieldValues(float absorbed, float maxAbsorption)
    {
        if (isServer)
            ClientRpcUpdateShieldValues(absorbed, maxAbsorption);
    }

    public void AddShieldValues(float maxAbsorption)
    {
        if (isServer)
            ClientRpcAddShieldValues(maxAbsorption);
    }

    public void ResetShieldValues()
    {
        ShieldDeactivated?.Invoke();
    }

    [ClientRpc]
    public void ClientRpcInvokeShieldDamageTaken(float value, DamageType damageType, Skill skill)
    {
        ShieldDamageTaken?.Invoke(value, damageType, skill);
    }

    [ClientRpc]
    public void ClientRpcUpdateShieldValues(float absorbed, float maxAbsorption)
    {
        OnShieldValuesChanged?.Invoke(absorbed, maxAbsorption);
    }

    [ClientRpc]
    public void ClientRpcAddShieldValues(float maxAbsorption)
    {
        OnShieldAdd?.Invoke(maxAbsorption);
    }

    [ClientRpc]
    private void ClientRpcDamage(Damage damage, Skill skill)
    {
        DamageTaken?.Invoke(damage, skill);
        _animator.SetTrigger(HashAnimPlayer.TakeDamage);
    }


    [ClientRpc]
    private void ClientRpcEvade() => _animator.SetTrigger(HashAnimPlayer.Evade);

    [ClientRpc]
    private void ClientRpcHealTaked(float healTaken, Skill skill, string sourceName)
    {
        HealTaked?.Invoke(healTaken, skill, sourceName);
    }

    [ClientRpc]
    private void ClientRpcDied()
    {
        //Died?.Invoke();
        _animator.SetBool(HashAnimPlayer.IsDead, true);
    }

    public void ShowPhantomValue(Damage phantomValue)
    {
        float curDamage = phantomValue.Value;
        if (phantomValue.Type == DamageType.Physical)
            curDamage *= 1 - (ResistancePhysical / 100.0f);
        if (phantomValue.Type == DamageType.Magical)
            curDamage *= 1 - (ResistanceMagical / 100.0f);

        PhantomValueShow(curDamage);
    }

    public void IncreaseRegen(float percentValue)
    {
        _regenerationValue *= percentValue;
    }

    public void DecreaseRegen(float  percentageValue) 
    {
        _regenerationValue /= percentageValue;
    }
}