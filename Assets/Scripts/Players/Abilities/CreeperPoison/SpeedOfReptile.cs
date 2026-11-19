using System;
using System.Collections;
using UnityEngine;

public class SpeedOfReptile : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private float _duration = 3f;
    [SerializeField] private float _castSpeedMultiplier = 2f;
    [SerializeField] private float _moveSpeedMultiplier = 2f;
    [SerializeField] private float _evasionMultiplier = 2f;

    public float CastSpeedMultiplier => _castSpeedMultiplier;
    public float MoveSpeedMultiplier => _moveSpeedMultiplier;
    public float EvasionMultiplier => _evasionMultiplier;

    protected override int AnimTriggerCast => 0;
    protected override int AnimTriggerPrepare => 0;
    protected override bool IsCanCast => true;

    public override void LoadTargetData(TargetInfo targetInfo) { }

    protected override IEnumerator TargetingJob(Action<TargetInfo> callbackDataSaved)
    {
        yield return null;
    }

    protected override IEnumerator CastJob()
    {
        var player = _player != null ? _player : Hero;
        player.CharacterState.CmdAddState(States.SpeedOfReptile, _duration, 0f, player.gameObject, Name);
        yield return null;
    }

    protected override void ClearData() { }
}
