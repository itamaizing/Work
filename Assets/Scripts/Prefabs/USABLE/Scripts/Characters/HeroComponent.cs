using UnityEngine;
using UnityEngine.Serialization;

public class HeroComponent : Character
{
    [SerializeField] private CharacterData _playerData; 
    [SerializeField] private TalentsComponent talentsComponent;

    public CharacterData PlayerData => _playerData;
    public TalentsComponent Talents => talentsComponent;
    
    private void Start()
    {
        Initialize(_playerData);
    }
    
    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health, characterData.HealthRegen, characterData.RegenDelay, characterData.StatsInfo);
        Move.Initialize(characterData.MoveSpeed, Agent, RvoAgent, true);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.StaminaRegenDelay);
        RuneComponent.Initialize(10,1,10);
        CharacterState.Initialize(Health, Move , Stamina);
        UIComponent.Initialize(Abilities, Talents, Move, Stamina, Health, _playerData);
        SelectComponent.Initialize(Move, Abilities, UIComponent);
    }
}
