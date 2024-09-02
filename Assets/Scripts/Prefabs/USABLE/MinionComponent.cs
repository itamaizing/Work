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
        /*
<<<<<<< HEAD
        CharacterState.Initialize(Health, Move , Stamina, this);
        UIPlayerComponents.Initialize(Abilities, Move, Stamina, Health);
=======
        CharacterState.Initialize(Health, Move , Stamina);
        //UIPlayerComponents.Initialize(Abilities, Move, Stamina, Health); //Why is initialization of this component necessary at all? Moreover, the UI should not initialize the logic
>>>>>>> main*/
        SelectComponent.Initialize(false, Move, Abilities, UIPlayerComponents);
    }

    public virtual void SetParent(GameObject parent)
    {
        Debug.Log(parent.name);
        _heroParent = parent;
    }
}
