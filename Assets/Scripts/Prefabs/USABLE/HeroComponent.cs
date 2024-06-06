using System;
using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private CharacterData _playerData;

    [SerializeField] private MinionComponent _minion;

    private void Awake()
    {
        Initialize(_playerData);
    }

    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health,characterData.HealthRegen,characterData.RegenDelay ,characterData.HealthInfo);
        Move.Initialize(characterData.MoveSpeed,Rb);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.StaminaRegenDelay);
        RuneComponent.Initialize(10,1,10);
        CharacterState.Initialize(Health, Move , Stamina);
        UIPlayerComponents.Initialize(Abilities,Move,Stamina,Health);
        SelectComponent.Initialize(false,Move,Abilities,UIPlayerComponents);
        SpawnMinion();
    }

    public void SpawnMinion()
    {
        var controllable = Instantiate(_minion);
        controllable.transform.position = transform.position + new Vector3(2, 2, 0);
        controllable.SetMinion(this);
    }
}
