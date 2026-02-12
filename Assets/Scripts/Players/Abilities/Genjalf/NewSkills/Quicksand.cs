using System;
using System.Collections;
using Gangdollarff;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class Quicksand : Skill, IGodLightSpell
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private QuicksandTile _quicksandTile;
    [SerializeField] private float _quicksandDuration = 6;

    private Vector3 _startPoint = Vector3.zero;
    private Vector3 _endPoint = Vector3.zero;
    private QuicksandTile _quicksandTempTile;
    private float _tempCastDeley = 1;
    private float _longPressThreshold = 0.25f;

    public override string AdditionalDescription =>
        $"Длительность: {AbilityNameBox.ColorOpen}{_quicksandDuration} сек{AbilityNameBox.ColorEnd}";

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast => CheckCanCast();

    public bool IsEnabled { get; set; }

    private bool CheckCanCast()
    {
        return Vector3.Distance(_startPoint, transform.position) <= Radius;
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        _startPoint = targetInfo.Points[0];
        _endPoint = targetInfo.Points[1];
    }

    public void ChangeMode()
    {
        if (IsEnabled)
        {
            IsEnabled = false;

            _castDeley = _tempCastDeley;
        }
        else
        {
            IsEnabled = true;

            _tempCastDeley = _cooldownTime;
            _cooldownTime = 0;
        }
    }

    protected override IEnumerator CastJob()
    {
        CmdUse(_startPoint, _endPoint);
        yield return null;
    }

    protected override void ClearData()
    {
        _lineRenderer.positionCount = 0;
        _startPoint = Vector3.zero;
        _endPoint = Vector3.zero;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();
        Vector3 firstPoint = Vector3.zero;

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        float downTime = Time.time;
        firstPoint = GetMousePoint();
        targetInfo.Points.Add(firstPoint);

        while (!Input.GetMouseButtonUp(0))
        {
            if (Time.time - downTime > _longPressThreshold)
            {
                Vector3 holdPoint = GetMousePoint();
                if (targetInfo.Points.Count == 1)
                    targetInfo.Points.Add(holdPoint);
                else
                    targetInfo.Points[1] = holdPoint;
            }

            yield return null;
        }

        bool longClick = (Time.time - downTime) > _longPressThreshold;

        if (longClick)
        {
            Vector3 secondPointOnUp = GetMousePoint();
            if (targetInfo.Points.Count == 1)
                targetInfo.Points.Add(secondPointOnUp);
            else
                targetInfo.Points[1] = secondPointOnUp;
        }
        else
        {
            while (!Input.GetMouseButtonDown(0))
                yield return null;

            Vector3 secondPoint = GetMousePoint();

            while (!Input.GetMouseButtonUp(0))
                yield return null;

            targetInfo.Points.Add(secondPoint);
        }

        callbackDataSaved.Invoke(targetInfo);
    }

    [Command]
    private void CmdUse(Vector3 startPoint, Vector3 endPoint)
    {
        GameObject item = Instantiate(_quicksandTile.gameObject, startPoint, Quaternion.identity);

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        NetworkServer.Spawn(item);

        _quicksandTempTile = item.GetComponent<QuicksandTile>();

        _quicksandTempTile.SetStartPosition(startPoint);
        _quicksandTempTile.SetEndPosition(endPoint);

        _quicksandTempTile.Build();

        StartCoroutine(DurationJob());
    }

    private IEnumerator DurationJob()
    {
        yield return new WaitForSecondsRealtime(_quicksandDuration);
        NetworkServer.Destroy(_quicksandTempTile.gameObject);
    }
}
