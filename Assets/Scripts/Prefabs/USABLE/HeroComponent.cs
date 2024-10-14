using UnityEngine;
using UnityEngine.Serialization;

public class HeroComponent : Character
{
    [FormerlySerializedAs("talents")] [SerializeField] private TalentSystem talentManager;

    public TalentSystem TalentManager => talentManager;

    public override void Initialize()
    {
        base.Initialize();
        TalentManager.Initialize();
    }
}