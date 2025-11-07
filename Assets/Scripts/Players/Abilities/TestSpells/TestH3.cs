using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestH3 : Skill
{
    [SerializeField] private Projectile _projectile;
    [SerializeField] private float _animSpeed = 1;

    private Vector3 _targetPoint = Vector3.positiveInfinity;
    private Character _target;

    protected override bool IsCanCast { get => CheckCanCast(); }

    protected override int AnimTriggerCastDelay => 0;

    protected override int AnimTriggerCast => Animator.StringToHash("H3Cast");

    public Character Target { get => _target; private set
        {
            if (value != null)
                Debug.Log(value.name);
            else
                Debug.Log(value);
            _target = value;
        } }

    private bool CheckCanCast()
    {
        if (Target == null)
            return Vector3.Distance(_targetPoint, transform.position) <= Radius;

        return Vector3.Distance(_targetPoint, transform.position) <= Radius ||
               Vector3.Distance(Target.transform.position, transform.position) <= Radius;
    }

    public void AnimCastH3()
    {
        AnimStartCastCoroutine();
    }

    public void AnimH3End()
    {
        AnimCastEnded();
    }

    public void Update()
    {
        if(Input.GetKeyUp(KeyCode.U))
        {
            EnableSkillBoost();
        }
        if(Input.GetKeyUp(KeyCode.I))
        {
            DisableSkillBoost();
        }
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        Target = (Character)targetInfo.Targets[0];
        _targetPoint = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        if (Target != null)
        {
            CmdCreateProjecttile(Target.transform);
        }
        else
        {
            Debug.Log(_targetPoint);
            CmdCreateProjecttile(new Vector3(_targetPoint.x, _targetPoint.y, _targetPoint.z));
        }
        yield return null;
    }

    protected override void ClearData()
    {
        Target = null;
        _targetPoint = Vector3.positiveInfinity;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Buff.CastSpeed.IncreasePercentage(_animSpeed);

        ITargetable target = null;
        Vector3 targetPoint = Vector3.positiveInfinity;

        while (float.IsPositiveInfinity(targetPoint.x) && target == null)
        {
            if (GetMouseButton)
            {
                if (GetRaycastTarget() is ITargetable t)
				    target = t;

                targetPoint = GetMousePoint();
            }
            yield return null;
        }
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(target);
        targetInfo.Points.Add(targetPoint);
        callbackDataSaved(targetInfo);
    }

    [Command]
    protected void CmdCreateProjecttile(Transform target)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.GetComponent<Projectile>().StartFly(target, true);

        NetworkServer.Spawn(item);
    }

    [Command]
    protected void CmdCreateProjecttile(Vector3 point)
    {
        GameObject item = Instantiate(_projectile.gameObject, transform.position, Quaternion.identity);

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);

        item.GetComponent<Projectile>().StartFly(point, true);

        NetworkServer.Spawn(item);
    }
}
