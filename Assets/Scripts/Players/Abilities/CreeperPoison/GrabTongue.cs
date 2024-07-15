using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabTongue : Ability
{
    [SerializeField] private Character _dad;
    [SerializeField] private GrabTongueProjectile _tongueProjectile;

    private Vector2 _mousePosition;

    private Coroutine _useAbilityCoroutine;
    private Coroutine _throwInDirectionTargetCoroutine;
    private Coroutine _mouseDirectionCoroutine;

    public Vector2 MousePosition { get => _mousePosition; set => _mousePosition = value; }

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
        CreateToungeProjectile(MousePosition);
        yield return null;
    }

    private void CreateToungeProjectile(Vector2 mousePosition)
    {
        CmdCreateToungeProjectile(mousePosition);
    }

    [Command]
    private void CmdCreateToungeProjectile(Vector2 mousePosition)
    {
        GameObject item = Instantiate(_tongueProjectile.gameObject, transform.position, Quaternion.identity);
        GrabTongueProjectile tongueProjectile = item.GetComponent<GrabTongueProjectile>();

        tongueProjectile.InitializationProjectile(_dad);
        tongueProjectile.MovingTongueFromPlayer(_dad.transform.position, mousePosition);

        NetworkServer.Spawn(item);

        RpcInitializationProjectile(item);
    }

    [ClientRpc]
    private void RpcInitializationProjectile(GameObject projectile)
    {
        projectile.GetComponent<GrabTongueProjectile>().InitializationProjectile(_dad);
    }
}
