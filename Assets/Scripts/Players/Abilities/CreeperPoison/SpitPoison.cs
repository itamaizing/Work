using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class SpitPoison : Ability
{
    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _playerLinks;
    private Vector2 _mousePos;

    private Coroutine _useCoroutine;
    private Coroutine _shootCoroutine;
    private Coroutine _mouseDirectionCoroutine;

    private float _angle;

    protected override void Cancel()
    {

        if (_useCoroutine != null)
            StopCoroutine(UseCoroutine());

        if (_shootCoroutine != null)
            StopCoroutine(CallShootCoroutine());

        if (_mouseDirectionCoroutine != null)
            StopCoroutine(MouseDirectionCoroutine());
    }

    protected override void Cast()
    {
        _useCoroutine = StartCoroutine(UseCoroutine());
    }

    private IEnumerator UseCoroutine()
    {
        yield return _mouseDirectionCoroutine = StartCoroutine(MouseDirectionCoroutine());
        _shootCoroutine = StartCoroutine(CallShootCoroutine());
    }
    private IEnumerator MouseDirectionCoroutine()
    {
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		Debug.LogError("fix");
		//Vector2 lookDir = _mousePos - _playerLinks.Rigidbody2D.position;
		Debug.LogError("fix");
		//_angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
    }

    private IEnumerator CallShootCoroutine()
    {
        PayCost();
        Shoot();
        yield return null;
    }


    private void Shoot()
    {
        CmdInstantiateProjectile(_angle, _playerLinks.Stamina.CurrentValue);

        _playerLinks.Stamina.TryUse(_playerLinks.Stamina.CurrentValue);

        Cancel();
    }


    [Command]
    private void CmdInstantiateProjectile(float angle, float manaValue)
    {
		Debug.LogError("fix"); 
		SpitPoisonProjectile projectile = Instantiate(_projectile, _playerLinks.gameObject.transform.position, Quaternion.Euler(0, 0, angle)); // it was Rb postion
       // projectile.InitializationProjectile(_playerLinks, manaValue);

        NetworkServer.Spawn(projectile.gameObject);

        RpcInitialization(projectile.gameObject, manaValue);
    }

    [ClientRpc]
    private void RpcInitialization(GameObject projectile, float manaValue)
    {
      //  projectile.GetComponent<SpitPoisonProjectile>().InitializationProjectile(_playerLinks, manaValue);
    }
}
