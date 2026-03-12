using Mirror;
using System.Collections;
using UnityEngine;

public class CampSurrenderController : NetworkBehaviour
{
    [SerializeField] private CampSurrenderUI _surrenderUI;
    [SerializeField, Range(0, 1)] private float _percentageHPForSurrender = 0.3f;
    [SerializeField, Range(0, 10)] private float _campDistance = 5f;
    [SerializeField] private float _checkDelaySeconds = 16f;

    private Coroutine _checkSurrenderCoroutine;
    private CampMinionManager _minionManager;
    private CampStatusController _statusController;
    private CampAttackerTracker _attackerTracker;
    private Transform _campTransform;
    
    private int _initialMinionCount;

    public void Initialize(Transform campTransform, CampMinionManager minionManager, 
        CampStatusController statusController, CampAttackerTracker attackerTracker, int initialMinionCount)
    {
        _campTransform = campTransform;
        _minionManager = minionManager;
        _statusController = statusController;
        _attackerTracker = attackerTracker;
        _initialMinionCount = initialMinionCount;
    }

    public void StartMonitoring()
    {
        if (_checkSurrenderCoroutine == null)
        {
            _checkSurrenderCoroutine = StartCoroutine(CheckSurrenderJob());
        }
    }

    private IEnumerator CheckSurrenderJob()
    {
        yield return new WaitUntil(() => 
            _minionManager.GetLead() != null && 
            _minionManager.GetMinions().Count >= _initialMinionCount);
        
        while (true)
        {
            yield return new WaitForSecondsRealtime(_checkDelaySeconds);

            if (_statusController.CurrentStatus != CampStatus.Neutral)
                continue;

            if (_minionManager.GetLead() == null && _minionManager.GetMinions().Count == 0)
                continue;

            float totalMaxHP = _minionManager.GetTotalMaxHP();
            if (totalMaxHP <= 0) continue;

            float hpThreshold = totalMaxHP * _percentageHPForSurrender;

            while (_minionManager.GetTotalCurrentHP(_campTransform.position, _campDistance) > hpThreshold)
            {
                yield return new WaitForSecondsRealtime(1f);

                if (_statusController.CurrentStatus != CampStatus.Neutral)
                    break;
            }

            if (_statusController.CurrentStatus == CampStatus.Neutral)
            {
                ShowSurrenderUI();
            }

            yield return new WaitForSecondsRealtime(1f);
        }
    }

    private void ShowSurrenderUI()
    {
        _attackerTracker.StopAllTimers();
        RpcSetMinionsUntargetable(true);

        foreach (var hero in _attackerTracker.Attackers)
        {
            if (hero == null || hero.netIdentity?.connectionToClient == null)
                continue;

            TargetShowSurrenderUI(hero.netIdentity.connectionToClient);
        }
    }

    [ClientRpc]
    private void RpcSetMinionsUntargetable(bool untargetable)
    {
        var lead = _minionManager.GetLead();
        if (lead != null && lead.Collider != null)
            lead.Collider.enabled = !untargetable;

        foreach (var minion in _minionManager.GetMinions())
        {
            if (minion != null && minion.Collider != null)
                minion.Collider.enabled = !untargetable;
        }
    }

    [TargetRpc]
    private void TargetShowSurrenderUI(NetworkConnectionToClient conn)
    {
        if (_surrenderUI == null) return;

        _surrenderUI.gameObject.SetActive(true);
        _surrenderUI.Setup(GetComponent<MinionCamp>());
        _surrenderUI.Show();
    }

    public void HideSurrenderUI()
    {
        RpcSetMinionsUntargetable(false);
        RpcHideSurrenderUI();
        _minionManager.UpdateAllMinionLayers();
    }
    [ClientRpc]
    private void RpcHideSurrenderUI()
    {
        if (_surrenderUI == null) return;

        _surrenderUI.Hide();
    }
}
