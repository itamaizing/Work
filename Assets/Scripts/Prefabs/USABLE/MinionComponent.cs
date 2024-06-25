using Mirror;
using UnityEngine;

public class MinionComponent : Character
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
        Health.Initialize(characterData.Health,characterData.HealthRegen, characterData.RegenDelay, characterData.HealthInfo);
        Move.Initialize(characterData.MoveSpeed, Rb);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.RegenDelay);
        CharacterState.Initialize(Health, Move , Stamina);
        UIPlayerComponents.Initialize(Abilities, Move, Stamina, Health);
        SelectComponent.Initialize(false, Move, Abilities, UIPlayerComponents);
    }

    public void SetParent(GameObject parent)
    {
        Debug.Log(parent.name);
        _heroParent = parent;
    }
}
