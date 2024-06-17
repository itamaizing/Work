using Mirror;
using UnityEngine;

public class TestH3 : TargetOrAreaAbility
{
    [SerializeField] private Projectile _projectile;

    protected override void Cancel()
    {
        
    }

    protected override void CastAction()
    {
        //var tile = Instantiate(projectile, transform.position, Quaternion.identity);
        
        if(Target != null)
        {
            //tile.StartFly(Target.transform, true);
            CmdCreateProjecttile(Target.transform);
        }
        else
        {
            //tile.StartFly(Point, true);
            CmdCreateProjecttile(Point);
        }
    }

    [Command]
    protected void CmdCreateProjecttile(Transform target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);

        item.GetComponent<Projectile>().StartFly(target, true);

        NetworkServer.Spawn(item);
    }

    [Command]
    protected void CmdCreateProjecttile(Vector3 point)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);

        item.GetComponent<Projectile>().StartFly(point, true);

        NetworkServer.Spawn(item);
    }
}
