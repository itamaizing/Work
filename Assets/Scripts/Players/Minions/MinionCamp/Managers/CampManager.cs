using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CampManager : MonoBehaviour
{
    public static CampManager Instance { get; private set; }

    [SerializeField] private RespawnManagement _management = RespawnManagement.Self;
    [Tooltip("Меньше 0 — брать TimeToFirstSpawn из Camp_SO лагеря. 0 — спавнить сразу.")]
    [SerializeField] private float _firstSpawnDelay = -1f;
    [Tooltip("Время до респавна лагеря после зачистки в режиме Managed")]
    [SerializeField] private float _managedRespawnDelay = 20f;

    public bool MatchStarted { get; private set; }
    public IReadOnlyList<MinionCamp> Camps => CampRegistry.Camps;

    protected virtual void Awake()
    {
        Instance = this;
        CampRegistry.ManagerPresent = true;
        CampRegistry.Registered += OnCampRegistered;
        CampRegistry.Unregistered += OnCampUnregistered;
    }
    
    protected virtual void OnDestroy()
    {
        CampRegistry.Registered -= OnCampRegistered;
        CampRegistry.Unregistered -= OnCampUnregistered;
        CampRegistry.ManagerPresent = false;

        foreach (var camp in CampRegistry.Camps)
            if (camp != null) camp.Cleared -= HandleCleared;

        if (Instance == this) Instance = null;
    }
    
    public virtual void StartMatch()
    {
        if (!NetworkServer.active) return;

        MatchStarted = true;
        foreach (var camp in new List<MinionCamp>(CampRegistry.Camps))
            if (camp != null) SetupCamp(camp);
    }
    
    protected virtual RespawnManagement GetManagement(MinionCamp camp) => _management;

    protected virtual float GetFirstSpawnDelay(MinionCamp camp) => _firstSpawnDelay;

    protected virtual float GetRespawnDelay(MinionCamp camp) => _managedRespawnDelay;

    protected virtual void OnCampCleared(MinionCamp camp, CampClearReason reason)
    {
        if (camp.RespawnMode == RespawnManagement.Managed)
            camp.ScheduleRespawn(GetRespawnDelay(camp));
    }
    
    protected virtual void SetupCamp(MinionCamp camp)
    {
        camp.Cleared -= HandleCleared;
        camp.Cleared += HandleCleared;

        camp.RespawnMode = GetManagement(camp);
        camp.Begin(GetFirstSpawnDelay(camp));
    }

    private void OnCampRegistered(MinionCamp camp)
    {
        if (MatchStarted) SetupCamp(camp);
    }

    private void OnCampUnregistered(MinionCamp camp)
    {
        if (camp != null) camp.Cleared -= HandleCleared;
    }

    private void HandleCleared(MinionCamp camp, CampClearReason reason) => OnCampCleared(camp, reason);
}