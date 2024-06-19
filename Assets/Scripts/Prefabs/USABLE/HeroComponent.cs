using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private CharacterData _playerData;

    public CharacterData PlayerData => _playerData;

    public bool AAA;
    private void Start()
    {
        Initialize(_playerData);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if(isLocalPlayer) SelectManager.Instance.AddControl(this);
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
