using Mirror;
using UnityEngine;

public class StandartRangeAutoAttack : AutoAttackAbility
{
    [SerializeField] private Projectile _projectilePref;
    [SerializeField] private float _damageRadius;
    [SerializeField] private float _damage;

    private Projectile _projectile;
    protected override void Cancel()
    {
        _projectile = null;
    }

    protected override void CastAction()
    {
        //_projectile = Instantiate(_projectilePref, transform.position, Quaternion.identity);
        //_projectile.StartFly(Target.transform);
        //_projectile.EndPointReached += OnEndPointReached;

        CmdCreateProjecttile(Target.transform);
    }

    protected virtual void OnEndPointReached(Projectile projectile)
    {
        if (projectile.GetDistanceToTarget() <= _damageRadius)
            projectile.Target.GetComponent<Character>().Health.TryTakeDamage(_damage, DamageType.Physical, AttackRangeType.RangeAttack);
    }

    [Command]
    protected void CmdCreateProjecttile(Transform target)
    {
        GameObject item = Instantiate(_projectilePref.gameObject, transform.position, Quaternion.identity);

        _projectile = item.GetComponent<Projectile>();

        _projectile.StartFly(target);

        _projectile.EndPointReached += OnEndPointReached;

        NetworkServer.Spawn(item);
    }
}
