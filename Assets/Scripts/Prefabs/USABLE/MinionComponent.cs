using Mirror;
using UnityEngine;

public class MinionComponent : Character
{
    [SyncVar]
    public GameObject _heroParent;
    public GameObject HeroParent => _heroParent;

    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health,characterData.HealthRegen, characterData.RegenDelay, characterData.HealthInfo);
        Move.Initialize(characterData.MoveSpeed, Rigidbody2D);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.RegenDelay);
        CharacterState.Initialize(Health, Move , Stamina, this);
        SelectComponent.Initialize(false, Move, Abilities, UIPlayerComponents);
    }

    public void SetParent(GameObject parent)
    {
        Debug.Log(parent.name);
        _heroParent = parent;
    }
}
