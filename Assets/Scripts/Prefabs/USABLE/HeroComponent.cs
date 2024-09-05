using UnityEngine;
using UnityEngine.Serialization;

public class HeroComponent : Character
{
    [FormerlySerializedAs("talents")] [SerializeField] private TalentSystem talentManager;
    [SerializeField] private SpawnComponent _spawnComponent;
    
    public SpawnComponent SpawnComponent => _spawnComponent;
    public TalentSystem TalentManager => talentManager;
    
    public override void Initialize()
    {
        base.Initialize();
        TalentManager.Initialize();
	}
}
