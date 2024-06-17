using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private CharacterData _playerData;

    public bool IController = false;

    public CharacterData PlayerData => _playerData;

    private void Awake()
    {
        Initialize(_playerData);
        if(IController) SelectManager.Instance.AddControl(this);
    }

    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health, characterData.HealthRegen, characterData.RegenDelay, characterData.HealthInfo);
        Move.Initialize(characterData.MoveSpeed, Rb);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.StaminaRegenDelay);
        RuneComponent.Initialize(10,1,10);
        CharacterState.Initialize(Health, Move , Stamina);
        UIPlayerComponents.Initialize(Abilities,Move,Stamina,Health);
        SelectComponent.Initialize(false,Move,Abilities,UIPlayerComponents);
    }
}
