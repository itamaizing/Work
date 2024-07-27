using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private CharacterData _playerData;

    public CharacterData PlayerData => _playerData;
    
    private void Start()
    {
        Initialize(_playerData);
    }
    
    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health, characterData.HealthRegen, characterData.RegenDelay, characterData.StatsInfo);
        Move.Initialize(characterData.MoveSpeed, Agent, RvoAgent);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.StaminaRegenDelay);
        RuneComponent.Initialize(10,1,10);
        CharacterState.Initialize(Health, Move , Stamina);
        TalentSystem.Initialize();
        UIComponent.Initialize(Abilities, Move, Stamina, Health);
        SelectComponent.Initialize(Move, Abilities, UIComponent);
    }
}
