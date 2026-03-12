using Mirror;
using UnityEngine;

[RequireComponent(typeof(CampMinionManager))]
[RequireComponent(typeof(CampSpawner))]
[RequireComponent(typeof(CampStatusController))]
[RequireComponent(typeof(CampAttackerTracker))]
[RequireComponent(typeof(CampSurrenderController))]
public class MinionCamp : NetworkBehaviour
{
    private CampMinionManager _minionManager;
    private CampSpawner _spawner;
    private CampStatusController _statusController;
    private CampAttackerTracker _attackerTracker;
    private CampSurrenderController _surrenderController;
    
    public CampStatus _campStatus => _statusController.CurrentStatus;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        _minionManager = GetComponent<CampMinionManager>();
        _spawner = GetComponent<CampSpawner>();
        _statusController = GetComponent<CampStatusController>();
        _attackerTracker = GetComponent<CampAttackerTracker>();
        _surrenderController = GetComponent<CampSurrenderController>();

        _minionManager.Initialize(this);
        _spawner.Initialize(transform, _minionManager, _statusController);
        _statusController.Initialize(_minionManager, _spawner);
        _attackerTracker.Initialize();
        _surrenderController.Initialize(transform, _minionManager, _statusController, 
            _attackerTracker, _spawner.InitialMinionCount);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        _spawner.StartSpawning();
        _surrenderController.StartMonitoring();
        _statusController.StartMonitoring();
    }
    
    public void SetPlayers(GameObject playersObject)
    {
        var character = playersObject.GetComponent<Character>();
        if (character != null)
        {
            _minionManager.AddPlayer(character);
        }
    }

    public void AddAttacker(GameObject attacker)
    {
        _attackerTracker.AddAttacker(attacker);
    }

    public void OnMinionDied(GameObject deadMinion)
    {
        if (!isServer || deadMinion == null) return;

        var minionComp = deadMinion.GetComponent<MinionComponent>();
        if (minionComp != null)
        {
            _minionManager.OnMinionDied(minionComp);
        }
    }
    
    [Command(requiresAuthority = false)]
    public void CmdOnCapture(bool isTakeLead, NetworkConnectionToClient senderConn = null)
    {
        Character clickedHero = _attackerTracker.FindHeroByConnection(senderConn);

        if (clickedHero == null)
            return;

        if (isTakeLead)
        {
            if (_minionManager.GetLead() == null || _minionManager.GetLead().Health.CurrentValue <= 0)
            {
                _spawner.SpawnLead();
            }
            _minionManager.TransferLeadToHero(clickedHero);
            _minionManager.TransferMinionsToHero(clickedHero);
     
            _statusController.ReturnToNeutralAfterCapture();
            _spawner.ResetSpawnTimer();
        }
        else
        {
            _statusController.SetCaptured(isTakeLead, clickedHero);
            _minionManager.TransferMinionsToHero(clickedHero);
            _spawner.ResetSpawnTimer();
        }

        _attackerTracker.ClearAllAttackers();
        _surrenderController.HideSurrenderUI();
        _minionManager.UpdateAllMinionLayers();
    }

    public void RemoveDeadMinion(MinionComponent minion)
    {
        _minionManager.OnMinionDied(minion);
    }
}

public enum CampStatus
{
    Neutral,
    DarkTeam,
    LightTeam
}
