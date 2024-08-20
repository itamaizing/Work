using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestH2 : Skill
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private float _damage;
    [SerializeField] private int _projectileCount;
    [SerializeField] private float _spawnDeley;

    private Character _target;

    protected override bool IsCanCast => Vector3.Distance(_target.transform.position, transform.position) <= Radius;

    protected override IEnumerator CastJob()
    {
        _target.Health.TryTakeDamage(Buff.Damage.GetBuffedValue(_damage), DamageType.Magical, AttackRangeType.RangeAttack);

        var deley = new WaitForSeconds(_spawnDeley); ;

        for (int i = 0; i < _projectileCount; i++)
        {
            float angle = i * 2 * Mathf.PI / _projectileCount;

            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            Vector3 point = new Vector3(x, y, 0) + _target.transform.position;

            CmdCreateProjecttile(point, _target.transform.position);
            yield return deley;
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
    }

    protected override IEnumerator PrepareJob()
    {
        while (_target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget();
            }
            yield return null;
        }
    }

    [Command]
    protected void CmdCreateProjecttile(Vector3 pointToflay, Vector3 spawnPoint)
    {
        GameObject item = Instantiate(_projectile.gameObject, spawnPoint, Quaternion.identity);

        //SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.GetComponent<Projectile>().StartFly(pointToflay, true);

        NetworkServer.Spawn(item);
    }
}
