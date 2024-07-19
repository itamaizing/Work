using DG.Tweening;
using JetBrains.Annotations;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongue : Ability
{
    [SerializeField] private Character _dad;
    //[SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private GrabTongueProjectile _tongueProjectile;

    private Vector2 _mousePosition;

    private Coroutine _useAbilityCoroutine;
    private Coroutine _throwInDirectionTargetCoroutine;
    private Coroutine _mouseDirectionCoroutine;

    protected override void Cast()
    {
        _useAbilityCoroutine = StartCoroutine(UseAbility());
    }

    protected override void Cancel()
    {
        if (_useAbilityCoroutine != null)
        {
            StopCoroutine(UseAbility());
            _useAbilityCoroutine = null;
        }

        if (_throwInDirectionTargetCoroutine != null)
        {
            StopCoroutine(ThrowInDirectionTarget());
            _throwInDirectionTargetCoroutine = null;
        }

        if (_mouseDirectionCoroutine != null)
        {
            StopCoroutine(MouseDirectionCoroutine());
            _mouseDirectionCoroutine = null;
        }
    }

    private IEnumerator UseAbility()
    {
        yield return _mouseDirectionCoroutine = StartCoroutine(MouseDirectionCoroutine());

        _throwInDirectionTargetCoroutine = StartCoroutine(ThrowInDirectionTarget());
    }
    
    private IEnumerator MouseDirectionCoroutine()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        _mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private IEnumerator ThrowInDirectionTarget() 
    {
        PayCost();
        CreateTongueProjectile(_mousePosition);
        yield return null;
    }

    private void CreateTongueProjectile(Vector2 mousePosition)
    {
        CmdCreateTongueProjectile(mousePosition);
    }

    [Command]
    private void CmdCreateTongueProjectile(Vector2 mousePosition)
    {
        GameObject item = Instantiate(_tongueProjectile.gameObject, transform.position, Quaternion.identity);
        GrabTongueProjectile tongueProjectile = item.GetComponent<GrabTongueProjectile>();

        tongueProjectile.InitializationProjectile(_dad);
        tongueProjectile.MovingTongueFromPlayer(_dad.transform.position, mousePosition);

        NetworkServer.Spawn(item);

        //RpcCreate(mousePosition);
        RpcInitializationProjectile(item);
    }

    //[ClientRpc]
    //private void RpcCreate(Vector2 mousePosition)
    //{
    //    GameObject item = Instantiate(_tongueProjectile.gameObject, transform.position, Quaternion.identity);
    //    GrabTongueProjectile tongueProjectile = item.GetComponent<GrabTongueProjectile>();

    //    tongueProjectile.InitializationProjectile(_dad);
    //    tongueProjectile.MovingTongueFromPlayer(_dad.transform.position, mousePosition);
    //}

    [ClientRpc]
    private void RpcInitializationProjectile(GameObject projectile)
    {
        projectile.GetComponent<GrabTongueProjectile>().InitializationProjectile(_dad);
    }
}
