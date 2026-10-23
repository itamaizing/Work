using UnityEngine;

/// все лагеря респавнятся по общему таймеру.
public class SharedTimerCampManager : CampManager
{
    [SerializeField, Min(1f)] private float _interval = 20f;

    private float _timeLeft;

    protected override RespawnManagement GetManagement(MinionCamp camp) => RespawnManagement.Managed;

    protected override void OnCampCleared(MinionCamp camp, CampClearReason reason)
    {
    }

    public override void StartMatch()
    {
        base.StartMatch();
        _timeLeft = _interval;
    }

    private void Update()
    {
        if (!MatchStarted) return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft > 0f) return;

        _timeLeft = _interval;
        foreach (var camp in Camps)
            if (camp != null && camp.Phase == CampPhase.WaitingRespawn)
                camp.SpawnNow();
    }
}