using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private TalentSystem talentManager;

    public TalentSystem TalentManager => talentManager;

    public override void Initialize()
    {
		base.Initialize();
        TalentManager.Initialize();
    }
}