using UnityEngine;

public class MinionComponent : Character
{
    [SerializeField] private CharacterData _playerData;
    private HeroComponent _heroParent;

    public HeroComponent HeroParent => _heroParent;
    
    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health,characterData.HealthRegen,characterData.RegenDelay ,characterData.HealthInfo);
        Move.Initialize(characterData.MoveSpeed,Rb);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.RegenDelay);
        CharacterState.Initialize(Health, Move , Stamina);
        UIPlayerComponents.Initialize(Abilities,Move,Stamina,Health);
    }

    public void SetMinion(HeroComponent parent)
    {
        _heroParent = parent;
        Initialize(_playerData);
    }
}
