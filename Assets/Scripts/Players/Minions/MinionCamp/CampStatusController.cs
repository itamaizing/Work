using Mirror;
using System.Collections;
using UnityEngine;

public class CampStatusController : NetworkBehaviour
{
    [SerializeField] private MeshRenderer _campColor;

    private CampStatus _campStatus = CampStatus.Neutral;
    private bool _isLeadTaken = false;
    private Character _currentOwner = null;

    private CampMinionManager _minionManager;
    private CampSpawner _spawner;
    private Coroutine _checkNeutralCoroutine;

    public CampStatus CurrentStatus => _campStatus;
    public bool IsLeadTaken => _isLeadTaken;
    public Character CurrentOwner => _currentOwner;

    public void Initialize(CampMinionManager minionManager, CampSpawner spawner)
    {
        _minionManager = minionManager;
        _spawner = spawner;
    }

    public void StartMonitoring()
    {
        if (_checkNeutralCoroutine == null)
        {
            _checkNeutralCoroutine = StartCoroutine(CheckNeutralJob());
        }
    }

    private IEnumerator CheckNeutralJob()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(1f);

            if (_campStatus == CampStatus.Neutral) continue;

            bool shouldReturnToNeutral = false;

            if (_isLeadTaken)
            {
                if (_minionManager.GetMinions().Count == 0)
                {
                    shouldReturnToNeutral = true;
                }
            }
            else
            {
                if (_minionManager.GetLead() == null)
                {
                    shouldReturnToNeutral = true;
                }
            }

            if (shouldReturnToNeutral)
            {
                ReturnToNeutral();
            }
        }
    }

    public void SetCaptured(bool takeLead, Character owner)
    {
        if (owner == null) return;

        int teamIndex = owner.NetworkSettings.TeamIndex;

        _campStatus = teamIndex == 1 ? CampStatus.DarkTeam : CampStatus.LightTeam;
        _isLeadTaken = takeLead;
        _currentOwner = owner;

        RpcSetStatus(_campStatus);
    }

    private void ReturnToNeutral()
    {
        bool wasLeadLeft = !_isLeadTaken;

        _campStatus = CampStatus.Neutral;
        _isLeadTaken = false;
        _currentOwner = null;

        RpcSetStatus(_campStatus);

        if (wasLeadLeft && _minionManager.GetLead() == null)
        {
            _spawner.FullRespawn();
        }
        else
        {
            _spawner.RespawnMissing();
        }

        _minionManager.UpdateAllMinionLayers();
    }

    [ClientRpc]
    private void RpcSetStatus(CampStatus status)
    {
        _campStatus = status;
        UpdateCampColor();
    }

    private void UpdateCampColor()
    {
        if (_campColor == null) return;

        Color color = _campStatus switch
        {
            CampStatus.Neutral => Color.gray,
            CampStatus.DarkTeam => Color.red,
            CampStatus.LightTeam => Color.blue,
            _ => Color.gray
        };

        _campColor.material.color = color;
    }
}
