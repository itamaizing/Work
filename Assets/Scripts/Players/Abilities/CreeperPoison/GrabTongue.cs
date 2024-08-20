using DG.Tweening;
using JetBrains.Annotations;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongue : Skill
{
    [SerializeField] private Character _player;
    [SerializeField] private GrabTongueProjectile _tongueProjectile;
    private Character _target;

    private Vector3 _mousePosition;
    private Vector3 _startPosition;
    private Vector3 _endPosition;

    public bool Enabled;

    protected override bool IsCanCast => throw new System.NotImplementedException();

    protected override IEnumerator PrepareJob()
    {
        _startPosition = _player.transform.position;

        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        if (Input.GetMouseButtonDown(0))
        {
            _target = GetRaycastTarget();
        }

        _mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Vector3.Distance(_startPosition, _mousePosition) <= Radius)
        {
            _endPosition = _mousePosition;
        }
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            TryPayCost();
            CreateTongueProjectile(_mousePosition, _target);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;

    }

    private void CreateTongueProjectile(Vector2 mousePosition, Character target)
    {
        CmdCreateTongueProjectile(mousePosition, target);
    }

    [Command]
    private void CmdCreateTongueProjectile(Vector2 mousePosition, Character target)
    {
        GameObject item = Instantiate(_tongueProjectile.gameObject, transform.position, Quaternion.identity);
        GrabTongueProjectile tongueProjectile = item.GetComponent<GrabTongueProjectile>();

        tongueProjectile.InitializationProjectile(_player, target, _startPosition, _endPosition);
        tongueProjectile.StartTongueAttract();

        NetworkServer.Spawn(item);

        //RpcCreate(mousePosition);
        RpcInitializationProjectile(item, target);
    }

    [ClientRpc]
    private void RpcInitializationProjectile(GameObject projectile, Character target)
    {
        projectile.GetComponent<GrabTongueProjectile>().InitializationProjectile(_player, target, _startPosition, _endPosition);
    }
}
