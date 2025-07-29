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
    [SerializeField] private ArrowTrapProjectile arrowTrapProjectile;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float minDistanceRadius = 2f;
    [SerializeField] private float newHealth = 80;
    [SerializeField] private ObjectData groundData;

    [Header("Talents")]
    [SerializeField] private bool isGroundHealthTalent;

    [Header("Raycast masks")]
    [SerializeField] private LayerMask groundLayer;

    private Color minDistanceGreenColor = Color.green;
    private Color minDistanceRedColor = Color.red;

    private Trap _preview;
    private Vector3 _startPosition;

    private float baseHealth = 23;

    protected override bool IsCanCast => true;
    protected override int AnimTriggerCastDelay => Animator.StringToHash("Shot");
    protected override int AnimTriggerCast => 0;

    private void OnDestroy() => OnSkillCanceled -= HandleSkillCanceled;
    private void OnEnable() => OnSkillCanceled += HandleSkillCanceled;

    private Vector3 GetMousePointOnGround(float y = 0f)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 point = hit.point;
            point.y = y;
            return point;
        }
        return Vector3.positiveInfinity;
    }

    private void HandleSkillCanceled()
    {
        if (_hero?.Move != null) Hero.Move.CanMove = true;

        if (_preview != null)
        {
            Destroy(_preview.gameObject);
            _preview = null;
        }

        Hero.Animator.speed = 1;
        minDistanceRadiusCircle?.Clear();
    }

    private void UpdateMinRadiusCircle(Vector3 mousePos)
    {
        if (minDistanceRadiusCircle == null) return;

        float distance = Vector3.Distance(transform.position, mousePos);
        bool insideMinZone = distance < minDistanceRadius;

        var color = insideMinZone ? minDistanceRedColor : minDistanceGreenColor;
        minDistanceRadiusCircle.SetColor(color);
        minDistanceRadiusCircle.Draw(minDistanceRadius);
    }


    private void SetGroundNewHealth()
    {
        groundData.MaxHealth = newHealth;
        CmdSetGorundNewHealth();
    }

    private void SetGroundBaseHealth()
    {
        groundData.MaxHealth = baseHealth;
        CmdSetGorundBaseHealth();
    }

    protected override IEnumerator PrepareJob(Action<TargetInfo> callbackDataSaved)
    {
        if (isGroundHealthTalent) SetGroundNewHealth();
        else SetGroundBaseHealth();

        Debug.Log($"GroundTrapHealth: {groundData.MaxHealth}");

        Hero.Animator.speed = Hero.Animator.speed / CastDeley;
        Hero.Move.CanMove = false;
        Hero.Move.StopMoveAnimation();

        _preview = Instantiate(trapPrefab);
        _preview.ResetPreview();

        minDistanceRadiusCircle?.SetColor(minDistanceGreenColor);
        minDistanceRadiusCircle?.Draw(minDistanceRadius);

        while (true)
        {
            Vector3 mousePos = GetMousePointOnGround();
            if (float.IsPositiveInfinity(mousePos.x)) { yield return null; continue; }

            bool inOuterRadius = Vector3.Distance(transform.position, mousePos) <= Radius;
            bool outOfInnerRing = Vector3.Distance(transform.position, mousePos) >= minDistanceRadius;

            UpdateMinRadiusCircle(mousePos);

            if (!inOuterRadius || !outOfInnerRing)
            {
                _preview.gameObject.SetActive(false);
                yield return null;
                continue;
            }

            _preview.gameObject.SetActive(true);
            _preview.transform.position = mousePos;

            Vector3 direction = (transform.position - mousePos).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction, Vector3.up);
                _preview.transform.rotation = Quaternion.Euler(0f, lookRot.eulerAngles.y, 0f);
            }

            _preview.UpdateLinePreview();

            if (GetMouseButton)
            {
                _startPosition = mousePos;
                break;
            }

            yield return null;
        }

        minDistanceRadiusCircle?.Clear();

        TargetInfo targetInfo = new TargetInfo();
        targetInfo.Points.Add(_startPosition);
        callbackDataSaved?.Invoke(targetInfo);
    }

    protected override IEnumerator CastJob()
    {
        if (_preview) Destroy(_preview.gameObject);
        CmdSpawnGroundTrap(_startPosition, _preview.transform.rotation);

        ClearData();
        HandleSkillCanceled();
        _preview = null;
        yield break;
    }

    protected override void ClearData()
    {
        Hero.Animator.speed = 1;
        Hero.Move.CanMove = true;
    }

    public void AnimSpawnTrapProjectile()
    {
        if (!isClient || !owner) return;
        CmdSpawnArrowProjectile(_startPosition);
    }

    public override void LoadTargetData(TargetInfo targetInfo)
    {
        if (targetInfo.Points.Count == 0) return;

        _startPosition = targetInfo.Points[0];

        if (_preview == null) _preview = Instantiate(trapPrefab);
        _preview.transform.position = _startPosition;
        _preview.transform.rotation = _preview.transform.rotation;
    }

    [Command] private void CmdSetGorundNewHealth() => groundData.MaxHealth = newHealth;
    [Command] private void CmdSetGorundBaseHealth() => groundData.MaxHealth = baseHealth;

    [Command]
    private void CmdSpawnGroundTrap(Vector3 startPosition, Quaternion rotation)
    {
        Trap trap = Instantiate(trapPrefab, startPosition, rotation);
        trap.Init(owner, this, startPosition, startPosition);
        trap.Finalise();
        SceneManager.MoveGameObjectToScene(trap.gameObject, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(trap.gameObject);
        RpcInit(trap.netIdentity, startPosition, rotation);
    }

    [Command]
    private void CmdSpawnArrowProjectile(Vector3 targetPosition)
    {
        if (!arrowTrapProjectile) return;

        Vector3 startPos = spawnPoint ? spawnPoint.position : transform.position;
        Vector3 direction = (targetPosition - startPos).normalized;
        if (direction == Vector3.zero) return;

        var arrow = Instantiate(arrowTrapProjectile, startPos, Quaternion.LookRotation(direction));
        SceneManager.MoveGameObjectToScene(arrow.gameObject, Hero.NetworkSettings.MyRoom);
        NetworkServer.Spawn(arrow.gameObject);

        arrow.StartFly(targetPosition);
        RpcInitArrow(arrow.netIdentity, targetPosition);
    }

    [ClientRpc]
    private void RpcInitArrow(NetworkIdentity arrowId, Vector3 targetPos)
    {
        if (arrowId && arrowId.TryGetComponent(out ArrowTrapProjectile arrow))
            arrow.StartFly(targetPos);
    }

    [ClientRpc]
    private void RpcInit(NetworkIdentity trapId, Vector3 startPos, Quaternion rotation)
    {
        if (trapId && trapId.TryGetComponent(out Trap trap))
        {
            trap.transform.rotation = rotation;
            trap.Init(owner, this, startPos, startPos);
            trap.Finalise();
            trap.FixSecondPoint();
        }
    }

    #region Talent
    public void GroundTrapHealthActiveTalent(bool value) => isGroundHealthTalent = value;
    #endregion
}
