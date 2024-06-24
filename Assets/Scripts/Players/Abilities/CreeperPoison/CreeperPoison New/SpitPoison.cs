using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SpitPoison : Ability
{
    [SerializeField] private SpitPoisonProjectile _projectile;
    [SerializeField] private Character _playerLinks;
    private Vector2 _mousePos;
    private bool _enabled = false;
    private bool _canAttack = true;
    private void Update()
    {
        if (!_enabled) return;

        Debug.Log("SpitPoisonUpdate Timer = " + _cooldown);
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("SpitPoisonUpdate");
            PayCost();
            Shoot();
        }
        if (Input.GetMouseButtonDown(1))
        {
            Cancel();
        }
    }

    protected override void Cast()
    {
        _enabled = true;
    }

    protected override void Cancel()
    {
        _enabled = false;
        _canAttack = false;
    }

    private void Shoot()
    {
        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 lookDir = _mousePos - _playerLinks.Rb.position;
        float angle = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg - 90f;
        SpitPoisonProjectile projectile = Instantiate(_projectile, gameObject.transform.position, Quaternion.Euler(0, 0, angle));
        projectile.dad = _playerLinks;
        projectile.energyDad = _playerLinks.Stamina.Value;
        _playerLinks.Stamina.Use(_playerLinks.Stamina.Value);
        Cancel();
    }

}
