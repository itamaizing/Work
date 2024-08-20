using Mirror;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestH3 : Skill
{
    [SerializeField] private Projectile _projectile;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector3.Distance(_targetPoint, transform.position) <= Radius;

        return Vector3.Distance(_targetPoint, transform.position) <= Radius ||
               Vector3.Distance(_target.transform.position, transform.position) <= Radius;
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            CmdCreateProjecttile(_target.transform);
        }
        else
        {
            CmdCreateProjecttile(_targetPoint);
        }
        yield return null;
    }

    protected override void ClearData()
    {
        _target = null;
        _targetPoint = Vector3.positiveInfinity;
    }

    protected override IEnumerator PrepareJob()
    {
        while (float.IsPositiveInfinity(_targetPoint.x) && _target == null)
        {
            if (Input.GetMouseButton(0))
            {
                _target = GetRaycastTarget();
                _targetPoint = GetMousePoint();
            }
            yield return null;
        }
    }

    [Command]
    protected void CmdCreateProjecttile(Transform target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);

        //SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.GetComponent<Projectile>().StartFly(target, true);

        NetworkServer.Spawn(item);
    }

    [Command]
    protected void CmdCreateProjecttile(Vector3 point)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);

        //SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.GetComponent<Projectile>().StartFly(point, true);

        NetworkServer.Spawn(item);
    }
}
