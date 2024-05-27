using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/Player", order = 1)]
public class PlayerInfo : ScriptableObject
{
    [SerializeField] private string _name;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private float _iconSize;
    [SerializeField] private float _health;
    [SerializeField] private HealthInfo _healthInfo;
    [SerializeField] private float _stamina;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _defaultHealthRegen;
    [SerializeField] private float _defaultStaminaRegen;
    [SerializeField] private float _defaultRegenDelay;

    public string Name => _name;
    public string Description => _description;
    public Sprite Icon => _icon;
    public float IconSize => _iconSize;

    public float Health => _health;

    public HealthInfo HealthInfo => _healthInfo;
    public float Stamina => _stamina;
    public float MoveSpeed => _moveSpeed;
    public float HealthRegen => _defaultHealthRegen;
    public float StaminaRegen => _defaultStaminaRegen;
    public float RegenDelay => _defaultRegenDelay;
}
