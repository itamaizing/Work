using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WhirlpoolSkill : Skill
{
    [SerializeField] private WhirlpoolTile _whirlpoolTilePrefab;
    [SerializeField] private float _duration = 5f;
    [SerializeField] private float _whirlRadius = 6f;
    [SerializeField] private float _maxForce = 12f;
    [SerializeField] private float _minForce = 2f;
    [SerializeField] private float _rate = 0.05f;

    private Vector3 _clickPoint;

    protected override int AnimTriggerCastDelay => 0;
    protected override int AnimTriggerCast => 0;

    protected override bool IsCanCast =>
        Vector3.Distance(_clickPoint, transform.position) <= AreaInfo.Radius;

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count > 0)
            _clickPoint = (Vector3)targetInfo.Points[0];
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        TargetInfo targetInfo = new TargetInfo();

        while (!GetMouseButton)
            yield return null;

        _clickPoint = Targeting.GetMousePoint();
        targetInfo.Points.Add(_clickPoint);
        callbackDataSaved(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        CmdSpawnWhirlpool(_clickPoint, _hero.NetworkSettings.TeamIndex);
        yield return null;
    }

    protected override void ClearData()
    {
        _clickPoint = Vector3.zero;
    }

    [Command]
    private void CmdSpawnWhirlpool(Vector3 position, byte ownerTeamIndex)
    {
        Vector3 instPosition = new Vector3(position.x, position.y + 0.2f, position.z);
        GameObject obj = Instantiate(_whirlpoolTilePrefab.gameObject, instPosition, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(obj, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(obj);

        var tile = obj.GetComponent<WhirlpoolTile>();
        tile.Init(ownerTeamIndex, Targeting.Layer, _whirlRadius, _maxForce, _minForce, _rate);
        tile.StartPull();

        tile.TargetRpcMarkAsOwner(connectionToClient);

        StartCoroutine(DestroyAfter(obj, _duration));
    }

    private IEnumerator DestroyAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            obj.GetComponent<WhirlpoolTile>()?.StopPull();
            NetworkServer.Destroy(obj);
        }
    }
}
