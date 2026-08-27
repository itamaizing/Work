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

    [ClientRpc]
    public void RpcApplyTalentsAndAttributes(HeroProgressSnapshot snapshot)
    {
        Initialize();
        HeroProgressSnapshotApplier.ApplyTalentsAndAttributes(this, snapshot);
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