using UnityEngine;
using UnityEngine.Serialization;

public class HeroComponent : Character
{
    [FormerlySerializedAs("talents")] [SerializeField] private TalentSystem talentManager;
    public TalentSystem TalentManager => talentManager;

    public override void Initialize()
    {
		//SaveManager.Instance.SetHero(this);
		base.Initialize();
        //SaveManager.Instance.SetHero(this);
        TalentManager.Initialize();
    }
}