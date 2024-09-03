using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private TalentSystem talents;
    [SerializeField] private SpawnComponent _spawnComponent;
    
    public SpawnComponent SpawnComponent => _spawnComponent;
    public TalentSystem Talents => talents;
    
    public override void Initialize()
    {
        base.Initialize();
        Talents.Initialize();
	}
}
