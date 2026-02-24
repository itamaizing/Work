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

       /* if (TeamsPanel.Instance == null) return;
        if (NetworkSettings.TeamIndex == 1)
        {
            TeamsPanel.Instance.AddInFirstTeam(this);
        }
        else
        {
            TeamsPanel.Instance.AddInSecondTeam(this);
        }*/
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