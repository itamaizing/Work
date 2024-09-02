using UnityEngine;

public class HeroComponent : Character
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        if(isLocalPlayer)
        {
            SelectManager.Instance.AddControl(this);
        }
    }

    public override void Initialize(CharacterData characterData)
    {
        Health.Initialize(characterData.Health, characterData.HealthRegen, characterData.RegenDelay, characterData.HealthInfo);
        Move.Initialize(characterData.MoveSpeed, Rigidbody2D , true);
        Stamina.Initialize(characterData.Stamina, characterData.StaminaRegen, characterData.StaminaRegenDelay);
        RuneComponent.Initialize(10,1,10);
        CharacterState.Initialize(this);
        TalentSystem.Initialize();
        //UIPlayerComponents.Initialize(Abilities,Move,Stamina,Health); //Why is initialization of this component necessary at all? Moreover, the UI should not initialize the logic
        SelectComponent.Initialize(false,Move,Abilities,UIPlayerComponents);
	}
}
