using Mirror;
using UnityEngine;

public class UnitComponent : Character
{
    [SerializeField] private CharacterData _playerData;
    
    [SyncVar]
    public GameObject _heroParent;
    public GameObject HeroParent => _heroParent;

    private void Awake()
    {
        Initialize(_playerData);
    }

    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health,characterData.HealthRegen, characterData.RegenDelay, characterData.StatsInfo);
        Move.Initialize(characterData.MoveSpeed, Agent, RvoAgent);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.RegenDelay);
        CharacterState.Initialize(Health, Move , Stamina);
        UIComponent.Initialize(Abilities, Move, Stamina, Health);
        SelectComponent.Initialize(Move, Abilities, UIComponent);
    }

    public void SetParent(GameObject parent)
    {
        _heroParent = parent;
    }
}
