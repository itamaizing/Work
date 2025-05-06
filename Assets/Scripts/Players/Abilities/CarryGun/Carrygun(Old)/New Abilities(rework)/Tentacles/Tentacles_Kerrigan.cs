using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;

public class Tentacles_Kerrigan : Skill
{
    [Header("Tentacles Settings")]
    [SerializeField] private float speed;
    [SerializeField] private AnimationCurve _animationCurve;
    [SerializeField] private GameObject _tentaclePrefab;

    [SerializeField] private TentaclesPrefab_Kerrigan _tentacle;

    [SerializeField] private Character _target;
    private Vector3? _pointMoveFrom;
    private Vector3? _pointMoveTo;

    protected override bool IsCanCast => IsMouseInRadius(Radius);

    protected override int AnimTriggerCastDelay => throw new System.NotImplementedException();

    protected override int AnimTriggerCast => throw new System.NotImplementedException();

    protected override IEnumerator CastJob()
    {
        _tentacle = null;

        //CmdCreateTentacle((Vector3)_pointMoveFrom);

        CmdCreateTentacle(_target.transform.position);

        Debug.LogWarning("Tentalce Created");

        yield return new WaitUntil(() => _tentacle != null);

        Debug.LogWarning(_tentacle.transform);
        Debug.LogWarning(_pointMoveFrom);
        Debug.LogWarning(_pointMoveTo);


        //StartCoroutine(MoveTentacle(_tentacle.transform, (Vector3) _pointMoveFrom, (Vector3) _pointMoveTo));

        Vector3 posStart = _target.transform.position;

        StartCoroutine(MoveTentacle(_tentacle.transform, posStart, (Vector3)_pointMoveTo));

        yield return null;
    }

    protected override void ClearData()
    {
        //_pointMoveFrom = null;
        //_pointMoveTo = null;
        //_tentacle = null;
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Character target = null;
        Vector3? firstPoint = null;
        Vector3? secondPoint = null;

        //while (firstPoint == null)
        //{
        //    firstPoint =  GetPointByMouseButtonCLick();
        //    yield return null;
        //}

        while (target == null)
        {
            if (GetMouseButton)
            {
                target = GetRaycastTarget(true);
            }
            yield return null;
        }

        yield return new WaitUntil(() => !GetMouseButton);

        while (secondPoint == null)
        {
            secondPoint = GetPointByMouseButtonCLick();
            yield return null;
        }

        _target = target;
        _pointMoveFrom = firstPoint;
        _pointMoveTo = secondPoint;

        yield return null;
    }

    [Command]
    private void CmdCreateTentacle(Vector3 pointMoveFrom)
    {
        var item = Instantiate(_tentaclePrefab.gameObject, pointMoveFrom, Quaternion.identity);

        SceneManager.MoveGameObjectToScene(item, _hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(item);

        SyncTentacle(item);
        _tentacle = item.GetComponent<TentaclesPrefab_Kerrigan>();

        Destroy(_tentacle.gameObject, CastStreamDuration);
    }

    private Vector3? GetPointByMouseButtonCLick()
    {
        if (GetMouseButton)
        {
            return new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, transform.position.z);
        }

        return null;
    }

    private IEnumerator MoveTentacle(Transform tentacleTransform, Vector3 pointMoveFrom, Vector3 pointMoveTo)
    {
        float time = 0f;
        float normalizedTime;
        Vector3 newPos;

        while (time <= CastStreamDuration)
        {
            yield return null;
            time += Time.deltaTime;
            //normalizedTime = time / CastStreamDuration;
            normalizedTime = Mathf.Clamp01(time / CastStreamDuration);

            newPos = Vector3.Lerp(pointMoveFrom, pointMoveTo, /*normalizedTime * */_animationCurve.Evaluate(normalizedTime));

            //tentacleTransform.position = newPos;
            if (_tentacle != null && newPos != null && _target != null)
                CmdMoveObj(tentacleTransform, newPos, _target.gameObject);
        }


        yield return null;
    }

    [TargetRpc]
    private void SyncTentacle(GameObject tentacle)
    {
        _tentacle = tentacle.GetComponent<TentaclesPrefab_Kerrigan>();
    }

    [Command]
    private void CmdMoveObj(Transform objTransform, Vector3 newPos, GameObject target)
    {
        if (objTransform == null || newPos == null || target)
            return;
        //if (_tempTarget != gameObject)
        //{
        //    _tempTarget = gameObject;
        //    _tempTargetMove = gameObject.GetComponent<MoveComponent>();
        //}
        //_tempTargetMove.TargetRpcSetTransformPosition(newPos);

        Debug.Log($"IM CMD Move with {newPos} newPos");

        objTransform.position = newPos;
        target.transform.position = newPos;

        //var targetMove = target.GetComponent<MoveComponent>();
        //targetMove.TargetRpcSetTransformPosition(newPos);


        //TargetRpcSetTransformPosition(transform, newPos);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        throw new NotImplementedException();
    }

    //[TargetRpc]
    //private void TargetRpcSetTransformPosition(Transform trasformToMove, Vector3 vector3)
    //{
    //    trasformToMove.position = vector3;
    //    Debug.Log($"IM TRPC move with {vector3} newPos");
    //}
}
