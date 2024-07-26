using UnityEngine;
public abstract class AbilityData : ScriptableObject
{
  [SerializeField] private EnergyCost _energyTypes;
  [SerializeField] private Sprite _icon;
  [SerializeField] private string _title;
  [SerializeField] private string _description;
  [SerializeField] private Charge _charges;
  [SerializeField] private float _castTime;
  [SerializeField] private float _cooldown;
  [SerializeField] private float _radius;
  [SerializeField] private bool _isTargetSpell;
  [SerializeField] private Schools _abilitySchool;
  [SerializeField] private AbilityForm _abilityForm;
  [SerializeField] private LayerMask _targetUnits;
  
    public EnergyCost EnergyTypes => _energyTypes;
    public Sprite CharacterIcon => _icon;
    public string Title => _title;
    public string Description => _description;
    public Charge Charges => _charges;
    public float CastTime => _castTime;
    public float Cooldown => _cooldown;
    public float Radius => _radius;
    public Schools School => _abilitySchool;
    public AbilityForm Form => _abilityForm;
    public abstract float MainValue { get; }
    public abstract AbilityDataType AbilityType{ get;}
    public LayerMask TargetUnits => _targetUnits;
    
    public bool IsTargetSpell => TargetUnits> 0;
    public bool HaveCharges => _charges != null;
}

public enum AbilityDataType
{
    Damage,
    Heal,
    Movement,
    Summon,
    Shield
}
public enum StaminaType
{
    Mana,
    Energy,
    Rune
}

public enum Schools
{
    None,
    Light,
    Dark,
    Fire,
    Water,
    Air,
    Earth,
    Physical
}

public enum AbilityForm
{
    Spell,
    Magic,
    Physical
}


[System.Serializable]
public class EnergyCost
{
    public StaminaType energyType;
    public float costValue;
}

[System.Serializable]
public class Charge
{
    public int ChargesCount;
    public float ChargeCooldown;
    public bool HaveSeparateCooldown;
}