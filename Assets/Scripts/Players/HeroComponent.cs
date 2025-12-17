using Mirror;
using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private TalentSystem talentManager;

    public TalentSystem TalentManager => talentManager;

    public override void Initialize()
    {
		base.Initialize();
        TalentManager.Initialize(LVL);
    }

    public void DestroySelf()
    {
        Del();
    }

    [Command]
    private void Del()
    {
        NetworkServer.Destroy(gameObject);
    }
}