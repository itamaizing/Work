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

    private bool CheckCanCast()
    {
        if (_target == null)
            return Vector3.Distance(_targetPoint, transform.position) <= Radius;

        return Vector3.Distance(_targetPoint, transform.position) <= Radius ||
               Vector3.Distance(_target.transform.position, transform.position) <= Radius;
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
        _target = (Character)targetInfo.Targets[0];
        _targetPoint = targetInfo.Points[0];
    }

    protected override IEnumerator CastJob()
    {
        if (_target != null)
        {
            CmdCreateProjecttile(_target.transform);
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
        _target = null;
        _targetPoint = Vector3.positiveInfinity;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Buff.CastSpeed.IncreasePercentage(_animSpeed);

        while (float.IsPositiveInfinity(_targetPoint.x) && _target == null)
        {
            if (GetMouseButton)
            {
                _target = GetTarget().character;
                _targetPoint = GetTarget().Position;

				//_target = GetRaycastTarget();
                _targetPoint = GetMousePoint();
            }
            yield return null;
        }
        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Targets.Add(_target);
        targetInfo.Points.Add(_targetPoint);
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
