using Mirror;
using UnityEngine;

public class HeroComponent : Character
{
    [SerializeField] private TalentSystem talentManager;
    [SerializeField] private GameObject _menuPreview;
    public TalentSystem TalentManager => talentManager;
    public GameObject MenuPreview => _menuPreview;

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