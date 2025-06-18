using Mirror;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GroundTrap : Skill
{
    [SerializeField] private Trap trapPrefab;
    [SerializeField] private HeroComponent owner;
    [SerializeField] private DrawCircleAlternative minDistanceRadiusCircle;
    [SerializeField] private float minDistanceRadius = 2f;
    [SerializeField] private float distanceforTrap = 2.1f;

    private Color minDistanceGreenColor = Color.green;
    private Color minDistanceRedColor = Color.red;

    private Trap _preview;
    private bool _isStartPointPlaced;
    private Vector3 _startPosition, _endPosition;

    protected override bool IsCanCast => !_isStartPointPlaced ||
                                         Vector3.Distance(transform.position, _endPosition) <= Radius &&
                                         Vector3.Distance(transform.position, _endPosition) >= minDistanceRadius &&
                                         Vector3.Distance(_startPosition, _endPosition) <= distanceforTrap;

    protected override int AnimTriggerCastDelay => Animator.StringToHash("ShotCastDelayAnimTrigger");
    protected override int AnimTriggerCast => 0;


    private void OnDestroy() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null) Hero.Move.CanMove = true;

        if (_preview != null)
        {
            Destroy(_preview.gameObject);
            _preview = null;
        }

        minDistanceRadiusCircle?.Clear();
    }

    private void UpdateMinRadiusCircle(Vector3 mousePos)
    {
        bool inside = Vector3.Distance(transform.position, mousePos) < minDistanceRadius;
        var color = inside ? minDistanceRedColor : minDistanceGreenColor;

        if (minDistanceRadiusCircle != null)
        {
            minDistanceRadiusCircle.SetColor(color);
            minDistanceRadiusCircle.Draw(minDistanceRadius);
        }
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        Hero.Move.CanMove = false;
        Hero.Move.StopMoveAnimation();

        CmdGroundTrapInstantiate();
        _preview.ResetPreview();

        minDistanceRadiusCircle?.SetColor(minDistanceGreenColor);
        minDistanceRadiusCircle?.Draw(minDistanceRadius);

        while (!_isStartPointPlaced)
        {
            Vector3 position = GetMousePoint();
            _preview.transform.position = position;
            UpdateMinRadiusCircle(position);

            if (InsideRadius(position) && GetMouseButton)
            {
                _startPosition = position;
                _preview.transform.position = _startPosition;
                _isStartPointPlaced = true;
                _preview.gameObject.SetActive(true);
                continue;
            }
            _preview.transform.position = position;
            yield return null;
        }

        _preview.transform.GetChild(1).gameObject.SetActive(true);

        yield return new WaitUntil(() => !GetMouseButton);
        yield return new WaitForSeconds(0.1f);

        while (true)
        {
            Vector3 position = GetMousePoint();
            _preview.UpdateSecondPoint(position);
            UpdateMinRadiusCircle(position);

         bool posIsValid = Vector3.Distance(transform.position, position) >= minDistanceRadius && Vector3.Distance(transform.position, position) <= Radius &&
         Vector3.Distance(_startPosition, position) <= distanceforTrap;

            if (posIsValid && GetMouseButton)
            {
                _endPosition = position;
                break;
            }
            yield return null;
        }

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_startPosition); targetInfo.Points.Add(_endPosition);
        callbackDataSaved.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        _preview.FixSecondPoint();
        CmdSpawnGroundTrap();
        _preview = null;

        ClearData();
        yield break;
    }

    private bool InsideRadius(Vector3 position)
    {
        float direction = Vector3.Distance(transform.position, position);
        return direction >= minDistanceRadius && direction <= Radius;
    }

    protected override void ClearData()
    {
        Hero.Move.CanMove = true;
        _isStartPointPlaced = false;

        minDistanceRadiusCircle?.Clear();
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count < 2) return;

        _startPosition = targetInfo.Points[0];
        _endPosition = targetInfo.Points[1];

        if (_preview == null) { _preview = Instantiate(trapPrefab); }
        _preview.transform.position = _startPosition;
        _preview.transform.GetChild(1).gameObject.SetActive(true);
        _preview.UpdateSecondPoint(_endPosition);
    }

    [Command] private void CmdGroundTrapInstantiate() => RpcGroundTrapInstantiate();

    [Command]
    private void CmdSpawnGroundTrap()
    {
        _preview.Init(owner, this, _startPosition, _endPosition);
        SceneManager.MoveGameObjectToScene(_preview.gameObject, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(_preview.gameObject);
        RpcInit(_preview.gameObject);
    }

    [ClientRpc] private void RpcGroundTrapInstantiate() => _preview = Instantiate(trapPrefab);

    [ClientRpc]
    protected void RpcInit(GameObject gameObject)
    {
        if (gameObject == null) return;

        Trap trap = gameObject.GetComponent<Trap>();
        if (trap != null) trap.Init(owner, this, _startPosition, _endPosition);
    }
}
