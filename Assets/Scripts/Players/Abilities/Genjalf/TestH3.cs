using UnityEngine;

public class TestH3 : TargetOrAreaAbility
{
    [SerializeField] private Projectile projectile;

    protected override void Cancel()
    {
        
    }

    protected override void CastAction()
    {
        var tile = Instantiate(projectile, transform.position, Quaternion.identity);
        
        if(Target != null)
        {
            tile.StartFly(Target.transform, true);
        }
        else
        {
            tile.StartFly(Point, true);
        }
    }
}
